using TopDog.Lobby;
using TopDog.Sim.Legion;
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
                state.members.Add(new MemberState
                {
                    memberId = slot.memberId,
                    name = slot.displayName,
                    equippedHullId = slot.hullId,
                    legionId = p.playerId,
                    appraised = true,
                });
                state.memberFittedModules[slot.memberId] = slot.fittedModules
                    .ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
            }
        }

        state.operationDurationSec = 0f;
        state.operationTimeRemainingSec = 0f;
        state.flags["skirmish.scale"] = lobby.scale.ToString();
        LegionPlayerRegistry.EnsureFromLegions(state);
        SkirmishDisplayNames.SyncSkirmishLabels(state);
    }
}
