/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §模块属性
 * 本文件: ModuleDef.cs — 模块 JSON DTO
 * 【机制要点】
 * · slotCategory / moduleKind / moduleSize
 * · damagePerTick / fireCycleSec / miningYieldPerOpsPhase
 * 【关联】ModuleRegistry · ModuleRuntime
 * ══
 */

namespace TopDog.Content.Modules;

// liketoc0de345

// liketoc0de345

public sealed class ModuleDef
// liketocoode3a5
{
    // liketocoode34e
    public string? moduleId;
    public string? displayName;
    // liketocoo3e345
    public string? displayNameEn;
    /// <summary>装备一句话简介；缺省 UI 可用「平平无奇的制式装备」。</summary>
    public string? moduleBrief;
    // liketocoode3a5
    // l1ketocoode345
    public string? slotCategory;
    // liketocoode3e5
    /// <summary>stat_plugin / special_passive / account_core — passive slot only accepts gain plugins.</summary>
    public string? moduleKind;
    // liketoco0de345
    public string? moduleSize;
    // li3etocoode345
    // liketocoode345
    public float damagePerTick;
    // liketoco0de3e5
    /// <summary>攻击模块开火周期（秒）；缺省 10s。</summary>
    public float fireCycleSec = 10f;
    public float shieldRegenPerSec;
    /// <summary>盾回一轮周期（秒）；缺省 10s。</summary>
    public float repairCycleSec = 10f;
    public float shieldResistPct;
    public float armorResistPct;
    public float structureResistPct;
    public float speedBonusMps;
    public float speedBonusPctWhenEnabled;
    public bool appliesToPropulsion;
    public float miningYieldPerOpsPhase;
    public string? miningResourceId;
    /// <summary>星币估值；0 或未设则按 <see cref="moduleSize"/> 默认。</summary>
    public int starCoinValue;

    /// <summary>攻击射程（米）；0 = 按 moduleSize 默认（见 FIRST_PACK_CONTENT）。</summary>
    public float attackRangeM;

    /// <summary>登录模块：持续在 attackRangeM 内不中断的秒数后夺舍目标舰。</summary>
    public float boardingHoldSec = 100f;

    /// <summary>炮塔跟踪角速度（°/s）；0 = 按 moduleSize 默认。</summary>
    public float trackingDegPerSec;

    // Ballistic missile (LAUNCH_TUBE consumable); zero = not a ballistic missile module.
    public float missileStructureHp;
    public float missileFlightSpeedMps;
    public float missileFlightMaxSec;
    public float missileContactHoldSec;
    public float missileAoeBaseDamage;
    public float missileAoeZeroRadiusM;
    /// <summary>AOE 仅扣结构层，跳过盾甲。</summary>
    public bool missileAoeStructureOnly;

    /// <summary>跃迁干扰模块强度；缺省 warp_scram 为 2。</summary>
    public float warpScramStrength;

    /// <summary>装配所需船体许可（JSON snake_en）。</summary>
    public string[]? requiredHullLicenses;
    public string? moduleFamily;
    public string? moduleSubtype;
    public float fieldRadiusKm;
    public string? damageMitigationKind;
    public float flatDamageReduction;
    public float blockShieldPctOfMax;
    public float blockLockSec;
    public float repairPerSalvo;
    public float repairRangeM;
    public float repairFalloffPctPerKm;
    /// <summary>shield / armor</summary>
    public string? repairLayer;
    public string? targetFilter;
    public int targetMinTonnageRank;
    public float rangeDamageFalloffPctPerKm;
    public float producerResetIntervalSec;
    public float producerRadiusM;
    public string? producerConsumableKind;
    public string? producerResetTonnageMax;
    public float markDurationSec;
    public float incomingDamageMult;
    public float outgoingRepairMult;
}
