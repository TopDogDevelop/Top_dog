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
}
