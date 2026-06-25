using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

public static class PersonalFortressAutonomyService
{
    public const double AnchorChancePerRound = 0.5;

    public static void TryAutonomousAnchors(GameState state, Random rng)
    {
        foreach (var m in state.members)
        {
            if (m.isAi)
            {
                continue;
            }
            var systemId = m.currentSolarSystemId;
            if (systemId == null || !BuildingService.HasPlayerLegionFortressInSystem(state, systemId))
            {
                continue;
            }
            if (rng.NextDouble() >= AnchorChancePerRound)
            {
                continue;
            }
            BuildingService.TryCreatePersonalFortress(state, m, systemId, rng);
        }
    }
}
