using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Ship;

public enum ModuleSizeRelation
{
    TooLarge,
    Oversized,
    Matched,
    Undersized,
}

public static class FittingCheckSummary
{
    public static ModuleSizeRelation SizeRelation(HullDef? hull, string slotKey, ModuleDef mod)
    {
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal) && FittingValidator.IsGainPlugin(mod))
        {
            return ModuleSizeRelation.Matched;
        }
        return ModuleSizeRelationFromDelta(ModuleSize.TierDelta(FittingValidator.SlotSize(hull, slotKey), mod.moduleSize));
    }

    public static ModuleSizeRelation ModuleSizeRelationFromDelta(int tierDelta) => tierDelta switch
    {
        > 1 => ModuleSizeRelation.TooLarge,
        1 => ModuleSizeRelation.Oversized,
        0 => ModuleSizeRelation.Matched,
        _ => ModuleSizeRelation.Undersized,
    };

    public static string SizeRelationLabel(ModuleSizeRelation rel) => rel switch
    {
        ModuleSizeRelation.TooLarge => "不可装（越两级及以上）",
        ModuleSizeRelation.Oversized => "越位（高一级）",
        ModuleSizeRelation.Matched => "对位",
        ModuleSizeRelation.Undersized => "低位",
        _ => "?",
    };

    public static int CountUndersizedFittings(
        GameState state,
        MemberState m,
        HullDef hull,
        ModuleRegistry modules)
    {
        var count = 0;
        foreach (var e in MemberFittingService.Fittings(state, m))
        {
            var fitted = modules.Find(e.Value);
            if (fitted != null && ModuleSize.TierDelta(FittingValidator.SlotSize(hull, e.Key), fitted.moduleSize) < 0)
            {
                count++;
            }
        }
        return count;
    }

    public static int EffectiveMaxOverslots(
        HullDef? hull,
        GameState state,
        MemberState m,
        ModuleRegistry modules)
    {
        if (hull == null)
        {
            return 0;
        }
        var bonus = 0;
        if (hull.underslotTradeConsume > 0 && hull.underslotTradeGrant > 0)
        {
            bonus = CountUndersizedFittings(state, m, hull, modules) / hull.underslotTradeConsume * hull.underslotTradeGrant;
        }
        return hull.maxOverslots + bonus;
    }

    public static string DescribeHullOverslotRules(HullDef? hull)
    {
        if (hull == null)
        {
            return "";
        }
        var sb = new System.Text.StringBuilder();
        sb.Append($"越位配额 {hull.maxOverslots}（已用 {{used}}/{{max}}）");
        if (hull.overslotAttackOnly)
        {
            sb.Append(" · 仅攻击槽可越位");
        }
        if (hull.underslotTradeConsume > 0 && hull.underslotTradeGrant > 0)
        {
            sb.Append($" · 每 {hull.underslotTradeConsume} 个低位额外 +{hull.underslotTradeGrant} 次越位");
        }
        sb.Append(" · 仅可向上越一级");
        return sb.ToString();
    }

    public static string DescribeModuleFit(
        HullDef? hull,
        string slotKey,
        ModuleDef mod,
        GameState state,
        MemberState m,
        ModuleRegistry modules)
    {
        if (hull == null)
        {
            return "";
        }
        var rel = SizeRelation(hull, slotKey, mod);
        var slotSize = FittingValidator.SlotSize(hull, slotKey);
        var line = $"{SizeRelationLabel(rel)} · 槽{ModuleSize.DisplayTag(slotSize).Trim('[', ']')} / 装{ModuleSize.DisplayTag(mod.moduleSize).Trim('[', ']')}";
        if (rel == ModuleSizeRelation.Oversized)
        {
            var used = FittingValidator.CountOversizedFittings(state, m, hull, modules);
            var fit = MemberFittingService.Fittings(state, m);
            if (fit.TryGetValue(slotKey, out var prev))
            {
                var old = modules.Find(prev);
                if (old != null && ModuleSize.IsOversized(slotSize, old.moduleSize))
                {
                    used--;
                }
            }
            var max = EffectiveMaxOverslots(hull, state, m, modules);
            line += used < max ? " · 可装配" : " · 越位已满";
        }
        else if (rel == ModuleSizeRelation.TooLarge)
        {
            line += " · 不可装配";
        }
        return line;
    }
}
