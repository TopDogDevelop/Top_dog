using TopDog.App.Brick;
using TopDog.Foundation.Bus;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Operations;

public sealed class OperationClockBrick : IBrick
{
    public string Id() => "operations.clock";

    public void OnRegister(BrickContext ctx)
    {
        if (ctx.State.operationTimeRemainingSec <= 0f)
        {
            ctx.State.operationTimeRemainingSec = ctx.State.operationDurationSec;
        }
    }

    public void Tick(BrickContext ctx, float dtSec)
    {
        if (ctx.State.emptyCombatPending)
        {
            EmptyCombatNoticeService.Tick(ctx.State, dtSec, ctx.Ships);
            return;
        }
        if (ctx.State.phase != GamePhase.OPERATIONS || ctx.State.tutorialComplete)
        {
            return;
        }
        if (ctx.State.operationTimeRemainingSec <= 0f)
        {
            return;
        }
        ctx.State.operationTimeRemainingSec -= dtSec;
        if (ctx.State.operationTimeRemainingSec <= 0f)
        {
            ctx.State.operationTimeRemainingSec = 0f;
            OperationsRoundService.EndOperationsPhase(ctx.State, ctx.Ships, ctx.Modules);
            ctx.Bus.Publish(GameEvent.Of("operations.timeup", "运营阶段倒计时结束"));
            PushAlert(ctx, "运营阶段倒计时结束");
            ctx.State.gameWeek++;
            if (ctx.State.gameWeek > 52)
            {
                ctx.State.gameWeek = 1;
                ctx.State.gameYear++;
            }
            if (!ctx.State.worldline.tutorialMode && ctx.State.worldline.customMatch != null)
            {
                CombatQueueCompiler.Compile(ctx.State, ctx.Ships, ctx.Modules);
                if (ctx.State.combatQueue.Count > 0)
                {
                    CombatPhaseService.EnterCombatPrep(ctx.State, ctx.Ships, ctx.Modules);
                }
                else
                {
                    EmptyCombatNoticeService.Begin(ctx.State);
                }
            }
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
