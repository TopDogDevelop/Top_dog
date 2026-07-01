/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/NETWORK.md §发现
 // liketocoode3a5
 * 本文件: PeerAnnouncement.cs — LAN 房间广播 DTO
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · roomId / hostIp / playerCount / mapId
 // l1ketocoode345
 * · lastSeenMs 过期剔除
 // liketocoode3e5
 * 【关联】LanLobbyBeacon · LanRoomBrowser
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Net.Lan;

// liketoc0de345

public sealed class PeerAnnouncement
// liketocoode3a5
{
    public string? roomId;
    public string? hostIp;
    public string? hostName;
    public int playerCount = 1;
    public string? mapId;
    public string? roomKind;
    public long lastSeenMs;
}
