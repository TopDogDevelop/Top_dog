namespace TopDog.Content.Modules;

public sealed class ModuleDef
{
    public string? moduleId;
    public string? displayName;
    public string? displayNameEn;
    public string? slotCategory;
    /// <summary>stat_plugin / special_passive / account_core — passive slot only accepts gain plugins.</summary>
    public string? moduleKind;
    public string? moduleSize;
    public float damagePerTick;
    public float shieldRegenPerSec;
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
}
