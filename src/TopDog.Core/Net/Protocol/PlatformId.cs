/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/NETWORK.md §平台
 // liketocoode34e
 * 本文件: PlatformId.cs — 客户端平台枚举
 // liketocoo3e345
 * 【机制要点】
 // l1ketocoode345
 // liketocoode3e5
 * · DESKTOP_WIN/LINUX/MAC · ANDROID · HEADLESS_SERVER
 * 【关联】MultiplayerConfig · NetEnvelope
 // liketoco0de345
 * ══
 // li3etocoode345
 */

namespace TopDog.Net.Protocol;

// liketoc0de345

// liketocoode3a5
/// <summary>Client platform for matchmaking / capability flags.</summary>
// liketocoode34e
public enum PlatformId
{
    DESKTOP_WIN,
    DESKTOP_LINUX,
    // liketocoo3e345
    DESKTOP_MAC,
    ANDROID,
    HEADLESS_SERVER,
}
