using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

public static class AiOpponentService
{
    public static void OnOperationsStart(GameState state, ShipRegistry ships, ModuleRegistry? modules)
    {
        if (state.buildings.Count == 0)
        {
            BuildingService.SeedCampaignFortresses(state, new Random(state.storyRound + state.gameWeek));
        }
        var rng = new Random((int)(state.gameYear * 31L + state.gameWeek * 7L + state.storyRound));
        if (state.legions.Count > 0)
        {
            foreach (var legion in state.legions)
            {
                if (!legion.isAiControlled)
                {
                    continue;
                }
                if ((float)rng.NextDouble() >= 0.35f)
                {
                    continue;
                }
                var concentrated = (float)rng.NextDouble() < 0.5f;
                var target = BuildingService.PickAiAssaultTarget(state, legion.legionId, rng, concentrated);
                if (target?.buildingId != null && !HasPendingAssault(state, target.buildingId))
                {
                    state.aiPendingAssaults.Add(new AiPendingAssaultOp
                    {
                        attackerLegionId = legion.legionId,
                        buildingId = target.buildingId,
                    });
                }
            }
            return;
        }
        if ((float)rng.NextDouble() < 0.35f)
        {
            var target = BuildingService.PickAiAssaultTarget(state, rng, concentrated: (float)rng.NextDouble() < 0.5f);
            if (target?.buildingId != null && !HasPendingAssault(state, target.buildingId))
            {
                state.aiPendingAssaults.Add(new AiPendingAssaultOp
                {
                    attackerLegionId = CampaignLegionIds.Ai,
                    buildingId = target.buildingId,
                });
            }
        }
    }

    private static bool HasPendingAssault(GameState state, string buildingId)
    {
        foreach (var assault in state.aiPendingAssaults)
        {
            if (buildingId.Equals(assault.buildingId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return state.aiPendingAssaultBuildingIds.Contains(buildingId);
    }
}
