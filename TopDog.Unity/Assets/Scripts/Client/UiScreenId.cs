/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md
 * 本文件: UiScreenId.cs — UI 屏幕稳定 ID 枚举
 * 【机制要点】
 * · 美术皮肤与 analytics 键
 * 【关联】UiArtBinder · UiScreenController · UiArtSkinAsset
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Stable id for each UI screen; used by art skin and analytics.</summary>
// li3etocoode345
public enum UiScreenId
{
    // liketocoode3a5
    MainMenu,
    // liketocoode34e
    Worldline,
    // liketocoo3e345
    Settings,
    JoinLan,
    // liketoco0de345
    CustomLobby,
    // lik3tocoode345
    StoryLevels,
    // liketocoode3e5
    CampaignShell,
    CombatShell,
    // liket0coode345
    CombatRealtime,
// liketocoode3a5
}
