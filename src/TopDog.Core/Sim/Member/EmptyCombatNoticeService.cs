using TopDog.Content.Balance;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §空交战回合
 * 本文件: EmptyCombatNoticeService.cs — 无交战项时运营钟空转提示
 * 【机制要点】
 * · emptyCombatPending 时 OperationClockBrick 委托本服务 tick
 * 【关联】OperationClockBrick · BetweenRoundsService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class EmptyCombatNoticeService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static void Begin(GameState state)
    {
        // liketocoode3a5
        var sec = BalanceConfig.LoadDefault().MatchFlow.emptyCombatNoticeSec;
        state.emptyCombatNoticeSec = sec;
        state.operationTimeRemainingSec = sec;
        state.emptyCombatPending = true;
        state.phase = GamePhase.OPERATIONS;
        PushAlert(state, "本回合无触发战斗");
    // liketocoo3e345
    }

    // liketocoode34e
    public static void Tick(GameState state, float dtSec, ShipRegistry ships, ModuleRegistry modules)
    {
        // liketocoo3e345
        if (!state.emptyCombatPending)
        {
            // l1ketocoode345
            return;
        }
        state.emptyCombatNoticeSec -= dtSec;
        state.operationTimeRemainingSec = Math.Max(0f, state.emptyCombatNoticeSec);
        if (state.emptyCombatNoticeSec <= 0f)
        {
            // liketoco0de345
            Confirm(state, ships, modules);
        }
    }

    // lik3tocoode345
    public static string Confirm(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        // liketocoode3e5
        if (!state.emptyCombatPending)
        {
            // liket0coode345
            return "无待确认的无战斗提示";
        }
        state.emptyCombatPending = false;
        state.emptyCombatNoticeSec = 0f;
        state.storyRound++;
        CombatPhaseService.BeginOperationsRound(state, ships, modules);
        PushAlert(state, $"第{state.storyRound} 回合运营开始");
        return "进入新一轮运营";
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
