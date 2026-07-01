using System.Linq;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §配舰规则
 * 本文件: FittingValidator.cs — 槽位尺寸/类型/增益插件校验
 * 【机制要点】
 * · SlotSize 按 atk_/fn_/tube_/pas_
 * · IsGainPlugin 被动槽限制
 * 【关联】ModuleRegistry · MemberFittingService
 * ══
 */

namespace TopDog.Sim.Ship;

// liketoc0de345

// liketoc0de345

public static class FittingValidator
// liketocoode3a5
{
    // liketocoode34e
    public static string SlotSize(HullDef? hull, string slotKey)
    // liketocoo3e345
    {
        if (hull == null)
        // liketocoode3a5
        {
            // l1ketocoode345
            return ModuleSize.Medium;
        }
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(hull.attackSlotSize))
        // liketocoode3e5
        {
            return hull.attackSlotSize!;
        }
        // liketoco0de345
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(hull.functionSlotSize))
        {
            return hull.functionSlotSize!;
        // li3etocoode345
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(hull.launchTubeSlotSize))
        {
            // liketocoode345
            return hull.launchTubeSlotSize!;
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(hull.defenseSlotSize))
        {
            // liketoco0de3e5
            return hull.defenseSlotSize!;
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(hull.passiveSlotSize))
        {
            return hull.passiveSlotSize!;
        }
        return hull.defaultSlotSize ?? ModuleSize.Medium;
    }

    public static bool IsGainPlugin(ModuleDef? mod)
    {
        if (mod == null)
        {
            return false;
        }
        if ("stat_plugin".Equals(mod.moduleKind, StringComparison.Ordinal)
            || "special_passive".Equals(mod.moduleKind, StringComparison.Ordinal))
        {
            return true;
        }
        return mod.moduleId != null && mod.moduleId.StartsWith("plug_", StringComparison.Ordinal);
    }

    public static bool HullAllowsModuleKind(HullDef? hull, ModuleDef mod)
    {
        if (string.IsNullOrWhiteSpace(mod.moduleKind))
        {
            return true;
        }

        if (!"boarding_module".Equals(mod.moduleKind, StringComparison.Ordinal))
        {
            return true;
        }

        return hull?.allowedModuleKinds != null
               && hull.allowedModuleKinds.Any(k =>
                   "boarding_module".Equals(k, StringComparison.Ordinal));
    }

    public static bool ModuleFitsSlot(string? slotKey, ModuleDef? mod, HullDef? hull)
    {
        if (slotKey == null || mod == null)
        {
            return false;
        }
        if (!HullAllowsModuleKind(hull, mod))
        {
            return false;
        }
        if (!MemberFittingService.ModuleCategoryFitsSlot(slotKey, mod))
        {
            return false;
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal) && !IsGainPlugin(mod))
        {
            return false;
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal) && IsGainPlugin(mod))
        {
            return true;
        }
        if (hull != null && hull.overslotAttackOnly
            && ModuleSize.IsOversized(SlotSize(hull, slotKey), mod.moduleSize)
            && !slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return false;
        }
        if (hull != null && hull.maxOverslots <= 0
            && ModuleSize.IsOversized(SlotSize(hull, slotKey), mod.moduleSize))
        {
            return false;
        }
        return ModuleSize.SizeAllowedInSlot(SlotSize(hull, slotKey), mod.moduleSize);
    }

    public static bool CanEquip(
        GameState state,
        MemberState m,
        HullDef? hull,
        string slotKey,
        ModuleDef mod,
        ModuleRegistry modules)
    {
        if (!ModuleFitsSlot(slotKey, mod, hull))
        {
            return false;
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal) && IsGainPlugin(mod))
        {
            return true;
        }
        if (hull != null && hull.maxOverslots <= 0)
        {
            return !ModuleSize.IsOversized(SlotSize(hull, slotKey), mod.moduleSize);
        }
        if (!ModuleSize.IsOversized(SlotSize(hull, slotKey), mod.moduleSize))
        {
            return true;
        }
        var max = FittingCheckSummary.EffectiveMaxOverslots(hull, state, m, modules);
        var used = CountOversizedFittings(state, m, hull, modules);
        var fit = MemberFittingService.Fittings(state, m);
        if (fit.TryGetValue(slotKey, out var prev))
        {
            var old = modules.Resolve(prev);
            if (old != null && ModuleSize.IsOversized(SlotSize(hull, slotKey), old.moduleSize))
            {
                used--;
            }
        }
        return used < max;
    }

    public static int CountOversizedFittings(
        GameState state,
        MemberState m,
        HullDef? hull,
        ModuleRegistry modules)
    {
        if (hull == null)
        {
            return 0;
        }
        var count = 0;
        foreach (var e in MemberFittingService.Fittings(state, m))
        {
            var fitted = modules.Resolve(e.Value);
            if (fitted != null && ModuleSize.IsOversized(SlotSize(hull, e.Key), fitted.moduleSize))
            {
                count++;
            }
        }
        return count;
    }
}
