using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TopDog.Lobby;

namespace TopDog.Net.Lan;

/// <summary>Sends join requests to a LAN host (ephemeral socket; does not bind the lobby port).</summary>
public sealed class LanJoinClient : IDisposable
{
    private readonly string _hostIp;
    private readonly int _port;
    private readonly string _joinerIp = LocalNetworkUtil.LocalIpv4();
    private UdpClient? _socket;
    private Thread? _txThread;
    private volatile bool _running;

    public LanJoinClient(string hostIp, int port)
    {
        _hostIp = hostIp;
        _port = port;
    }

    public void Start()
    {
        if (_running)
        {
            return;
        }
        _running = true;
        _socket = new UdpClient();
        SendOnce();
        _txThread = new Thread(Loop) { IsBackground = true, Name = "topdog-lan-join" };
        _txThread.Start();
    }

    public void SendOnce()
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(LanProtocol.BuildJoinPayload(_joinerIp));
            if (_socket != null)
            {
                _socket.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(_hostIp), _port));
            }
            else
            {
                using var s = new UdpClient();
                s.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(_hostIp), _port));
            }
        }
        catch
        {
            // retry on next tick
        }
    }

    private void Loop()
    {
        while (_running)
        {
            SendOnce();
            try
            {
                Thread.Sleep(2500);
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _running = false;
        _txThread?.Interrupt();
        try
        {
            _socket?.Close();
        }
        catch
        {
            // ignore
        }
        _socket = null;
    }
}
