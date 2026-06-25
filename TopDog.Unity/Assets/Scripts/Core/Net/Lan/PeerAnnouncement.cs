namespace TopDog.Net.Lan;

public sealed class PeerAnnouncement
{
    public string? roomId;
    public string? hostIp;
    public string? hostName;
    public int playerCount = 1;
    public string? mapId;
    public long lastSeenMs;
}
