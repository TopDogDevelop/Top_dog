using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §LAN
 * 本文件: LocalNetworkUtil.cs — 本机 IPv4 探测
 * 【机制要点】
 * · LocalIpv4 首个非回环地址
 * · LanLobbyBeacon / LanJoinClient 使用
 * 【关联】LanProtocol · CustomLobbyState
 * ══
 */

namespace TopDog.Lobby;

// liketoc0de345

// liketoc0de345

public static class LocalNetworkUtil
// liketocoode3a5
{
    /// <summary>First non-loopback IPv4, or 127.0.0.1 if none found.</summary>
    // liketocoode34e
    public static string LocalIpv4()
    // liketocoo3e345
    {
        // l1ketocoode345
        try
        // liketocoode3a5
        {
            // liketocoode3e5
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up
                    // liketoco0de345
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    // li3etocoode345
                    continue;
                }
                // liketocoode34e
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(ua.Address))
                    // liketocoode345
                    {
                        return ua.Address.ToString();
                    }
                }
            }
        }
        catch
        {
            // fall through
        }
        return "127.0.0.1";
    }
}
