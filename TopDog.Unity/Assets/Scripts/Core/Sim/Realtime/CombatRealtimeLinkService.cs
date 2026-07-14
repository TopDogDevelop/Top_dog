using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>实时战场连接握手：画面可见但模拟冻结 2s，广播连接状态。</summary>
public static class CombatRealtimeLinkService
{
    public const float HandshakeDelaySec = 2f;
    private const string Tag = "combat.link";

    public static void Begin(GameState state)
    {
        // 战役 / 约战 / 机制详测共用：开启全量战斗诊断落盘
        CombatTelemetrySessionExport.Begin(state);
        state.combatRealtimeLinkHandshakeActive = true;
        state.combatRealtimeLinkDelaySec = HandshakeDelaySec;
        PushAlert(state, "正在建立战场连接，请等候");
        CombatTelemetryLog.Log(Tag, "正在建立战场连接，请等候");
    }

    public static void Reset(GameState state)
    {
        state.combatRealtimeLinkHandshakeActive = false;
        state.combatRealtimeLinkDelaySec = -1f;
        CombatTelemetrySessionExport.End("link.reset");
    }

    /// <summary>推进握手倒计时；返回 true 时战场模拟可 tick。</summary>
    public static bool TickHandshake(GameState state, float dtSec)
    {
        if (!state.combatRealtimeActive || !state.combatRealtimeLinkHandshakeActive)
        {
            return true;
        }

        if (state.combatRealtimeLinkDelaySec <= 0f)
        {
            return true;
        }

        state.combatRealtimeLinkDelaySec -= dtSec;
        if (state.combatRealtimeLinkDelaySec > 0f)
        {
            return false;
        }

        state.combatRealtimeLinkDelaySec = 0f;
        state.combatRealtimeLinkHandshakeActive = false;
        PushAlert(state, "实时战场连接成功！");
        CombatTelemetryLog.Log(Tag, "实时战场连接成功！");
        return true;
    }

    public static bool IsHandshakeFrozen(GameState state) =>
        state.combatRealtimeActive
        && state.combatRealtimeLinkHandshakeActive
        && state.combatRealtimeLinkDelaySec > 0f;

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
