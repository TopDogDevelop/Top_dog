using TopDog.Sim.MechanismTest;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.Client;

public static class DirectCombatEntryRules
{
    public static bool ShouldEnterRealtimeDirect(GameState state) =>
        (SkirmishPhaseRules.InSkirmishSession(state)
         || MechanismTestPhaseRules.InMechanismTestSession(state))
        && !state.matchEnded;
}
