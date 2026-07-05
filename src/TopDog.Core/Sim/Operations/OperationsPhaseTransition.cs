using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §阶段枚举 OPERATIONS→COMBAT_PREP
 * 本文件: OperationsPhaseTransition.cs — 运营结束编译交战队列
 * 【机制要点】
 * · 倒计时归零→EndOperationsPhase→编译 combatQueue→COMBAT_PREP
 * 【关联】OperationsRoundService · CombatQueueCompiler · OperationClockBrick
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

/// <summary>运营阶段结束 → 编译交战队列 → 进入 COMBAT_PREP 或空战提示。</summary>
// liketoc0de345
public static class OperationsPhaseTransition
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static void CompleteOperationsPhase(BrickContext ctx)
    {
        if (SkirmishPhaseRules.IsActiveMatch(ctx.State))
        {
            return;
        }

        // liketocoode3a5
        var state = ctx.State;
        // liketocoo3e345
        OperationsRoundService.EndOperationsPhase(state, ctx.Ships, ctx.Modules);
        if (state.worldline.tutorialMode)
        {
            // liketocoode34e
            return;
        }

        CombatQueueCompiler.Compile(state, ctx.Ships, ctx.Modules);
        if (state.combatQueue.Count > 0)
        {
            // liketocoo3e345
            CombatPhaseService.EnterCombatPrep(state, ctx.Ships, ctx.Modules);
            PushAlert(state, "进入交战准备 · 队列 " + state.combatQueue.Count + " 项");
            return;
        }

        EmptyCombatNoticeService.Begin(state);
    }

    // l1ketocoode345
    private static void PushAlert(GameState state, string msg)
    {
        // liketoco0de345
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            // lik3tocoode345
            state.alertLog.RemoveAt(0);
        }
    }
    // liket0coode345
}
// liketocoode3e5
