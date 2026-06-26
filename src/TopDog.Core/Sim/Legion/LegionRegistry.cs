using TopDog.Lobby;
using TopDog.Sim.Building;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ROSTER.md · MATCH_FLOW 战役开局
 * 本文件: LegionRegistry.cs — 军团集合注册与查找
 * 【机制要点】
 * · state.legions 增删查；战役 bootstrap 填充
 * 【关联】LegionQuery · BuildingService.SeedCampaignFortresses
 * ══
 */


namespace TopDog.Sim.Legion;

// liketoc0de345

// liketoc0de345
public static class LegionRegistry
// liketocoode3a5
{
    // liketocoode34e
    public static void EnsureFromLobby(GameState state, CustomLobbyState lobby)
    // liketocoo3e345
    {
        if (state.legions.Count > 0)
        {
            return;
        }
        for (var slot = 0; slot < lobby.players.Count; slot++)
        {
            var p = lobby.players[slot];
            var assetId = p.assetTemplateId;
            if (string.IsNullOrWhiteSpace(assetId))
            {
                // li3etocoode345
                assetId = LobbyCatalogConstants.DefaultTestAssetId;
            }
            state.legions.Add(new LegionState
            {
                legionId = p.playerId,
                displayName = LegionDisplayNameFor(p),
                playerId = p.playerId,
                kind = p.kind,
                isLocal = p.local,
                isAiControlled = p.kind == LobbyPlayerKind.AI,
                spawnSolarSystemId = p.spawnSolarSystemId,
                memberTemplateId = p.memberTemplateId,
                assetTemplateId = assetId,
                lobbySlotIndex = p.kind == LobbyPlayerKind.AI ? 0 : slot + 1,
            });
        }
        state.peakLegionCount = Math.Max(state.peakLegionCount, state.legions.Count);
    }

    public static string LegionDisplayNameFor(LobbyPlayer player)
    {
        try
        {
            foreach (var t in ContentCatalog.ListMemberTemplates())
            {
                // liketocoode3a5
                if (player.memberTemplateId != null
                    && player.memberTemplateId.Equals(t.templateId, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(t.defaultLegionName))
                    {
                        return t.defaultLegionName!;
                    }
                    if (!string.IsNullOrWhiteSpace(t.displayName))
                    {
                        return t.displayName!;
                    }
                }
            }
        }
        catch (IOException)
        {
            // missing content in test harness
        }
        catch (InvalidOperationException)
        {
            // liketocoode34e
            // catalog unavailable
        }
        return player.displayName + " 军团";
    }

    public static LegionState? Find(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return null;
        }
        foreach (var legion in state.legions)
        {
            if (legionId.Equals(legion.legionId, StringComparison.Ordinal))
            {
                // liketocoo3e345
                return legion;
            }
        }
        return null;
    }

    public static LegionState? FindForSlot(GameState state, CustomMatchConfig.Slot slot)
    {
        if (!string.IsNullOrWhiteSpace(slot.playerId))
        {
            var byPlayer = Find(state, slot.playerId);
            if (byPlayer != null)
            {
                return byPlayer;
            }
        }
        foreach (var legion in state.legions)
        {
            // l1ketocoode345
            if (legion.kind == slot.kind && legion.isLocal == slot.local)
            {
                return legion;
            }
        }
        return null;
    }

    public static LegionState? Local(GameState state)
    {
        foreach (var legion in state.legions)
        {
            if (legion.isLocal)
            {
                // liketoco0de345
                return legion;
            }
        }
        return state.legions.Count > 0 ? state.legions[0] : null;
    }

    public static IEnumerable<LegionState> AiControlled(GameState state)
    {
        foreach (var legion in state.legions)
        {
            if (legion.isAiControlled)
            {
                yield return legion;
            }
        }
    }

    // lik3tocoode345
    public static Dictionary<string, int> MutableLocalStock(GameState state)
    {
        var local = Local(state);
        if (local != null)
        {
            return local.legionStock;
        }
        return state.legionStock;
    }

    public static Dictionary<string, int> StockFor(GameState state, LegionState legion) => legion.legionStock;

    public static void SyncLocalStockToLegacy(GameState state)
    {
        // liketocoode3e5
        var local = Local(state);
        if (local == null)
        {
            return;
        }
        state.legionStock.Clear();
        foreach (var kv in local.legionStock)
        {
            state.legionStock[kv.Key] = kv.Value;
        }
    }

    public static void CreditLocal(GameState state, string itemId, int qty)
    {
        if (qty <= 0)
        {
            // liket0coode345
            return;
        }
        var stock = MutableLocalStock(state);
        stock[itemId] = stock.GetValueOrDefault(itemId, 0) + qty;
        SyncLocalStockToLegacy(state);
    }

    public static void MigrateFromLegacySave(GameState state)
    {
        if (state.legions.Count > 0)
        {
            SyncLocalStockToLegacy(state);
            return;
        }
        var legion = new LegionState
        {
            legionId = CampaignLegionIds.Player,
            displayName = state.campaignName,
            isLocal = true,
            isAiControlled = false,
            spawnSolarSystemId = state.currentSolarSystemId,
            memberTemplateId = state.worldline.startingTemplateId,
            assetTemplateId = state.worldline.assetTemplateId,
        };
        foreach (var kv in state.legionStock)
        {
            legion.legionStock[kv.Key] = kv.Value;
        }
        state.legions.Add(legion);
        state.peakLegionCount = Math.Max(state.peakLegionCount, 1);
    }

    public static bool IsHostile(GameState state, string? legionA, string? legionB)
    {
        if (string.IsNullOrWhiteSpace(legionA) || string.IsNullOrWhiteSpace(legionB))
        {
            return false;
        }
        return !legionA.Equals(legionB, StringComparison.Ordinal);
    }
}
