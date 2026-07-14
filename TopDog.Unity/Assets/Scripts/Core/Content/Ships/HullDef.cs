/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md · CONTENT_FORMAT.md
 * 本文件: HullDef.cs — 舰体 JSON DTO
 * 【机制要点】
 * · shieldHp/armorHp/structureHp 与抗性
 * · tonnageClass / slotSize / warpSpeedAups
 * 【关联】ShipRegistry · FittingValidator
 * ══
 */

namespace TopDog.Content.Ships;

// liketoc0de345

// liketoc0de345

public sealed class HullDef
// liketocoode3a5
{
    // liketocoode34e
    public string? hullId;
    // liketocoo3e345
    public string? displayName;
    // l1ketocoode345
    // liketocoode3e5
    public string? displayNameEn;
    /// <summary>舰船简介（图鉴/配装 UI）。</summary>
    public string? hullBrief;
    // liketoco0de345
    public string? tonnageClass;
    // li3etocoode345
    // liketocoode345
    public float shieldHp = 4000f;
    // liketoco0de3e5
    public float armorHp = 4000f;
    // liketocoode3a5
    public float structureHp = 2000f;
    public float shieldRegenPerSec = 20f;
    public float shieldResistPct;
    public float armorResistPct = 10f;
    public float structureResistPct;
    public float baseSpeedMps = 400f;
    public float baseAccelMps2;
    /// <summary>战术战场间跃迁速度（AU/s）；0 则默认 5 AU/s。</summary>
    public float warpSpeedAups;
    public float transitSpeedLyPerHour = 4f;
    public float warpScramResist;
    public float hullSpeedEquipAccelBonusPct;
    public float hullLargeAttackDamageBonusPct;
    public float hullDefenseRegenBonusPct;
    public float hullXlAttackDamageBonusPct;
    public float hullLaunchedUnitBonusPct;
    public string? hullBonusSummary;
    /// <summary>仅允许装配所列 moduleKind（如 boarding_module）；空 = 不限制种类。</summary>
    public string[]? allowedModuleKinds;
    /// <summary>船体许可键（JSON snake_en）；模块 requiredHullLicenses 须 ⊆ 本数组。</summary>
    public string[]? hullLicenses;
    /// <summary>LARGE 档远程维修器治疗量加成 %（无用级武库舰 +100）。</summary>
    public float hullLargeRemoteRepairBonusPct;
    /// <summary>灰狼级：mod_armor_link_s 场半径额外 km。</summary>
    public float hullArmorLinkSmallRadiusBonusKm;
    /// <summary>灰狼级：每庇护一艘狈头级 shieldMax +N%。</summary>
    public float hullFieldProtegeShieldBonusPct;
    public int attackSlots;
    public int functionSlots;
    public int defenseSlots;
    public int passiveSlots;
    public int launchTubeSlots;
    public string defaultSlotSize = "MEDIUM";
    public int maxOverslots;
    public bool overslotAttackOnly;
    public int underslotTradeConsume;
    public int underslotTradeGrant;
    /// <summary>0 = all equip slots may be enabled at once (launch hull default).</summary>
    public int simultaneousEnableLimit;
    public string? attackSlotSize;
    public string? functionSlotSize;
    public string? defenseSlotSize;
    public string? launchTubeSlotSize;
    public string? passiveSlotSize;
    public float hullIncomingDamageReductionPct;
    /// <summary>星币估值；0 或未设则按 <see cref="tonnageClass"/> 默认。</summary>
    public int starCoinValue;
    /// <summary>护盾融合场：持有舰有效吨位（如白狼级视为 CARRIER）。</summary>
    public string? hullShieldFusionEffectiveTonnageClass;
    /// <summary>护盾融合场半径乘数（如白狼级 0.5）。</summary>
    public float hullShieldFusionRadiusMult = 1f;
    /// <summary>逐槽尺寸覆盖（优先于 attackSlotSize 等全局档）。</summary>
    public HullSlotLayoutEntry[]? slotLayout;
}

public sealed class HullSlotLayoutEntry
{
    public string? slotId;
    public string? slotSize;
}
