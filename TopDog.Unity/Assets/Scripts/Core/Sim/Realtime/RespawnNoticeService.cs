using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>击毁后重生排队时右侧播报（alertLog）一次性提示。</summary>
public static class RespawnNoticeService
{
    public static void PushQueuedOnce(
        GameState state,
        string memberId,
        string shipDisplayName,
        string memberDisplayName,
        float remainSec)
    {
        var key = "respawn.notice." + memberId;
        if (state.flags.ContainsKey(key))
        {
            return;
        }

        state.flags[key] = "1";
        var remainText = FormatRemainText(remainSec);
        var msg = $"{shipDisplayName} · {memberDisplayName} · {remainText}";
        PushAlert(state, msg);
    }

    public static string FormatRemainText(float remainSec)
    {
        var rounded = (int)Math.Max(1, Math.Round(remainSec));
        if (rounded >= 60 && rounded % 60 == 0)
        {
            return $"还有 {rounded / 60} 分钟重生";
        }

        return $"还有 {rounded} 秒重生";
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
