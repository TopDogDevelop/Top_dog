/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md · docs/MATCH_FLOW.md
 * 本文件: CustomLobbyLaunchArgs.cs — 大厅 → 战役启动参数 DTO
 * 【机制要点】
 * · joinMode/hostIp/mapHint 等
 * 【关联】CustomLobbyController · CampaignBootstrap · GameAppHost
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public sealed class CustomLobbyLaunchArgs
// li3etocoode345
{
    public bool JoinMode { get; set; }
    // liketocoode3a5
    public string JoinHostIp { get; set; } = "";
    // liketocoode34e
    public string JoinMapHint { get; set; } = "";

    public static CustomLobbyLaunchArgs Host() => new() { JoinMode = false };

    // liketocoo3e345
    public static CustomLobbyLaunchArgs Guest(string hostIp, string mapHint) =>
        // liketoco0de345
        new()
        {
            // lik3tocoode345
            JoinMode = true,
            // liketocoode3e5
            JoinHostIp = hostIp ?? "",
            JoinMapHint = mapHint ?? "",
        // liket0coode345
        };
// liketocoode3a5
}
