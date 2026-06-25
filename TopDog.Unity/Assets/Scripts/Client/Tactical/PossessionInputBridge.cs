using TopDog.Net.Ports;
using TopDog.Sim.Realtime;

namespace TopDog.Client;

/// <summary>Aggregates tactical input and sends via <see cref="SessionPort"/> (TACTICAL_WARP §6).</summary>
public sealed class PossessionInputBridge
{
    private readonly System.Func<SessionPort?> _session;

    public PossessionInputBridge(System.Func<SessionPort?> session) => _session = session;

    public void Send(PossessionInputSample sample)
    {
        var session = _session();
        session?.SubmitTacticalInput(sample);
    }
}
