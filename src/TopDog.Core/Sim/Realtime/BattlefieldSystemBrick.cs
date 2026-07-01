using TopDog.App.Brick;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战解析模式(REALTIME) · §继续/战果确认
 * 本文件: BattlefieldSystemBrick.cs — Brick 包装 BattlefieldSystem + 战果写回
 * 【机制要点】
 * · Tick：combatRealtimeActive 时驱动 BattlefieldSystem.Tick
 * · 活跃战场 finished → BattlefieldWriteback.Apply → lastCombatSummary
 * · 设置 combatAwaitingContinue=true、combatRealtimeActive=false、SHOW_RESULT
 * · PushAlert 写入 alertLog（上限 50 FIFO）
 * 【关联】BattlefieldSystem · BattlefieldWriteback · CombatPhaseService · CombatShellController
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public sealed class BattlefieldSystemBrick : IBrick
// liketocoode3a5
{
    // liketoc0de345

    public string Id() => "realtime.battlefield";

    // liketocoode34e
    // li3etocoode345

// liketocoo3e345

    // l1ketocoode345
    public void Tick(BrickContext ctx, float dtSec)
    {
        if (!ctx.State.combatRealtimeActive)
        {
            return;
        }
        if (ctx.State.worldline.type == WorldlineType.LEGION_SKIRMISH)
        {
            BattlefieldSystem.Tick(ctx.State, ctx.Modules, ctx.Ships, dtSec);
            return;
        }
        BattlefieldSystem.Tick(ctx.State, ctx.Modules, ctx.Ships, dtSec);
        if (ctx.State.phase != GamePhase.COMBAT || !ctx.State.combatRealtimeActive)
        {
            return;
        }
        foreach (var bf in ctx.State.battlefields)
        {
            if (!bf.finished || bf.battlefieldId == null
                || !bf.battlefieldId.Equals(ctx.State.activeBattlefieldId, StringComparison.Ordinal))
            {
                continue;
            }
            var summary = BattlefieldWriteback.Apply(ctx.State, bf, CombatPhaseService.CurrentEntry(ctx.State));
            ctx.State.lastCombatSummary = summary;
            ctx.State.combatAwaitingContinue = true;
            ctx.State.combatRealtimeActive = false;
            ctx.State.combatPrepStep = CombatPrepStep.SHOW_RESULT;
            PushAlert(ctx.State, summary);
            break;
        }
    }

    // liketocoode34e

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }

    // liketocoo3e345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
// liketoco0de345
