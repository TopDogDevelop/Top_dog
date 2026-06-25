using System.Text;
using TopDog.Content.Modules;

namespace TopDog.Content.Ships;

public static class HullBonusSummary
{
    public static string Describe(HullDef? hull)
    {
        if (hull == null)
        {
            return "";
        }
        if (!string.IsNullOrWhiteSpace(hull.hullBonusSummary))
        {
            return hull.hullBonusSummary!;
        }

        var parts = new List<string>();
        if (hull.hullSpeedEquipAccelBonusPct > 0f)
        {
            parts.Add($"速度+装备加速幅度 +{hull.hullSpeedEquipAccelBonusPct:0}%");
        }
        if (hull.hullLargeAttackDamageBonusPct > 0f)
        {
            parts.Add($"大型攻击伤害 +{hull.hullLargeAttackDamageBonusPct:0}%");
        }
        if (hull.hullDefenseRegenBonusPct > 0f)
        {
            parts.Add($"防御装备回复 +{hull.hullDefenseRegenBonusPct:0}%");
        }
        if (hull.hullXlAttackDamageBonusPct > 0f)
        {
            parts.Add($"超大型攻击伤害 +{hull.hullXlAttackDamageBonusPct:0}%");
        }
        if (hull.hullLaunchedUnitBonusPct > 0f)
        {
            parts.Add($"从舰单位速度/伤害 +{hull.hullLaunchedUnitBonusPct:0}%");
        }
        if (hull.hullIncomingDamageReductionPct > 0f)
        {
            parts.Add($"受到伤害 -{hull.hullIncomingDamageReductionPct:0}%");
        }
        if (hull.warpScramResist > 0f)
        {
            parts.Add($"跃迁中断抗性 {hull.warpScramResist:0}");
        }
        if (hull.transitSpeedLyPerHour > 0f)
        {
            parts.Add($"跃迁速度 {hull.transitSpeedLyPerHour:0.#} ly/h");
        }
        return parts.Count == 0 ? "" : string.Join(" · ", parts);
    }

    public static float AttackDamageMultiplier(HullDef? hull, ModuleDef? mod)
    {
        if (hull == null || mod == null || mod.damagePerTick <= 0f)
        {
            return 1f;
        }
        var mult = 1f;
        var size = mod.moduleSize ?? "";
        if (hull.hullLargeAttackDamageBonusPct > 0f
            && (size.Equals(ModuleSize.Large, StringComparison.Ordinal)
                || size.Equals("LARGE", StringComparison.OrdinalIgnoreCase)))
        {
            mult *= 1f + hull.hullLargeAttackDamageBonusPct / 100f;
        }
        if (hull.hullXlAttackDamageBonusPct > 0f
            && (size.Equals(ModuleSize.ExtraLarge, StringComparison.Ordinal)
                || size.Equals("EXTRA_LARGE", StringComparison.OrdinalIgnoreCase)))
        {
            mult *= 1f + hull.hullXlAttackDamageBonusPct / 100f;
        }
        return mult;
    }

    public static float DefenseRegenMultiplier(HullDef? hull) =>
        hull == null || hull.hullDefenseRegenBonusPct <= 0f
            ? 1f
            : 1f + hull.hullDefenseRegenBonusPct / 100f;
}
