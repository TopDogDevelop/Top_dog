using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TopDog.Lobby;

namespace TopDog.Net.Lan;

/// <summary>UDP LAN room beacon (TOP_DOG_LAN). Host broadcasts; all instances listen and merge peers.</summary>
public sealed class LanLobbyBeacon : IDisposable
{
    private readonly CustomLobbyState _lobby;
    private readonly string _localIp;
    private int _running;
    private readonly Dictionary<string, PeerAnnouncement> _peers = new();
    private readonly object _peerLock = new();
    private UdpClient? _socket;
    private Thread? _rxThread;
    private Thread? _txThread;

    public LanLobbyBeacon(CustomLobbyState lobby)
    {
        _lobby = lobby;
        _localIp = LocalNetworkUtil.LocalIpv4();
    }

    public IReadOnlyDictionary<string, PeerAnnouncement> Peers
    {
        get
        {
            lock (_peerLock)
            {
                return new Dictionary<string, PeerAnnouncement>(_peers);
            }
        }
    }

    public string LocalIp => _localIp;

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return;
        }
        _socket = new UdpClient(_lobby.lanPort);
        _socket.EnableBroadcast = true;
        _socket.Client.ReceiveTimeout = 500;

        _rxThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "topdog-lan-rx" };
        _rxThread.Start();
        _txThread = new Thread(BroadcastLoop) { IsBackground = true, Name = "topdog-lan-tx" };
        _txThread.Start();
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _running, 0, 1) == 0)
        {
            return;
        }
        try
        {
            _socket?.Close();
        }
        catch
        {
            // ignore
        }
        _socket = null;
        _rxThread?.Interrupt();
        _txThread?.Interrupt();
    }

    /// <summary>Merge discovered LAN humans into lobby player list.</summary>
    public void SyncDiscoveredHumans()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        List<PeerAnnouncement> snapshot;
        lock (_peerLock)
        {
            snapshot = _peers.Values.ToList();
        }
        foreach (var peer in snapshot)
        {
            if (now - peer.lastSeenMs > 8000)
            {
                continue;
            }
            if (peer.hostIp == null || peer.hostIp == _localIp)
            {
                continue;
            }
            if (FindHumanByIp(peer.hostIp) != null)
            {
                continue;
            }
            if (_lobby.players.Count >= _lobby.maxPlayers)
            {
                continue;
            }
            _lobby.players.Add(new LobbyPlayer
            {
                kind = LobbyPlayerKind.HUMAN,
                displayName = peer.hostIp,
                remoteHostIp = peer.hostIp,
                host = false,
                local = false,
            });
        }
    }

    private LobbyPlayer? FindHumanByIp(string ip)
    {
        foreach (var p in _lobby.players)
        {
            if (p.kind == LobbyPlayerKind.HUMAN && ip == p.remoteHostIp)
            {
                return p;
            }
            if (p.kind == LobbyPlayerKind.HUMAN && p.local && ip == _localIp)
            {
                return p;
            }
            if (p.kind == LobbyPlayerKind.HUMAN && ip == p.displayName)
            {
                return p;
            }
        }
        return null;
    }

    private void ReceiveLoop()
    {
        var buf = new byte[2048];
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
                HandlePacket(msg, remote.Address.ToString());
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // continue
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                if (_running == 0)
                {
                    break;
                }
            }
        }
    }

    private void HandlePacket(string msg, string fromIp)
    {
        var joiner = LanProtocol.ParseJoinerIp(msg);
        if (joiner != null)
        {
            LanProtocol.ApplyJoinerToLobby(_lobby, joiner, _localIp);
            return;
        }
        if (!msg.StartsWith(LanProtocol.RoomMagic + "|", StringComparison.Ordinal))
        {
            return;
        }
        var peer = LanProtocol.ParseRoomBeacon(msg, fromIp);
        if (peer?.hostIp == null || peer.hostIp == _localIp)
        {
            return;
        }
        peer.lastSeenMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_peerLock)
        {
            _peers[peer.hostIp] = peer;
        }
    }

    private void BroadcastLoop()
    {
        while (_running == 1)
        {
            try
            {
                var payload = BuildPayload();
                var data = Encoding.UTF8.GetBytes(payload);
                var socket = _socket;
                if (socket != null)
                {
                    socket.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, _lobby.lanPort));
                }
            }
            catch
            {
                // retry
            }
            try
            {
                Thread.Sleep(2000);
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    private string BuildPayload()
    {
        var mapId = _lobby.mapPath != null ? Path.GetFileName(_lobby.mapPath) : "";
        var humans = 0;
        foreach (var p in _lobby.players)
        {
            if (p.kind == LobbyPlayerKind.HUMAN)
            {
                humans++;
            }
        }
        return LanProtocol.BuildRoomPayload(
            _lobby.roomId,
            _localIp,
            humans,
            mapId,
            _lobby.lanPort);
    }
}
