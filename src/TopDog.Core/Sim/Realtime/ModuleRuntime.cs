using TopDog.Content.Modules;
using TopDog.Content.Ships;

namespace TopDog.Sim.Realtime;

public static class ModuleRuntime
{
    public static void ApplyToUnit(BattlefieldUnit unit, HullDef hull, ModuleRegistry modules)
    {
        if (unit == null || hull == null)
        {
            return;
        }

        unit.tonnageClass = hull.tonnageClass;
        unit.shieldMax = hull.shieldHp;
        unit.armorMax = hull.armorHp;
        unit.structureMax = hull.structureHp;
        unit.shieldHp = hull.shieldHp;
        unit.armorHp = hull.armorHp;
        unit.structureHp = hull.structureHp;
        unit.maxSpeedMps = Math.Max(80f, hull.baseSpeedMps);
        unit.accelMps2 = Math.Max(10f, hull.baseAccelMps2 > 0f ? hull.baseAccelMps2 : hull.baseSpeedMps * 0.05f);

        var dps = 25f;
        var range = 6000f;
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod == null)
            {
                continue;
            }
            if (mod.damagePerTick > 0f)
            {
                dps += mod.damagePerTick * 0.15f;
            }
            if (mod.shieldRegenPerSec > 0f)
            {
                unit.shieldMax += mod.shieldRegenPerSec * 2f;
            }
            if (string.Equals(mod.slotCategory, "ATTACK", StringComparison.Ordinal))
            {
                range = Math.Max(range, 8000f);
            }
        }

        unit.damagePerSec = dps;
        unit.attackRangeM = range;
        unit.shieldHp = Math.Min(unit.shieldHp, unit.shieldMax);
    }
}
