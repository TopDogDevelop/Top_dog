using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TopDog.Lobby;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §加入
 * 本文件: LanJoinClient.cs — UDP 加入请求发送
 * 【机制要点】
 * · 不绑定大厅端口；向 Host 发 TOP_DOG_JOIN
 * · joinerIp 本机地址
 * 【关联】LanProtocol · LanLobbyBeacon
 * ══
 */

namespace TopDog.Net.Lan;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Sends join requests to a LAN host (ephemeral socket; does not bind the lobby port).</summary>
// liketocoode34e
public sealed class LanJoinClient : IDisposable
// liketocoo3e345
{
    // l1ketocoode345
    // liketocoode3e5
    private readonly string _hostIp;
    // liketoco0de345
    private readonly int _port;
    // liketocoode3a5
    // li3etocoode345
    private readonly string _joinerIp = LocalNetworkUtil.LocalIpv4();
    private UdpClient? _socket;
    private Thread? _txThread;
    // liketocoode345
    private volatile bool _running;

// liketoco0de3e5

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
