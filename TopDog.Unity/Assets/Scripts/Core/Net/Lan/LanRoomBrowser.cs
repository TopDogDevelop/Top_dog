using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TopDog.Lobby;

namespace TopDog.Net.Lan;

/// <summary>Listen-only LAN room discovery for the join-game browser.</summary>
public sealed class LanRoomBrowser : IDisposable
{
    private readonly int _port;
    private int _running;
    private readonly Dictionary<string, PeerAnnouncement> _rooms = new();
    private readonly object _roomLock = new();
    private UdpClient? _socket;
    private Thread? _rxThread;

    public LanRoomBrowser(int port)
    {
        _port = port;
    }

    public LanRoomBrowser() : this(CustomLobbyState.DefaultLanPort)
    {
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return;
        }
        _socket = new UdpClient(_port);
        _socket.EnableBroadcast = true;
        _socket.Client.ReceiveTimeout = 500;
        _rxThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "topdog-lan-browse" };
        _rxThread.Start();
    }

    /// <summary>Active rooms, newest ping first.</summary>
    public List<PeerAnnouncement> ActiveRooms()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_roomLock)
        {
            return _rooms.Values
                .Where(p => now - p.lastSeenMs <= 8000)
                .OrderByDescending(p => p.lastSeenMs)
                .ToList();
        }
    }

    public void PruneStale()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_roomLock)
        {
            var stale = _rooms.Where(kv => now - kv.Value.lastSeenMs > 8000).Select(kv => kv.Key).ToList();
            foreach (var key in stale)
            {
                _rooms.Remove(key);
            }
        }
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
    }

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
                var peer = LanProtocol.ParseRoomBeacon(msg, remote.Address.ToString());
                if (peer?.hostIp != null)
                {
                    peer.lastSeenMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    lock (_roomLock)
                    {
                        _rooms[peer.hostIp] = peer;
                    }
                }
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
}
