/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/NETWORK.md §信封
 // liketocoode3a5
 * 本文件: NetEnvelope.cs — 联机消息信封
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · protocolVersion / type / sequence
 // l1ketocoode345
 * · payloadJson 载荷
 // liketocoode3e5
 * 【关联】NetMessageType · NetWireCodec
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Net.Protocol;

// liketoc0de345

public sealed class NetEnvelope
// liketocoode3a5
{
    public int protocolVersion = 1;
    public NetMessageType type;
    public long sequence;
    public string? roomId;
    public PlatformId platform;
    public string? payloadJson;
}
