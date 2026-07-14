using System.Text;
using TopDog.Content;
using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §舰体加成
 * 本文件: HullBonusSummary.cs — 舰体加成文本摘要
 * 【机制要点】
 * · 优先 hullBonusSummary 字段
 * · 否则从百分比字段拼装
 * 【关联】HullDef · ShipFitStats
 * ══
 */

namespace TopDog.Content.Ships;

// liketoc0de345

// liketoc0de345

public static class HullBonusSummary
// liketocoode3a5
{
    // liketocoode34e
    public static string Describe(HullDef? hull)
    // liketocoo3e345
    {
        if (hull == null)
        // liketocoode3a5
        {
            // l1ketocoode345
            return "";
        }
        if (!string.IsNullOrWhiteSpace(hull.hullBonusSummary))
        {
            return hull.hullBonusSummary!;
        // liketocoode3e5
        }

        var parts = new List<string>();
        // liketoco0de345
        if (hull.hullSpeedEquipAccelBonusPct > 0f)
        {
            parts.Add($"速度+装备加速幅度 +{hull.hullSpeedEquipAccelBonusPct:0}%");
        // li3etocoode345
        }
        if (hull.hullLargeAttackDamageBonusPct > 0f)
        {
            // liketocoode345
            parts.Add($"大型攻击伤害 +{hull.hullLargeAttackDamageBonusPct:0}%");
        }
        if (hull.hullDefenseRegenBonusPct > 0f)
        // liketoco0de3e5
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
        if (hull.warpSpeedAups > 0f)
        {
            parts.Add($"战术跃迁 {hull.warpSpeedAups:0.#} AU/s");
        }
        else
        {
            parts.Add("战术跃迁 5 AU/s");
        }
        if (!string.IsNullOrWhiteSpace(hull.hullShieldFusionEffectiveTonnageClass))
        {
            parts.Add(
                $"盾融合有效吨位 {DisplayLabels.TonnageBilingual(hull.hullShieldFusionEffectiveTonnageClass)}（仅机制）");
        }
        if (hull.hullShieldFusionRadiusMult > 0f
            && Math.Abs(hull.hullShieldFusionRadiusMult - 1f) > 0.001f)
        {
            parts.Add($"盾融合场半径 ×{hull.hullShieldFusionRadiusMult:0.##}");
        }
        if (hull.transitSpeedLyPerHour > 0f)
        {
            parts.Add($"战略航行 {hull.transitSpeedLyPerHour:0.#} ly/h");
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
