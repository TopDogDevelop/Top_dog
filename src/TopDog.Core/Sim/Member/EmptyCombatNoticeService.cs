using TopDog.Content.Balance;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class EmptyCombatNoticeService
{
    public static void Begin(GameState state)
    {
        var sec = BalanceConfig.LoadDefault().MatchFlow.emptyCombatNoticeSec;
        state.emptyCombatNoticeSec = sec;
        state.operationTimeRemainingSec = sec;
        state.emptyCombatPending = true;
        state.phase = GamePhase.OPERATIONS;
        PushAlert(state, "本回合无触发战斗");
    }

    public static void Tick(GameState state, float dtSec, ShipRegistry ships)
    {
        if (!state.emptyCombatPending)
        {
            return;
        }
        state.emptyCombatNoticeSec -= dtSec;
        state.operationTimeRemainingSec = Math.Max(0f, state.emptyCombatNoticeSec);
        if (state.emptyCombatNoticeSec <= 0f)
        {
            Confirm(state, ships);
        }
    }

    public static string Confirm(GameState state, ShipRegistry ships)
    {
        if (!state.emptyCombatPending)
        {
            return "无待确认的无战斗提示";
        }
        state.emptyCombatPending = false;
        state.emptyCombatNoticeSec = 0f;
        state.storyRound++;
        CombatPhaseService.BeginOperationsRound(state, ships, null);
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
