namespace TopDog.Net.Protocol;

public sealed class MultiplayerConfig
{
    public bool enabled;
    public string transport = "UDP_LAN";
    public int port = 7777;
    public int maxPlayers = 4;
    public string? modHash;
    public PlatformId hostPlatform;
}
