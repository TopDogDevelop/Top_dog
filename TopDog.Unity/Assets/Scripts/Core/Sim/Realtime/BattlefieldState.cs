using TopDog.Sim.Combat;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §多战场 · docs/MATCH_FLOW.md
 * 本文件: BattlefieldState.cs — 单场实时战场容器
 * 【机制要点】
 * · battlefieldId/systemId/anchorAu/eventRegion
 * · combatSubtype/resolveMode/timeSec/finished
 * · units[] + pendingHpDeltas[] + pendingCombatFx[]
 * · sceneProxiesSealed：场景外占位已在加载时 seed，实时阶段不得再 mutate
 * · focusFire*：显式集火顺序槽（TACTICAL_WARP §4c）
 * · 建筑/收割专用字段
 * 【实现逻辑】
 * · sceneProxiesSealed 由 BattlefieldSceneProxyService.SeedSceneProxies 置 true
 * · units 内 SCENE_PROXY 条目与密封标志同寿命；finished 战场可整局移除
 * 【关联】BattlefieldSystem · FocusFireSequencer · TacticalWarpService · VisionGate
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
    /// <summary>特效只读事件（Client Drain；不改结算）。</summary>
    public List<CombatFxEvent> pendingCombatFx = new();
    /// <summary>场景外占位已在加载时 seed 并密封；实时 tick/指令不得再增删 proxy。</summary>
    public bool sceneProxiesSealed;
    /// <summary>不因歼敌自动结束（开场配置；任意战场可开）。</summary>
    public bool disableAutoVictory;
    /// <summary>本场景时间膨胀系数（1=正常）；见 MAP_SPEC §4.3 / FLEET_SCALE_10K。</summary>
    public float timeDilation = 1f;
    public int entityBudget = 12_000;
    public float tickBudgetMs = 16f;
    public float minTimeDilation = 0.1f;
    /// <summary>本 tick 重建的邻域索引（可选缓存）。</summary>
    public BattlefieldSpatialHash? spatialHash;
    public int maxLiveMissiles = 0;
    /// <summary>密舰队单位处理轮转游标（BattlefieldScalePolicy）。</summary>
    public int unitProcessLodCursor;
    /// <summary>spatialHash 重建计数。</summary>
    public int spatialHashTickCounter;
    /// <summary>来源化效果与动态启用配额下次 1 Hz 结算时刻。</summary>
    public float runtimeEffectNextTickSec;
    /// <summary>固定与随舰区域拦截发射源；由跃迁舰主动查询。</summary>
    public List<InterdictionFieldSource> interdictionSources = new();
    /// <summary>显式集火顺序槽目标（TACTICAL_WARP §4c）。</summary>
    public string? focusFireTargetId;
    /// <summary>集火开火队列（下令舰 unitId 稳定序）。</summary>
    public List<string>? focusFireQueue;
    /// <summary>当前应开火的队列下标。</summary>
    public int focusFireCursor;
    /// <summary>上一发成功结算的 sim 时间；同 tick 防多发。</summary>
    public float focusFireLastVolleySimSec = float.NegativeInfinity;
// liketocoode3a5
}
