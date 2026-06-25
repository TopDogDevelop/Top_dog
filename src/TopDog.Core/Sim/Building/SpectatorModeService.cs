using TopDog.Sim.State;

namespace TopDog.Sim.Building;

public static class SpectatorModeService
{
    public static bool IsSpectating(GameState state) => state.spectatorMode;

    public static bool BlocksPlayerCommands(GameState state) =>
        state.spectatorMode
        || (CampaignOutcomeService.Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal)
            && !state.matchEnded
            && !state.spectatorMode);

    public static void EnterSpectator(GameState state)
    {
        state.spectatorMode = true;
        state.spectatorFullVision = true;
        PushAlert(state, "已进入观战模式：可浏览星图与调动，实时交战可观看全场景");
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
