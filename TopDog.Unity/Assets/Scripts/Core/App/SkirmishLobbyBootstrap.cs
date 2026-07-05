using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.App;

public static class SkirmishLobbyBootstrap
{
    public static void ApplyToState(GameState state, SkirmishLobbyState lobby)
    {
        state.map = SkirmishMapGenerator.Generate(lobby.scale, lobby.seed == 0 ? new Random().Next() : lobby.seed);
        state.worldline.type = WorldlineType.LEGION_SKIRMISH;
        state.worldline.tutorialMode = false;
        state.currentSolarSystemId = SkirmishMapGenerator.SystemId;
        state.skirmish = new SkirmishMatchState { scale = lobby.scale };

        foreach (var p in lobby.players)
        {
            state.legions.Add(new LegionState
            {
                legionId = p.playerId,
                displayName = LegionRegistry.LegionDisplayNameFor(p),
                playerId = p.playerId,
                kind = p.kind,
                isLocal = p.local,
                isAiControlled = p.kind == LobbyPlayerKind.AI,
                spawnSolarSystemId = SkirmishMapGenerator.SystemId,
                memberTemplateId = p.memberTemplateId,
                assetTemplateId = p.assetTemplateId,
            });
        }

        state.peakLegionCount = state.legions.Count;
        var local = lobby.FindLocal();
        if (local != null)
        {
            state.flags["lobby.localPlayerId"] = local.playerId;
            state.campaignName = local.displayName + " 约战";
        }

        var legionIds = state.legions.ConvertAll(l => l.legionId!);
        SkirmishMapGenerator.SeedBuildings(state, legionIds, lobby.scale);

        foreach (var p in lobby.players)
        {
            if (!lobby.rosterByPlayerId.TryGetValue(p.playerId, out var roster))
            {
                continue;
            }

        foreach (var slot in roster)
        {
            if (string.IsNullOrWhiteSpace(slot.memberTemplateId)
                && !string.IsNullOrWhiteSpace(p.memberTemplateId))
            {
                slot.memberTemplateId = p.memberTemplateId;
            }

            state.members.Add(SkirmishRosterMemberFactory.CreateMember(slot, p.playerId));
                state.memberFittedModules[slot.memberId] = slot.fittedModules
                    .ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
            }
        }

        IdentityMigrationService.EnsureFromMembers(state);
        RestoreRosterVisionTraits(state, lobby);
        state.operationDurationSec = 0f;
        state.operationTimeRemainingSec = 0f;
        state.combatQueue.Clear();
        state.combatQueueIndex = 0;
        state.flags["skirmish.scale"] = lobby.scale.ToString();
        LegionPlayerRegistry.EnsureFromLegions(state);
        SkirmishDisplayNames.SyncSkirmishLabels(state);
    }

    /// <summary>Identity 同步后从名册模版补回可附身/情报员词条（BuildCore 会再次 EnsureFromMembers）。</summary>
    public static void RestoreRosterVisionTraits(GameState state, SkirmishLobbyState lobby)
    {
        foreach (var p in lobby.players)
        {
            if (!lobby.rosterByPlayerId.TryGetValue(p.playerId, out var roster))
            {
                continue;
            }

            foreach (var slot in roster)
            {
                var templateMember = SkirmishRosterMemberFactory.CreateMember(slot, p.playerId);
                var live = state.members.Find(m =>
                    slot.memberId != null
                    && slot.memberId.Equals(m.memberId, StringComparison.Ordinal));
                if (live == null)
                {
                    continue;
                }

                foreach (var traitId in templateMember.traitIds)
                {
                    if (!live.traitIds.Contains(traitId))
                    {
                        live.traitIds.Add(traitId);
                    }

                    var code = IdentityCodes.Of(live);
                    if (string.IsNullOrWhiteSpace(code)
                        || !state.identities.TryGetValue(code, out var identity))
                    {
                        continue;
                    }

                    if (!identity.traitIds.Contains(traitId))
                    {
                        identity.traitIds.Add(traitId);
                    }
                }
            }
        }
    }
}
