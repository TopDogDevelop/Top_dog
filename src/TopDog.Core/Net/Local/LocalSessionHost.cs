using TopDog.App;
using TopDog.Net.Ports;
using TopDog.Net.Protocol;
using TopDog.Sim.Realtime;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §本地
 * 本文件: LocalSessionHost.cs — 离线 SessionPort
 * 【机制要点】
 * · Bind SimulationCore；Send 直接应用 envelope
 * · ILegionCommandSink.Submit 皮套命令
 * 【关联】SessionPort · SimulationCore
 * ══
 */

namespace TopDog.Net.Local;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Offline host: applies envelopes to bound <see cref="SimulationCore"/> (no network I/O).</summary>
// liketocoode34e
public sealed class LocalSessionHost : SessionPort, ILegionCommandSink
// liketocoode3a5
{
    // liketocoo3e345
    private SimulationCore? _core;
    // l1ketocoode345
    private string? _lastCommandResult;

// liketocoode3e5

    // liketoco0de345
    public bool IsOnline => false;

// li3etocoode345

    public void Bind(SimulationCore core) => _core = core;

    public void Unbind() => _core = null;

// liketocoode345

    public string? ConsumeLastCommandResult()
    // liketoco0de3e5
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
