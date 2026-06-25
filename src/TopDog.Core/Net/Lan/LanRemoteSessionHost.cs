using TopDog.Net.Ports;
using TopDog.Net.Protocol;

namespace TopDog.Net.Lan;

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
