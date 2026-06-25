using TopDog.Lobby;
using TopDog.Sim.Building;
using TopDog.Sim.State;

namespace TopDog.Sim.Legion;

public static class LegionRegistry
{
    public static void EnsureFromLobby(GameState state, CustomLobbyState lobby)
    {
        if (state.legions.Count > 0)
        {
            return;
        }
        foreach (var p in lobby.players)
        {
            var assetId = p.assetTemplateId;
            if (string.IsNullOrWhiteSpace(assetId))
            {
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
