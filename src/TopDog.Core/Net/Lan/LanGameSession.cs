using System.Net.Sockets;
using System.Text;
using TopDog.Net.Protocol;
using TopDog.Sim.Persist;
using TopDog.Sim.State;

namespace TopDog.Net.Lan;

/// <summary>TCP guest client: send commands upstream, receive authoritative state.</summary>
public sealed class LanGameSession : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private long _sequence = 1;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private Action<GameState>? _stateListener;
    private Action<MatchPausePayload>? _pauseListener;

    public LanGameSession(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public bool IsConnected => _client?.Connected == true;

    public void SetStateListener(Action<GameState>? listener) => _stateListener = listener;

    public void SetPauseListener(Action<MatchPausePayload>? listener) => _pauseListener = listener;

    public void Connect(int timeoutMs = 3000)
    {
        _client = new TcpClient();
        var task = _client.ConnectAsync(_host, _port);
        if (!task.Wait(timeoutMs))
        {
            throw new TimeoutException("连接 Host 超时");
        }
        var stream = _client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        SendHello();
    }

    public void SendCommand(string commandLine, string? legionId = null)
    {
        if (_writer == null || string.IsNullOrEmpty(commandLine))
        {
            return;
        }
        var env = new NetEnvelope
        {
            type = NetMessageType.COMMAND_SUBMIT,
            sequence = ++_sequence,
            platform = PlatformId.DESKTOP_WIN,
            payloadJson = CommandSubmitCodec.ToJson(commandLine, legionId),
        };
        _writer.WriteLine(NetWireCodec.ToWire(env));
    }

    public void SendPauseRequest(MatchPausePayload payload)
    {
        if (_writer == null)
        {
            return;
        }
        var env = new NetEnvelope
        {
            type = payload.paused ? NetMessageType.MATCH_PAUSE : NetMessageType.MATCH_RESUME,
            sequence = ++_sequence,
            platform = PlatformId.DESKTOP_WIN,
            payloadJson = MatchPauseCodec.ToJson(payload),
        };
        _writer.WriteLine(NetWireCodec.ToWire(env));
    }

    public void SendEnvelope(NetEnvelope envelope)
    {
        if (_writer == null)
        {
            return;
        }
        if (envelope.sequence <= 0)
        {
            envelope.sequence = ++_sequence;
        }
        _writer.WriteLine(NetWireCodec.ToWire(envelope));
    }

    public void PollIncoming()
    {
        if (_reader == null)
        {
            return;
        }
        try
        {
            while (_client?.GetStream().DataAvailable == true)
            {
                var line = _reader.ReadLine();
                if (line == null)
                {
                    Dispose();
                    break;
                }
                HandleLine(line);
            }
        }
        catch (IOException)
        {
            Dispose();
        }
    }

    private void HandleLine(string line)
    {
        if (!NetWireCodec.TryParseLine(line, out var env))
        {
            return;
        }
        if (env.type is NetMessageType.MATCH_PAUSE or NetMessageType.MATCH_RESUME)
        {
            var pause = MatchPauseCodec.FromJson(env.payloadJson);
            if (pause != null)
            {
                pause.paused = env.type == NetMessageType.MATCH_PAUSE;
                _pauseListener?.Invoke(pause);
            }
            return;
        }
        if (env.type != NetMessageType.STATE_DELTA || _stateListener == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(env.payloadJson))
        {
            return;
        }
        _stateListener(SaveCodec.FromJson(env.payloadJson));
    }

    private void SendHello()
    {
        if (_writer == null)
        {
            return;
        }
        var env = new NetEnvelope
        {
            type = NetMessageType.HELLO,
            sequence = 1,
            platform = PlatformId.DESKTOP_WIN,
            payloadJson = "guest",
        };
        _writer.WriteLine(NetWireCodec.ToWire(env));
    }

    public void Dispose()
    {
        try
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _client?.Close();
        }
        catch
        {
            // ignore
        }
        _reader = null;
        _writer = null;
        _client = null;
    }
}
