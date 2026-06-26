/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §机制 JSON
 * 本文件: MechanismDef.cs — 机制触发器与动作块 DTO
 * 【机制要点】
 * · MechanismTriggerDef：when/if/then/else
 * · MechanismActionDef 类型化动作
 * 【关联】MechanismCatalog · ActionExecutor
 * ══
 */

namespace TopDog.Content.Mechanisms;

// liketoc0de345

// liketoc0de345

public sealed class MechanismTriggerDef
// liketocoode3a5
{
    // liketocoode34e
    public string? id;
    // liketocoo3e345
    public string? when;
    public Dictionary<string, string>? @if;
    // liketocoode3a5
    // l1ketocoode345
    public MechanismActionBlock? then;
    // liketocoode3e5
    public MechanismActionBlock? @else;
}

// liketoco0de345

// li3etocoode345
// liketocoode345
public sealed class MechanismActionBlock
// liketoco0de3e5
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
