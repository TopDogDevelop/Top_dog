using TopDog.App;
using TopDog.Net.Ports;
using TopDog.Net.Protocol;
using TopDog.Sim.Realtime;

namespace TopDog.Net.Local;

/// <summary>Offline host: applies envelopes to bound <see cref="SimulationCore"/> (no network I/O).</summary>
public sealed class LocalSessionHost : SessionPort, ILegionCommandSink
{
    private SimulationCore? _core;
    private string? _lastCommandResult;

    public bool IsOnline => false;

    public void Bind(SimulationCore core) => _core = core;

    public void Unbind() => _core = null;

    public string? ConsumeLastCommandResult()
    {
        var r = _lastCommandResult;
        _lastCommandResult = null;
        return r;
    }

    public string Submit(string legionId, string commandLine) =>
        ApplyCommand(commandLine, legionId);

    public void Send(NetEnvelope envelope)
    {
        if (_core == null)
        {
            return;
        }
        switch (envelope.type)
        {
            case NetMessageType.COMMAND_SUBMIT:
                var (legionId, line) = CommandSubmitCodec.Parse(envelope.payloadJson);
                _lastCommandResult = ApplyCommand(line, legionId);
                break;
            case NetMessageType.TACTICAL_INPUT:
                if (string.IsNullOrEmpty(envelope.payloadJson))
                {
                    break;
                }
                var sample = System.Text.Json.JsonSerializer.Deserialize<PossessionInputSample>(
                    envelope.payloadJson, Foundation.Json.TopDogJson.Options);
                if (sample != null)
                {
                    _core.ApplyPossessionInput(sample);
                }
                break;
        }
    }

    private string ApplyCommand(string line, string? legionId)
    {
        if (_core == null)
        {
            return "未绑定 SimulationCore";
        }
        return _core.SubmitCommand(line, legionId);
    }

    public void PollIncoming(SessionPort.ISessionListener listener)
    {
        // no-op for local play
    }
}
