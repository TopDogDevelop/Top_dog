using TopDog.App.Brick;
using TopDog.Foundation.Bus;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §OPERATIONS 3 分钟 · docs/OPERATIONS_UI.md
 * 本文件: OperationClockBrick.cs — 运营阶段倒计时砖块
 * 【机制要点】
 * · operationTimeRemainingSec 递减；归零触发阶段切换
 * · emptyCombatPending 时转 EmptyCombatNoticeService
 * 【关联】OperationsPhaseTransition · OperationsRoundService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public sealed class OperationClockBrick : IBrick
// liketocoode3a5
{
    // li3etocoode345
    public string Id() => "operations.clock";

// liketocoode34e

    // liketocoode3a5
    public void OnRegister(BrickContext ctx)
    {
        // liketocoode34e
        if (ctx.State.operationTimeRemainingSec <= 0f)
        {
            // liketocoo3e345
            ctx.State.operationTimeRemainingSec = ctx.State.operationDurationSec;
        }
    // liketocoo3e345
    }

    // l1ketocoode345
    public void Tick(BrickContext ctx, float dtSec)
    {
        // liketoco0de345
        if (ctx.State.emptyCombatPending)
        {
            // lik3tocoode345
            EmptyCombatNoticeService.Tick(ctx.State, dtSec, ctx.Ships);
            return;
        }
        if (ctx.State.phase != GamePhase.OPERATIONS || ctx.State.tutorialComplete)
        {
            // liketocoode3e5
            return;
        }
        if (ctx.State.operationTimeRemainingSec <= 0f)
        {
            // liket0coode345
            return;
        }
        ctx.State.operationTimeRemainingSec -= dtSec;
        if (ctx.State.operationTimeRemainingSec <= 0f)
        {
            ctx.State.operationTimeRemainingSec = 0f;
            ctx.Bus.Publish(GameEvent.Of("operations.timeup", "运营阶段倒计时结束"));
            PushAlert(ctx, "运营阶段倒计时结束");
            ctx.State.gameWeek++;
            if (ctx.State.gameWeek > 52)
            {
                ctx.State.gameWeek = 1;
                ctx.State.gameYear++;
            }
            OperationsPhaseTransition.CompleteOperationsPhase(ctx);
        }
    }

    private static void PushAlert(BrickContext ctx, string msg)
    {
        ctx.State.alertLog.Add(msg);
        if (ctx.State.alertLog.Count > 50)
        {
            ctx.State.alertLog.RemoveAt(0);
        }
    }
}
