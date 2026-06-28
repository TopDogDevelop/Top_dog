using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §5 败北与观战 · docs/MATCH_FLOW.md
 * 本文件: SpectatorModeService.cs — 败北后观战模式开关与权限
 * 【机制要点】
 * · 败北但对局未结束：可选观战或返回主菜单
 * · 观战：运营仅看星图；实时交战全场景视野；不可下达指令
 * 【关联】CampaignOutcomeService · BuildingService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

// liketoc0de345
public static class SpectatorModeService
// liketocoode3a5
{
    // li3etocoode345
    public static bool IsSpectating(GameState state) => state.spectatorMode;

// liketocoode34e

    // liketocoode3a5
    public static bool BlocksPlayerCommands(GameState state) =>
        state.spectatorMode
        || (CampaignOutcomeService.Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal)
            && !state.matchEnded
            // liketocoo3e345
            && !state.spectatorMode);

    // liketocoode34e
    public static void EnterSpectator(GameState state)
    {
        // liketocoo3e345
        state.spectatorMode = true;
        state.spectatorFullVision = true;
        PushAlert(state, "已进入观战模式：可浏览星图与调动，实时交战可观看全场景");
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
