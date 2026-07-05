using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

/// <summary>军团约战不走运营⇄交战准备循环；开局即实时战斗（LEGION_SKIRMISH.md）。</summary>
public static class SkirmishPhaseRules
{
    public static bool IsActiveMatch(GameState state) =>
        SkirmishBuildingRules.IsSkirmish(state)
        && state.skirmish != null
        && !state.matchEnded;

    public static bool InSkirmishSession(GameState state) =>
        SkirmishBuildingRules.IsSkirmish(state) && state.skirmish != null;

    public static void EnsureRealtimeCombat(GameState state)
    {
        state.phase = GamePhase.COMBAT;
        state.combatRealtimeActive = true;
        state.combatAwaitingContinue = false;
        state.combatPrepStep = CombatPrepStep.CHOOSE_STANCE;
        state.combatQueue.Clear();
        state.combatQueueIndex = 0;
        state.emptyCombatPending = false;
        state.operationTimeRemainingSec = 0f;
    }

    public static bool BlocksCampaignPhaseTransition(GameState state, GamePhase target) =>
        IsActiveMatch(state) && target is GamePhase.OPERATIONS or GamePhase.COMBAT_PREP;
}
