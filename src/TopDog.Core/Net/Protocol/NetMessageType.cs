/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/NETWORK.md §消息类型
 // liketocoode3a5
 * 本文件: NetMessageType.cs — 联机消息枚举
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · HELLO / COMMAND_SUBMIT / STATE_DELTA / TACTICAL_INPUT…
 // l1ketocoode345
 * · 与 Java NetSessionHost 对齐
 // liketocoode3e5
 * 【关联】NetEnvelope · NetWireCodec
 // liketoco0de345
 * ══
 // li3etocoode345
 */

// liketocoode345

// liketoco0de3e5
namespace TopDog.Net.Protocol;

// liketoc0de345

public enum NetMessageType
// liketocoode3a5
{
    HELLO,
    HELLO_ACK,
    ROOM_LIST,
    JOIN_REQUEST,
    JOIN_ACCEPT,
    COMMAND_SUBMIT,
    STATE_DELTA,
    PHASE_SYNC,
    TACTICAL_INPUT,
    MATCH_PAUSE,
    MATCH_RESUME,
    DISCONNECT,
}
