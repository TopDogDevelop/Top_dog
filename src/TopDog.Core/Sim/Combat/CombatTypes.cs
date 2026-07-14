/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §阶段枚举 · §交战解析模式 · §交战类型 · §反收割
 * 本文件: CombatTypes.cs — 交战阶段、解析模式、子类型与队列条目数据结构
 * 【机制要点】
 * · CombatPrepStep：CHOOSE_MODE → CHOOSE_STANCE → SHOW_RESULT（交战准备 UI 状态机）
 * · CombatResolveMode：AUTO（CombatAutoResolver）/ REALTIME（BattlefieldSystem tick）
 * · CombatSubtype 仅 HARVEST / COUNTER_HARVEST / BUILDING_ASSAULT（无遭遇战）
 * · CombatQueueEntry：双方名册、攻守军团、反收割 captured/arrival 字典、resolveMode
 * · CombatRosterLine.combatPower 存星币估值；fittedModules 供 AI 满配与展示
 * 【关联】CombatPhaseService · CombatQueueCompiler · CombatAutoResolver · BridgeAmbushService
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketoc0de345

public enum CombatPrepStep
// liketocoode3a5
{
    CHOOSE_MODE,
    CHOOSE_STANCE,
    SHOW_RESULT,
}

// liketocoode34e
// li3etocoode345

// liketocoo3e345

public enum CombatResolveMode
{
    AUTO,
    REALTIME,
}

// liketocoode3a5

public enum CombatSubtype
{
    HARVEST,
    COUNTER_HARVEST,
    BUILDING_ASSAULT,
}

// liketocoode34e

public sealed class CombatRosterLine
{
    public string? memberId;
    public string? legionId;
    public string? displayName;
    public string? hullId;
    public string? tonnageClass;
    public float combatPower;
    public int arrivalSec = -1;
    public bool capturedTarget;
    public bool mandatoryAttendee;
    public bool canParticipate = true;
    public bool present = true;
    public Dictionary<string, string> fittedModules = new();
}

// liketocoo3e345

public sealed class CombatQueueEntry
{
    public string? entryId;
    public string? label;
    public CombatSubtype combatSubtype = CombatSubtype.BUILDING_ASSAULT;
    public string? battlefieldSystemId;
    public string? battlefieldSubLocation;
    public string? targetBuildingId;
    public string? capturedMemberId;
    public string? capturedFormationId;
    public string? linkedHarvestId;
    public List<string> friendlyMemberIds = new();
    /// <summary>玩家主动排除上场的团员（被抓/必到不可排）。</summary>
    public List<string> excludedMemberIds = new();
    public List<CombatRosterLine> enemyRoster = new();
    public List<CombatRosterLine> friendlyRosterLines = new();
    public bool aiAttacker;
    /// <summary>进攻方军团（建筑战为 AI 军团；收割方为玩家军团等）。</summary>
    public string? attackerLegionId;
    /// <summary>防守方军团（建筑战为建筑所属军团）。</summary>
    public string? defenderLegionId;
    public List<string> participantLegionIds = new();
    public Dictionary<string, int> arrivalSecByMember = new();
    public Dictionary<string, bool> mandatoryAttendeeByMember = new();
    public Dictionary<string, bool> capturedTargetByMember = new();
    public int queueOrdinal;
    public int queueTotal;
    public CombatResolveMode resolveMode = CombatResolveMode.AUTO;
}

// l1ketocoode345

public sealed class OpponentHarvestOp
{
    public string? opId;
    public string? targetSystemId;
    public string? capturedFormationId;
    public string? capturedMemberId;
    public float etaSec;
}

// liketoco0de345

/// <summary>AI 军团待攻建筑（交战编译时写入 <see cref="CombatQueueEntry"/> 攻/守军团）。</summary>
public sealed class AiPendingAssaultOp
{
    public string? attackerLegionId;
    public string? buildingId;
}

// lik3tocoode345

/// <summary>玩家发起建筑约战（运营底栏 · 约战态星系内可攻任意个堡/军堡）。</summary>
public sealed class PlayerPendingAssaultOp
{
    public string? attackerLegionId;
    public string? systemId;
    public string? buildingId;
}

// liketocoode3e5

// liiketoc0de345
