namespace TopDog.Sim.Combat;

public enum CombatPrepStep
{
    CHOOSE_MODE,
    CHOOSE_STANCE,
    SHOW_RESULT,
}

public enum CombatResolveMode
{
    AUTO,
    REALTIME,
}

public enum CombatSubtype
{
    HARVEST,
    COUNTER_HARVEST,
    BUILDING_ASSAULT,
}

public sealed class CombatRosterLine
{
    public string? memberId;
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

public sealed class OpponentHarvestOp
{
    public string? opId;
    public string? targetSystemId;
    public string? capturedFormationId;
    public string? capturedMemberId;
    public float etaSec;
}

/// <summary>AI 军团待攻建筑（交战编译时写入 <see cref="CombatQueueEntry"/> 攻/守军团）。</summary>
public sealed class AiPendingAssaultOp
{
    public string? attackerLegionId;
    public string? buildingId;
}

/// <summary>玩家发起建筑约战（运营底栏 · 约战态星系内可攻任意个堡/军堡）。</summary>
public sealed class PlayerPendingAssaultOp
{
    public string? attackerLegionId;
    public string? systemId;
    public string? buildingId;
}
