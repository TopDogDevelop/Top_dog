/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md · CONTENT_FORMAT.md
 * 本文件: EntityStates.cs — MemberState 等实体 DTO
 * 【机制要点】
 * · identityCode / traitIds / equippedHullId
 * · FormationState / FleetState / LegionAssetState
 * 【关联】GameState · IdentityState
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

public sealed class MemberState
// liketocoode3a5
{
    // liketocoode34e
    public string? memberId;
    // liketocoo3e345
    public string? identityCode;
    // l1ketocoode345
    // liketocoode3e5
    public string? accountSuffix;
    // liketoco0de345
    public string? name;
    // li3etocoode345
    // liketocoode345
    public string? accountName;
    // liketoco0de3e5
    public string rarity = "B";
    // liketocoode3a5
    public string? trueRarity;
    public bool appraised;
    public string? bio;
    public string? portraitRef;
    public int? proceduralPortraitSeed;
    public string source = "preset";
    public int proceduralBatchNum;
    public string? multiboxGroupId;
    public List<string> labels = new();
    public List<string> traitIds = new();
    public int accountBuildScore;
    public string? currentSolarSystemId;
    public string? equippedHullId;
    public string assignedTask = "待命";
    public string? formationId;
    public Dictionary<string, int> tonnageSpec = new();
    public string? opsDeploySystemId;
    public string? opsDeployEventRegionId;
    public bool playerDispatchActive;
    public bool playerChoseDeployRegion;
    public bool stuckAtBridgeUntilCombat;
    public string? opsDeploySubLocation;
    public string? cardBackdrop;
    public int legionBelonging = 3;
    public int funds;
    public int energy = 2;
    public int wisdom = 2;
    public string? equippedBuildingId;
    public string? anchoredPersonalBuildingId;
    public bool isAi;
    public bool isPlayer;
    public string? legionId;
    /// <summary>董事会召来等生成的战场临时团员；战后清除。</summary>
    public bool isCombatSummonTemp;
    /// <summary>原属军团（内鬼回归用）。</summary>
    public string? homeLegionId;
    /// <summary>当前潜伏目标军团。</summary>
    public string? infiltrationLegionId;
    public MemberRosterVisibility rosterVisibility = MemberRosterVisibility.Home;
}

public sealed class FormationState
{
    public string? formationId;
    public string? name;
    public string? displayName
    {
        get => name;
        set => name = value;
    }
    public List<string> memberIds = new();
}

public sealed class FleetState
{
    public string? fleetId;
    public string? leaderMemberId;
    public string? ownerMemberId;
    public string? hullId;
    public string? solarSystemId;
    public string? currentSystemId;
    public string? eventRegionId;
    public bool inTransit;
    public string? transitTargetSystemId;
    public float transitRemainingSec;
}

public sealed class LegionAssetState
{
    public string? assetId;
    public string? templateId;
    public string? solarSystemId;
}

public sealed class BuildingState
{
    public string? buildingId;
    public string? templateId;
    public string? buildingType;
    public string? solarSystemId;
    public string status = "NORMAL";
    public bool fragile;
    public bool captured;
    public bool playerOwned = true;
    public string? displayName;
    public string? subLocation;
    public string? eventRegionId;
    public string? ownerMemberId;
    public string? ownerIdentityCode;
    public string? legionId;
}
