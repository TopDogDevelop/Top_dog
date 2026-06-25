using System.Net;

using System.Net.Sockets;

using System.Text;

using TopDog.App;

using TopDog.Net;

using TopDog.Net.Protocol;

using TopDog.Sim.Persist;

using TopDog.Sim.Realtime;



namespace TopDog.Net.Host;



/// <summary>Authoritative host: apply guest envelopes, tick sim, broadcast state snapshots.</summary>

public sealed class NetSessionHost : IDisposable

{

    private readonly int _port;

    private readonly object _ioLock = new();

    private TcpListener? _listener;

    private TcpClient? _client;

    private NetworkStream? _stream;

    private StreamReader? _reader;

    private StreamWriter? _writer;

    private long _sequence = 1;

    private SimulationCore? _core;

    private bool _running;

    private bool _matchPaused;

    private MatchPausePayload? _activePause;

    private string? _guestLegionId;



    public event Action<MatchPausePayload>? MatchPauseChanged;



    public NetSessionHost(int port) => _port = port;



    public int Port => _port;



    public bool MatchPaused => _matchPaused;



    public MatchPausePayload? ActivePause => _activePause;



    public int ActivePort

    {

        get

        {

            lock (_ioLock)

            {

                return _listener?.LocalEndpoint is IPEndPoint ep ? ep.Port : _port;

            }

        }

    }



    public bool IsClientConnected

    {

        get

        {

            lock (_ioLock)

            {

                return _client?.Connected == true;

            }

        }

    }



    public void Bind(SimulationCore core) => _core = core;



    public void Start()

    {

        if (_running)

        {

            return;

        }

        _listener = new TcpListener(IPAddress.Any, _port);

        _listener.Start();

        _running = true;

    }



    public void AcceptPending()

    {

        if (!_running || _listener == null)

        {

            return;

        }

        lock (_ioLock)

        {

            if (_client != null)

            {

                return;

            }

            if (!_listener.Pending())

            {

                return;

            }

            _client = _listener.AcceptTcpClient();

            _stream = _client.GetStream();

            _reader = new StreamReader(_stream, Encoding.UTF8);

            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

            SendHelloAck();

        }

    }



    public void Poll(float dtSec)

    {

        if (!_running || _core == null)

        {

            return;

        }

        AcceptPending();

        ReadIncoming();

        if (!_matchPaused)

        {

            _core.Tick(dtSec);

            if (IsClientConnected)

            {

                BroadcastState();

            }

        }

    }



    /// <summary>Host-local human pause toggle (authoritative).</summary>

    public void ApplyHostPause(MatchPausePayload payload)

    {

        if (!MatchPauseCodec.IsHumanInitiator(payload))

        {

            return;

        }

        SetMatchPause(payload);

    }



    private void SetMatchPause(MatchPausePayload payload)

    {

        if (!MatchPauseCodec.IsHumanInitiator(payload))

        {

            return;

        }

        _matchPaused = payload.paused;

        _activePause = payload.paused ? payload : null;

        if (IsClientConnected)

        {

            BroadcastPause(payload);

        }

        MatchPauseChanged?.Invoke(payload);

    }



    private void ReadIncoming()

    {

        lock (_ioLock)

        {

            if (_reader == null)

            {

                return;

            }

            try

            {

                while (_stream?.DataAvailable == true)

                {

                    var line = _reader.ReadLine();

                    if (line == null)

                    {

                        DisconnectClient();

                        break;

                    }

                    ApplyEnvelope(line);

                }

            }

            catch (IOException)

            {

                DisconnectClient();

            }

        }

    }



    private void ApplyEnvelope(string line)

    {

        if (_core == null)

        {

            return;

        }

        if (!NetWireCodec.TryParseLine(line, out var env))

        {

            if (!string.IsNullOrWhiteSpace(line))

            {

                _core.SubmitCommand(line.Trim());

            }

            return;

        }

        switch (env.type)

        {

            case NetMessageType.HELLO:

                if (!string.IsNullOrWhiteSpace(env.payloadJson)
                    && env.payloadJson.TrimStart().StartsWith('{'))
                {
                    try
                    {
                        var hello = System.Text.Json.JsonSerializer.Deserialize<GuestHelloPayload>(
                            env.payloadJson, Foundation.Json.TopDogJson.Options);
                        if (!string.IsNullOrWhiteSpace(hello?.legionId))
                        {
                            _guestLegionId = hello.legionId;
                        }
                    }
                    catch
                    {
                        // legacy plain "guest"
                    }
                }

                SendHelloAck();

                break;

            case NetMessageType.COMMAND_SUBMIT:

                if (!_matchPaused)

                {

                    var (legionId, cmdLine) = CommandSubmitCodec.Parse(env.payloadJson);

                    if (!string.IsNullOrWhiteSpace(legionId))
                    {
                        _guestLegionId ??= legionId;
                    }

                    _core.SubmitCommand(cmdLine, legionId);

                }

                break;

            case NetMessageType.TACTICAL_INPUT:

                if (!_matchPaused && !string.IsNullOrEmpty(env.payloadJson))

                {

                    var sample = System.Text.Json.JsonSerializer.Deserialize<PossessionInputSample>(

                        env.payloadJson, Foundation.Json.TopDogJson.Options);

                    if (sample != null)

                    {

                        _core.ApplyPossessionInput(sample);

                    }

                }

                break;

            case NetMessageType.MATCH_PAUSE:

            case NetMessageType.MATCH_RESUME:

                var pause = MatchPauseCodec.FromJson(env.payloadJson);

                if (pause != null)

                {

                    pause.paused = env.type == NetMessageType.MATCH_PAUSE;

                    SetMatchPause(pause);

                }

                break;

        }

    }



    private void BroadcastState()

    {

        if (_core == null)

        {

            return;

        }

        lock (_ioLock)

        {

            if (_writer == null)

            {

                return;

            }

            var env = new NetEnvelope

            {

                type = NetMessageType.STATE_DELTA,

                sequence = ++_sequence,

                platform = PlatformId.DESKTOP_WIN,

                payloadJson = SaveCodec.ToJson(
                    string.IsNullOrWhiteSpace(_guestLegionId)
                        ? _core.State
                        : NetSnapshotPartition.ForGuest(_core.State, _guestLegionId)),

            };

            _writer.WriteLine(NetWireCodec.ToWire(env));

        }

    }



    private void BroadcastPause(MatchPausePayload payload)

    {

        lock (_ioLock)

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

    }



    private void SendHelloAck()

    {

        if (_writer == null)

        {

            return;

        }

        var env = new NetEnvelope

        {

            type = NetMessageType.HELLO_ACK,

            sequence = ++_sequence,

            platform = PlatformId.DESKTOP_WIN,

            payloadJson = "{\"ok\":true}",

        };

        _writer.WriteLine(NetWireCodec.ToWire(env));

    }



    private void DisconnectClient()

    {

        _guestLegionId = null;

        try

        {

            _reader?.Dispose();

            _writer?.Dispose();

            _stream?.Dispose();

            _client?.Close();

        }

        catch

        {

            // ignore

        }

        _reader = null;

        _writer = null;

        _stream = null;

        _client = null;

        _matchPaused = false;

        _activePause = null;

    }



    public void Dispose()

    {

        _running = false;

        DisconnectClient();

        _listener?.Stop();

        _listener = null;

    }

}


