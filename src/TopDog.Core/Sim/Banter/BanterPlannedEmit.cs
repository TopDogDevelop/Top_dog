namespace TopDog.Sim.Banter;

/// <summary>伴聊计划输出：剧本随机与节奏排定后，按 <see cref="EmitAtSec"/> 投递。</summary>
public sealed class BanterPlannedEmit
{
    public float EmitAtSec;
    public string MemberId = "";
    public string Text = "";
    public string Channel = "idle";
    public string? EventKey;
    public string? GroupId;
}
