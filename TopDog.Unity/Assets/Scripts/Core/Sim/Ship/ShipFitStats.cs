using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Ship;

public sealed class ShipFitStats
{
    public float shieldHp;
    public float armorHp;
    public float structureHp;
    public float shieldRegenPerSec;
    public float shieldResistPct;
    public float armorResistPct;
    public float structureResistPct;
    public float dps;
    public float baseSpeedMps;
    public float fittedSpeedMps;
    public string? activePropulsionLabel;

    public static ShipFitStats Compute(
        HullDef? hull,
        IReadOnlyDictionary<string, string>? fitted,
        ModuleRegistry? modules,
        GameState? state = null,
        MemberState? member = null)
    {
        var s = new ShipFitStats();
        if (hull == null)
        {
            return s;
        }
        s.shieldHp = hull.shieldHp;
        s.armorHp = hull.armorHp;
        s.structureHp = hull.structureHp;
        s.shieldRegenPerSec = hull.shieldRegenPerSec;
        s.shieldResistPct = hull.shieldResistPct;
        s.armorResistPct = hull.armorResistPct;
        s.structureResistPct = hull.structureResistPct;
        s.baseSpeedMps = hull.baseSpeedMps;
        s.fittedSpeedMps = hull.baseSpeedMps;

        if (fitted == null || modules == null)
        {
            return s;
        }
        var activePropSlot = state != null && member?.memberId != null
            ? state.memberActivePropulsionSlot.GetValueOrDefault(member.memberId)
            : null;

        foreach (var entry in fitted)
        {
            var mod = modules.Find(entry.Value);
            if (mod == null)
            {
                continue;
            }
            if (mod.damagePerTick > 0f)
            {
                s.dps += mod.damagePerTick * HullBonusSummary.AttackDamageMultiplier(hull, mod);
            }
            var regenMult = entry.Key.StartsWith("def_", StringComparison.Ordinal)
                ? HullBonusSummary.DefenseRegenMultiplier(hull)
                : 1f;
            s.shieldRegenPerSec += mod.shieldRegenPerSec * regenMult;
            s.shieldResistPct = CombineResist(s.shieldResistPct, mod.shieldResistPct);
            s.armorResistPct = CombineResist(s.armorResistPct, mod.armorResistPct);
            s.structureResistPct = CombineResist(s.structureResistPct, mod.structureResistPct);
            s.fittedSpeedMps += mod.speedBonusMps;

            var propulsionActive = mod.appliesToPropulsion
                && (activePropSlot == null || activePropSlot.Equals(entry.Key, StringComparison.Ordinal));
            if (propulsionActive && mod.speedBonusPctWhenEnabled > 0f)
            {
                s.fittedSpeedMps += hull.baseSpeedMps * mod.speedBonusPctWhenEnabled;
                s.activePropulsionLabel = ModuleRegistry.Bilingual(mod);
            }
        }
        return s;
    }

    private static float CombineResist(float bas, float add)
    {
        if (add <= 0f)
        {
            return bas;
        }
        var remain = 1f - bas / 100f;
        return (1f - remain * (1f - add / 100f)) * 100f;
    }
}
