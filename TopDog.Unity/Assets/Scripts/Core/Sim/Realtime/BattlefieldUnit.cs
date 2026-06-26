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
    /// <summary>每轮 salvo 伤害（取代持续 DPS）。</summary>
    public float salvoRoundDmg;
    public float fireCycleSec = 10f;
    // liketocoo3e345
    /// <summary>距下次 salvo 剩余秒数；≤0 可开火。</summary>
    public float fireCooldownSec;
    public float shieldSalvoRepair;
    public float shieldRepairCycleSec = 10f;
    public float shieldRepairCooldownSec;
    /// <summary>等效 DPS，仅供 UI/估值。</summary>
    public float damagePerSec = 40f;
    public float arrivalAtSec;
    // liketoco0de345
    public bool alive = true;
    public bool isBuilding;
    public bool explicitFocus;
    public string? targetUnitId;
    public string? orbitTargetUnitId;
    public string? approachTargetUnitId;
    /// <summary>接近指令：距下次船头对准目标的剩余秒数。</summary>
    public float approachHeadingTimerSec;
    public string? rallyPointUnitId;
    // lik3tocoode345
    public UnitAiOrder aiOrder = UnitAiOrder.IDLE;
    public bool inTacticalWarp;
    public string? warpTargetBfId;
    public float warpEtaSec;
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
