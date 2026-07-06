using TopDog.Sim.Combat;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.Sim.MechanismTest;

/// <summary>机制详测：与军团约战相同直进实时战，不走运营循环。</summary>
public static class MechanismTestPhaseRules
{
    public static bool InMechanismTestSession(GameState state) =>
        state.mechanismTest != null;

    public static bool IsActiveMatch(GameState state) =>
        InMechanismTestSession(state) && !state.matchEnded;

    public static void EnsureRealtimeCombat(GameState state)
    {
        SkirmishPhaseRules.EnsureRealtimeCombat(state);
    }

    public static bool BlocksCampaignPhaseTransition(GameState state, GamePhase target) =>
        IsActiveMatch(state) && target is GamePhase.OPERATIONS or GamePhase.COMBAT_PREP;

    public static bool ShouldSkipBoardSummonUseLimit(GameState state) =>
        InMechanismTestSession(state);
}
