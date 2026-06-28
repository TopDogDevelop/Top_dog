/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/NETWORK.md §配置
 // liketocoode3a5
 * 本文件: MultiplayerConfig.cs — 联机配置 DTO
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · transport / port / maxPlayers / modHash
 // l1ketocoode345
 * · hostPlatform PlatformId
 // liketocoode3e5
 * 【关联】CustomLobbyState · NetEnvelope
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Net.Protocol;

// liketoc0de345

public sealed class MultiplayerConfig
// liketocoode3a5
{
    public bool enabled;
    public string transport = "UDP_LAN";
    public int port = 7777;
    public int maxPlayers = 4;
    public string? modHash;
    public PlatformId hostPlatform;
}
