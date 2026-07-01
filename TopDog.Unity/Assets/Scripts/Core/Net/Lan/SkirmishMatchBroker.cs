using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TopDog.Lobby;

namespace TopDog.Net.Lan;

/// <summary>军团约战真人 UDP 匹配（无用户建房）。</summary>
public sealed class SkirmishMatchBroker : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (SkirmishMatchPacket packet, float seenAt)> _peers = new(StringComparer.Ordinal);
    private readonly List<SkirmishMatchPacket> _peerScratch = new();

    private readonly string _localIp;
    private readonly string _nonce = Guid.NewGuid().ToString("N")[..8];

    private int _running;
    private int _scale;
    private float _elapsedSec;
    private UdpClient? _socket;
    private Thread? _rxThread;
    private Thread? _txThread;
    private SkirmishMatchSnapshot _snapshot = new();

    public SkirmishMatchBroker()
    {
        _localIp = LocalNetworkUtil.LocalIpv4();
    }

    public string LocalIp => _localIp;

    public SkirmishMatchSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    public void StartSeeking(int scale)
    {
        Stop();
        _scale = scale;
        _elapsedSec = 0f;
        lock (_gate)
        {
            _peers.Clear();
            _snapshot = new SkirmishMatchSnapshot
            {
                Phase = SkirmishMatchPhase.Seeking,
                StatusMessage = "正在匹配局域网对手…",
            };
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return;
        }

        _socket = new UdpClient(SkirmishLobbyState.MatchUdpPort);
        _socket.EnableBroadcast = true;
        _socket.Client.ReceiveTimeout = 500;
        _rxThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "topdog-skirmish-match-rx" };
        _rxThread.Start();
        _txThread = new Thread(BroadcastLoop) { IsBackground = true, Name = "topdog-skirmish-match-tx" };
        _txThread.Start();
    }

    public void Tick(float dtSec)
    {
        if (_running == 0)
        {
            return;
        }

        _elapsedSec += dtSec;
        lock (_gate)
        {
            var nowSec = (float)Environment.TickCount / 1000f;
            var stale = new List<string>();
            foreach (var kv in _peers)
            {
                if (nowSec - kv.Value.seenAt > 4f)
                {
                    stale.Add(kv.Key);
                }
            }

            foreach (var key in stale)
            {
                _peers.Remove(key);
            }

            _peerScratch.Clear();
            foreach (var kv in _peers)
            {
                _peerScratch.Add(kv.Value.packet);
            }

            _snapshot = SkirmishMatchLogic.Evaluate(_localIp, _scale, _peerScratch, _elapsedSec);
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _running, 0, 1) == 0)
        {
            return;
        }

        try { _socket?.Close(); } catch { /* ignore */ }
        _socket = null;
        _rxThread?.Interrupt();
        _txThread?.Interrupt();
        lock (_gate)
        {
            _peers.Clear();
            _snapshot = new SkirmishMatchSnapshot();
        }
    }

    public void Dispose() => Stop();

    private void ReceiveLoop()
    {
        while (_running == 1)
        {
            try
            {
                var socket = _socket;
                if (socket == null)
                {
                    break;
                }

                IPEndPoint remote = new(IPAddress.Any, 0);
                var data = socket.Receive(ref remote);
                var msg = Encoding.UTF8.GetString(data);
                var packet = LanProtocol.ParseSkirmishMatch(msg);
                if (packet == null || packet.localIp == _localIp)
                {
                    continue;
                }

                lock (_gate)
                {
                    var seenAt = (float)Environment.TickCount / 1000f;
                    _peers[packet.localIp] = (packet, seenAt);
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) { }
            catch (ObjectDisposedException) { break; }
            catch when (_running == 0) { break; }
        }
    }

    private void BroadcastLoop()
    {
        while (_running == 1)
        {
            try
            {
                var payload = LanProtocol.BuildSkirmishMatchPayload(_localIp, _scale, "seeking", _nonce);
                var data = Encoding.UTF8.GetBytes(payload);
                _socket?.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, SkirmishLobbyState.MatchUdpPort));
            }
            catch { /* retry */ }

            try { Thread.Sleep(1000); }
            catch (ThreadInterruptedException) { break; }
        }
    }
}
