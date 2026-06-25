namespace TopDog.Net.Protocol;

public sealed class NetEnvelope
{
    public int protocolVersion = 1;
    public NetMessageType type;
    public long sequence;
    public string? roomId;
    public PlatformId platform;
    public string? payloadJson;
}
