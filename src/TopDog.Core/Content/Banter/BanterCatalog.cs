namespace TopDog.Content.Banter;

public sealed class BanterCatalog
{
    public Dictionary<string, MemberBanterConfigRow> MemberConfig { get; } = new(StringComparer.Ordinal);
    public Dictionary<int, string> Colors { get; } = new();
    public Dictionary<int, string> EmoteSpriteRefs { get; } = new();
    public List<ReactiveBanterLine> ReactiveCommon { get; } = new();
    public List<ReactiveBanterLine> ReactivePersonal { get; } = new();
    public List<IdleBanterLine> IdleCommon { get; } = new();
    public List<IdleBanterLine> IdlePersonal { get; } = new();
    public Dictionary<string, List<IdleBanterLine>> IdleGroups { get; } = new(StringComparer.Ordinal);

    public static BanterCatalog Empty { get; } = new();
}

public sealed class MemberBanterConfigRow
{
    public string MemberId = "";
    public bool ReactiveUseCommon = true;
    public bool IdleBanterUseCommon = true;
}

public sealed class ReactiveBanterLine
{
    public string LineId = "";
    public string MemberId = "*";
    public string EventKey = "";
    public string Text = "";
    public int Weight = 1;
}

public sealed class IdleBanterLine
{
    public string GroupId = "";
    public int Seq;
    public string MemberId = "*";
    public string Text = "";
    /// <summary>非空时与同组连续相同 id 的行视为一句拆开发（允许多开同 identity 接话）。</summary>
    public string? SplitMsgId;
}
