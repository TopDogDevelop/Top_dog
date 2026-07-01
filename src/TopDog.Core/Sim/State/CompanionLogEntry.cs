namespace TopDog.Sim.State;

/// <summary>团员伴聊左栏输出（NARRATIVE）；不含发言人前缀。</summary>
public sealed class CompanionLogEntry
{
    public float tick;
    public string? memberId;
    public string text = "";
    public string channel = "reactive";
    public string? eventKey;
    public string? groupId;
    public string trustLevel = "NARRATIVE";
}
