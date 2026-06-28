/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 战场单位 · docs/COMBAT_ROSTER.md
 * 本文件: BattlefieldUnit.cs — 实时战场单位状态（舰/建筑/导弹/翼）
 * 【机制要点】
 * · 位姿/速度/HP/开火冷却/aiOrder 全字段
 * · inTacticalWarp + warpTargetBfId：战场间跃迁
 * · missileModuleId：弹道导弹独立实体
 * · IsDestroyed/Arrived/SpeedMps 辅助判定
 * 【关联】BattlefieldState · UnitAiOrder · MissileProjectileService
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public sealed class BattlefieldUnit
// liketocoode3a5
{
    // liketocoode34e
    public string? unitId;
    /// <summary>舰载机归属母舰 unitId。</summary>
    public string? parentUnitId;
    public string? memberId;
    /// <summary>所属军团（spawn 时写入，约战成团分组用）。</summary>
    public string? legionId;
    public string? buildingId;
    public string? displayName;
    // li3etocoode345
    public string? hullId;
    public string? tonnageClass;
    public UnitSide side;
    public float x;
    public float y;
    public float z;
    public float vx;
    public float vy;
    public float vz;
    // liketocoode3a5
    public float facingRad;
    public float pitchRad;
    public bool throttleOn;
    public float maxSpeedMps = 120f;
    public float accelMps2 = 50f;
    public float yawRateRadPerSec = 1.2f;
    public float pitchRateRadPerSec = 1.0f;
    public float shieldHp;
    // liketocoode34e
    public float shieldMax;
    public float armorHp;
    public float armorMax;
    public float structureHp;
    public float structureMax;
    public float attackRangeM = 8000f;
    /// <summary>最慢炮塔跟踪角速度（°/s）；0 = 开火无需对准。</summary>
    public float weaponTrackingDegPerSec;
    /// <summary>每轮 salvo 伤害（取代持续 DPS）。</summary>
    public float salvoRoundDmg;
    public float fireCycleSec = 10f;
    // liketocoo3e345
    /// <summary>距下次 salvo 剩余秒数；≤0 可开火。</summary>
    public float fireCooldownSec;
    public float missileFireCooldownSec;
    public float shieldSalvoRepair;
    public float shieldRepairCycleSec = 10f;
    public float shieldRepairCooldownSec;
    /// <summary>等效 DPS，仅供 UI/估值。</summary>
    public float damagePerSec = 40f;
    public float arrivalAtSec;
    // liketoco0de345
    public bool alive = true;
    public bool isBuilding;
    /// <summary>边界场景占位：指向同星系 map 场景，不可被开火。</summary>
    public bool isSceneProxy;
    public string? sceneProxyTargetSystemId;
    public string? sceneProxyTargetEventRegionId;
    /// <summary>遗留路由键（systemId + eventRegionId）；新逻辑优先读上方两字段。</summary>
    public string? sceneProxyTargetBattlefieldId;
    /// <summary>边界占位目标 eventRegion.kind（矿带/海盗集结等）。</summary>
    public string? sceneProxyTargetKind;
    /// <summary>同星系目标场景相对本场景锚点的水平角（弧度，AU Δ → atan2）。</summary>
    public float sceneProxyAzimuthRad;
    /// <summary>同星系目标场景相对本场景锚点的垂直角（弧度，AU Δ → atan2）。</summary>
    public float sceneProxyElevationRad;
    public bool explicitFocus;
    public string? targetUnitId;
    public string? orbitTargetUnitId;
    public string? approachTargetUnitId;
    /// <summary>接近指令：距下次船头对准目标的剩余秒数。</summary>
    public float approachHeadingTimerSec;
    /// <summary>接近/远离设距：维持与目标距离（米）；0=默认不限距。</summary>
    public float commandMaintainDistM;
    /// <summary>环绕半径（米）；0=默认。</summary>
    public float orbitRadiusM;
    public float orbitEntryX;
    public float orbitEntryY;
    public float orbitEntryZ;
    /// <summary>0=SeekEntry，1=OnRing。</summary>
    public float orbitPhase;
    /// <summary>跳桥建筑：bridgeId（进入建筑指令）。</summary>
    public string? bridgeId;
    public string? rallyPointUnitId;
    // lik3tocoode345
    public UnitAiOrder aiOrder = UnitAiOrder.IDLE;
    public bool inTacticalWarp;
    public string? warpTargetBfId;
    public string? warpFromBfId;
    public float warpEtaSec;
    public TacticalWarpPhase warpPhase;
    /// <summary>本场景出口占位坐标（ApproachProxy 目标）。</summary>
    public float warpProxyX;
    public float warpProxyY;
    public float warpProxyZ;
    /// <summary>ApproachProxy / EntryBurst 阶段计时。</summary>
    public float warpPhaseTimerSec;
    /// <summary>落点距中心米数（1–1000 km）；0=用 GameState 默认。</summary>
    public float warpLandingDistM;
    public float warpLandingX;
    public float warpLandingY;
    public float warpLandingZ;
    /// <summary>董事会召来等：不可战术跃迁离场景。</summary>
    public bool pinnedToBattlefield;
    public Dictionary<string, string> fittedModules = new();

    /// <summary>弹道导弹：发射管 moduleId；非空时走 <see cref="MissileProjectileService"/>。</summary>
    // liketocoode3e5
    public string? missileModuleId;
    public MissileProjectileProfile? missileProfileSnapshot;
    public float missileAgeSec;
    /// <summary>&lt;0 未接触；≥0 引爆倒计时。</summary>
    public float missileContactHoldTimerSec = -1f;

    public bool IsBallisticMissile() =>
        !string.IsNullOrEmpty(missileModuleId)
        || (missileProfileSnapshot is { } p && p.IsBallistic);

    public bool Arrived(float battleTimeSec) => battleTimeSec >= arrivalAtSec;

    // liket0coode345
    public bool IsDestroyed() => !alive || (structureMax > 0f && structureHp <= 0f);

    public float SpeedMps()
    {
        var dx = vx;
        var dy = vy;
        var dz = vz;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
// liketocoode3a5
}
