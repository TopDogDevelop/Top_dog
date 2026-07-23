using TopDog.Sim.Legion;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Member;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.App;

public static class MechanismTestBootstrap
{
    public static void ApplyToState(GameState state, MechanismTestScenarioDef scenario, string scenarioId)
    {
        var seed = scenario.seed == 0 ? 1 : scenario.seed;
        if ("nav_rally".Equals(scenario.mapMode, StringComparison.Ordinal))
        {
            state.map = MechanismNavMapGenerator.Generate(seed);
        }
        else if ("dual_belt".Equals(scenario.mapMode, StringComparison.Ordinal))
        {
            state.map = MechanismMapGenerator.GenerateDualBelt(seed);
        }
        else
        {
            state.map = MechanismMapGenerator.Generate(seed);
        }
        state.worldline.type = WorldlineType.STORY;
        state.worldline.tutorialMode = false;
        state.currentSolarSystemId = "nav_rally".Equals(scenario.mapMode, StringComparison.Ordinal)
            ? state.map?.Project.systems.Count > 0
                ? state.map.Project.systems[0].solarSystemId
                : null
            : MechanismMapGenerator.SystemId;
        state.mechanismTest = new MechanismTestMatchState
        {
            scenarioId = string.IsNullOrWhiteSpace(scenario.scenarioId) ? scenarioId : scenario.scenarioId,
            seed = seed,
        };

        var rng = new Random(seed);
        MechanismTestRosterLoader.ApplyScenario(state, scenario, rng);

        var playerLegion = scenario.legions.Find(l => l.isPlayer);
        if (playerLegion != null)
        {
            state.flags["lobby.localPlayerId"] = playerLegion.legionId ?? "mt_player";
            state.campaignName = scenario.displayName;
        }

        IdentityMigrationService.EnsureFromMembers(state);
        state.operationDurationSec = 0f;
        state.operationTimeRemainingSec = 0f;
        state.combatQueue.Clear();
        state.combatQueueIndex = 0;
        state.flags["mechanismTest.scenarioId"] = scenario.scenarioId;
        LegionPlayerRegistry.EnsureFromLegions(state);
        SkirmishDisplayNames.SyncSkirmishLabels(state);
    }
}
