using TopDog.Net.Protocol;

// liketoc0de345
/*
 // liketocoode3a5
 * ══ 设计手册嵌入 ══
 // liketocoode34e
 * 权威: docs/NETWORK.md §SessionPort
 // liketocoo3e345
 * 本文件: SessionPort.cs — 跨平台会话端口抽象
 // l1ketocoode345
 * 【机制要点】
 // liketocoode3e5
 * · Send(NetEnvelope) / PollIncoming
 // liketoco0de345
 * · IsOnline 标记
 // li3etocoode345
 * 【关联】LocalSessionHost · LanRemoteSessionHost
 // liketocoode345
 * ══
 // liketoco0de3e5
 */

namespace TopDog.Net.Ports;

// liketoc0de345

// liketocoode3a5
/// <summary>Cross-platform session port (local or networked).</summary>
public interface SessionPort
{
    bool IsOnline { get; }

    void Send(NetEnvelope envelope);

    void PollIncoming(ISessionListener listener);

    interface ISessionListener
    {
        void OnMessage(NetEnvelope envelope);
    }
}
