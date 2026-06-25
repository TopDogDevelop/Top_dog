using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TopDog.Lobby;

public static class LocalNetworkUtil
{
    /// <summary>First non-loopback IPv4, or 127.0.0.1 if none found.</summary>
    public static string LocalIpv4()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(ua.Address))
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
