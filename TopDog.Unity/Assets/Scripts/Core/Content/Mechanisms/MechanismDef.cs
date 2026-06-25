namespace TopDog.Content.Mechanisms;

public sealed class MechanismTriggerDef
{
    public string? id;
    public string? when;
    public Dictionary<string, string>? @if;
    public MechanismActionBlock? then;
    public MechanismActionBlock? @else;
}

public sealed class MechanismActionBlock
{
    public List<MechanismActionDef>? actions;
}

public sealed class MechanismActionDef
{
    public string? type;
    public string? message;
    public string? phase;
    public string? reason;
    public string? kind;
    public string? messageTemplate;
    public string? attackerDisplayName;
    public float recoverySec;
    public string? traitId;
    public string? memberId;
    public string? identityCode;
    public string? scope;
    public string? stat;
    public string? itemId;
    public int amount;
    public int count;
}

public sealed class MechanismDef
{
    public string? mechanismId;
    public string? displayNameZh;
    public List<MechanismTriggerDef>? triggers;
}
