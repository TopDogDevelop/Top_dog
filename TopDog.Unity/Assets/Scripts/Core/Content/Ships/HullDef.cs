namespace TopDog.Content.Ships;

public sealed class HullDef
{
    public string? hullId;
    public string? displayName;
    public string? displayNameEn;
    public string? tonnageClass;
    public float shieldHp = 4000f;
    public float armorHp = 4000f;
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
