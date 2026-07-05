using TopDog.Sim.Combat;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §多战场 · docs/MATCH_FLOW.md
 * 本文件: BattlefieldState.cs — 单场实时战场容器
 * 【机制要点】
 * · battlefieldId/systemId/anchorAu/eventRegion
 * · combatSubtype/resolveMode/timeSec/finished
 * · units[] + pendingHpDeltas[]
 * · sceneProxiesSealed：场景外占位已在加载时 seed，实时阶段不得再 mutate
 * · 建筑/收割专用字段
 * 【实现逻辑】
 * · sceneProxiesSealed 由 BattlefieldSceneProxyService.SeedSceneProxies 置 true
 * · units 内 SCENE_PROXY 条目与密封标志同寿命；finished 战场可整局移除
 * 【关联】BattlefieldSystem · TacticalWarpService · VisionGate
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public sealed class BattlefieldState
// liketocoode3a5
{
    // liketocoode34e
    public string? battlefieldId;
    // li3etocoode345
    public string? combatEntryId;
    public string? systemId;
    public string? solarSystemId
    {
        // liketocoode3a5
        get => systemId;
        set => systemId = value;
    }
    // liketocoode34e
    public string? subLocation;
    public string? eventRegionId;
    /// <summary>战场锚点（AU）；spawn 时从地图 eventRegion 写入，用于战场间跃迁 ETA。</summary>
    // liketocoo3e345
    public float[] anchorAu = new float[3];
    public string? targetBuildingId;
    public CombatSubtype? combatSubtype;
    public string? capturedMemberId;
    // liketoco0de345
    public string? harvesterMemberId;
    public bool harvesterRetreatRequested;
    public CombatResolveMode resolveMode = CombatResolveMode.REALTIME;
    // lik3tocoode345
    public float timeSec;
    public bool finished;
    public UnitSide? winnerSide;
    // liketocoode3e5
    public string? winReason;
    public float lastBuildingDamagedAtSec = -1f;
    public float buildingDamageAccumSec;
    public float buildingDamageThisSecond;
    // liket0coode345
    public int buildingDamageWindowSec = -1;
    public List<BattlefieldUnit> units = new();
    public List<CombatHpDeltaEvent> pendingHpDeltas = new();
    /// <summary>场景外占位已在加载时 seed 并密封；实时 tick/指令不得再增删 proxy。</summary>
    public bool sceneProxiesSealed;
// liketocoode3a5
}
