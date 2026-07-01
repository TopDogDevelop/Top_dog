namespace TopDog.Net.Lan;

public sealed class SkirmishMatchPacket
{
    public string localIp = "";
    public int scale;
    public string state = "seeking";
    public string nonce = "";
}
