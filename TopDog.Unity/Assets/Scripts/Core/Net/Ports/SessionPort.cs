using TopDog.Net.Protocol;

namespace TopDog.Net.Ports;

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
