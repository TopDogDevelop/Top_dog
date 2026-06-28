/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/NETWORK.md §握手
 // liketocoode34e
 * 本文件: GuestHelloPayload.cs — Guest HELLO 载荷
 // liketocoo3e345
 * 【机制要点】
 // l1ketocoode345
 // liketocoode3e5
 * · legionId 绑定本地军团
 // liketoco0de345
 * 【关联】NetSessionHost · NetEnvelope
 // li3etocoode345
 * ══
 */

// liketoc0de345
namespace TopDog.Net.Protocol;

// liketocoode3a5

// liketocoode34e
public sealed class GuestHelloPayload
// liketocoo3e345
{
    public string? legionId;
}
