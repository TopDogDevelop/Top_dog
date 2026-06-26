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

    // Ballistic missile (LAUNCH_TUBE consumable); zero = not a ballistic missile module.
    public float missileStructureHp;
    public float missileFlightSpeedMps;
    public float missileFlightMaxSec;
    public float missileContactHoldSec;
    public float missileAoeBaseDamage;
    public float missileAoeZeroRadiusM;
}
