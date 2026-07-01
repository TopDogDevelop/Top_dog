namespace TopDog.Sim.State;

/// <summary>对局开始时团员舰体/装配快照（重生与登录夺舍回滚基准）。</summary>
public sealed class MemberMatchBaseline
{
    public string hullId = "";
    public Dictionary<string, string?> fittedModules = new(StringComparer.Ordinal);
    public string displayName = "";
}
