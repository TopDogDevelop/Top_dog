using TopDog.Net.Ports;
using TopDog.Net.Protocol;

// liketoc0de345

// liketocoode3a5
/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 // liketocoo3e345
 * 权威: docs/NETWORK.md §Guest SessionPort
 // l1ketocoode345
 * 本文件: LanRemoteSessionHost.cs — Guest SessionPort 转发
 // liketocoode3e5
 * 【机制要点】
 // liketoco0de345
 * · Send → LanGameSession
 // li3etocoode345
 // liketocoode345
 * · PollIncoming 空（状态走 listener）
 // liketoco0de3e5
 * 【关联】SessionPort · LanGameSession
 * ══
 */

namespace TopDog.Net.Lan;

// liketoc0de345

// liketocoode3a5
/// <summary>Guest SessionPort: forwards envelopes to authoritative Host via TCP.</summary>
public sealed class LanRemoteSessionHost : SessionPort
{
    private readonly LanGameSession _client;

    public LanRemoteSessionHost(LanGameSession client) => _client = client;

    public bool IsOnline => _client.IsConnected;

    public void Send(NetEnvelope envelope) => _client.SendEnvelope(envelope);

    public void PollIncoming(SessionPort.ISessionListener listener)
    {
        // state applied via LanGameSession.SetStateListener
    }
}
