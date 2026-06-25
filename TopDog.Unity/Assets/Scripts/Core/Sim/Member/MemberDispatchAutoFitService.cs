using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Ship;
using TopDog.Sim.State;

namespace TopDog.Sim.Member;

/// <summary>派遣执行任务时：先卸装 → 按词条或默认规则用个人库存填装。</summary>
public static class MemberDispatchAutoFitService
{
    private enum FillMode
    {
        Random,
        Luxury,
        Thrift,
    }

    public static string TryFillEmptySlots(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random? rng = null)
    {
        if (state.phase != GamePhase.OPERATIONS || m.equippedHullId == null)
        {
            return "";
        }
        var hull = ships.FindHull(m.equippedHullId);
        if (hull == null)
        {
            return "";
        }

        MemberFittingService.ClearAllFittingsToPersonal(state, m, modules);
        var mode = ResolveFillMode(m);
        var filled = mode switch
        {
            FillMode.Luxury => FillSorted(state, m, hull, modules, descending: true),
            FillMode.Thrift => FillSorted(state, m, hull, modules, descending: false),
            _ => FillRandom(state, m, hull, modules, rng ?? new Random()),
        };

        if (filled <= 0)
        {
            return "";
        }
        BrickDebugLog.Log("economy.autofit", $"Fill {m.memberId} slots={filled} mode={mode}");
        var verb = mode switch
        {
            FillMode.Luxury => "奢侈填装",
            FillMode.Thrift => "节约填装",
            _ => "自动填装",
        };
        return " · " + verb + " " + filled + " 槽";
    }

    private static FillMode ResolveFillMode(MemberState m)
    {
        if (MemberTraitIds.HasEquipLuxury(m))
        {
            return FillMode.Luxury;
        }
        if (MemberTraitIds.HasEquipThrift(m))
        {
            return FillMode.Thrift;
        }
        return FillMode.Random;
    }

    private static int FillRandom(
        GameState state,
        MemberState m,
        HullDef hull,
        ModuleRegistry modules,
        Random random)
    {
        var fit = MemberFittingService.Fittings(state, m);
        var emptySlots = MemberFittingService.ListOpenSlots(hull)
            .Where(s => !fit.ContainsKey(s))
            .OrderBy(_ => random.Next())
            .ToList();

        var filled = 0;
        foreach (var slotKey in emptySlots)
        {
            var candidates = ListPersonalCandidates(
                state, m, slotKey, hull, modules, random, ignoreValuationCap: false);
            if (candidates.Count == 0)
            {
                continue;
            }
            var moduleId = candidates[random.Next(candidates.Count)];
            if (TryEquipPersonal(state, m, slotKey, moduleId, hull, modules))
            {
                filled++;
            }
        }
        return filled;
    }

    private static int FillSorted(
        GameState state,
        MemberState m,
        HullDef hull,
        ModuleRegistry modules,
        bool descending)
    {
        const bool ignoreValuationCap = true;
        var instances = ExpandPersonalEquippable(state, m, modules, hull, ignoreValuationCap);
        instances.Sort((a, b) =>
        {
            var cmp = a.value.CompareTo(b.value);
            if (cmp == 0)
            {
                cmp = string.Compare(a.moduleId, b.moduleId, StringComparison.Ordinal);
            }
            return descending ? -cmp : cmp;
        });

        var slots = MemberFittingService.ListOpenSlots(hull);
        var filled = 0;
        foreach (var inst in instances)
        {
            foreach (var slotKey in slots)
            {
                var fit = MemberFittingService.Fittings(state, m);
                if (fit.ContainsKey(slotKey))
                {
                    continue;
                }
                if (!CanEquipPersonal(
                        state, m, slotKey, inst.moduleId, hull, modules, ignoreValuationCap))
                {
                    continue;
                }
                if (TryEquipPersonal(state, m, slotKey, inst.moduleId, hull, modules))
                {
                    filled++;
                    break;
                }
            }
        }
        return filled;
    }

    private static List<(string moduleId, int value)> ExpandPersonalEquippable(
        GameState state,
        MemberState m,
        ModuleRegistry modules,
        HullDef hull,
        bool ignoreValuationCap)
    {
        var hullValue = AssetValuation.HullStarCoinValue(hull);
        var list = new List<(string moduleId, int value)>();
        foreach (var e in MemberAssetService.PersonalStock(state, m))
        {
            if (e.Value <= 0 || !MemberFittingService.IsEquippableModuleId(e.Key, modules))
            {
                continue;
            }
            var mod = modules.Resolve(e.Key);
            if (mod?.moduleId == null)
            {
                continue;
            }
            if (!ignoreValuationCap && AssetValuation.ModuleStarCoinValue(mod) > hullValue)
            {
                continue;
            }
            for (var i = 0; i < e.Value; i++)
            {
                list.Add((mod.moduleId, AssetValuation.ModuleStarCoinValue(mod)));
            }
        }
        return list;
    }

    private static bool CanEquipPersonal(
        GameState state,
        MemberState m,
        string slotKey,
        string moduleId,
        HullDef hull,
        ModuleRegistry modules,
        bool ignoreValuationCap)
    {
        if (MemberAssetService.PersonalQty(state, m, moduleId) <= 0)
        {
            return false;
        }
        var mod = modules.Resolve(moduleId);
        if (mod?.moduleId == null)
        {
            return false;
        }
        if (!ignoreValuationCap
            && AssetValuation.ModuleStarCoinValue(mod) > AssetValuation.HullStarCoinValue(hull))
        {
            return false;
        }
        if (!FittingValidator.ModuleFitsSlot(slotKey, mod, hull)
            || !FittingValidator.CanEquip(state, m, hull, slotKey, mod, modules))
        {
            return false;
        }
        if (mod.appliesToPropulsion && HasOtherPropulsionModule(state, m, slotKey, modules))
        {
            return false;
        }
        return true;
    }

    private static bool TryEquipPersonal(
        GameState state,
        MemberState m,
        string slotKey,
        string moduleId,
        HullDef hull,
        ModuleRegistry modules)
    {
        var echo = MemberFittingService.EquipModule(
            state, m, slotKey, moduleId, MemberFittingService.SourcePersonal, hull, modules);
        return echo.Contains("装配", StringComparison.Ordinal);
    }

    private static List<string> ListPersonalCandidates(
        GameState state,
        MemberState m,
        string slotKey,
        HullDef hull,
        ModuleRegistry modules,
        Random random,
        bool ignoreValuationCap)
    {
        var list = new List<string>();
        foreach (var e in MemberAssetService.PersonalStock(state, m))
        {
            if (e.Value <= 0 || !MemberFittingService.IsEquippableModuleId(e.Key, modules))
            {
                continue;
            }
            if (!CanEquipPersonal(state, m, slotKey, e.Key, hull, modules, ignoreValuationCap: false))
            {
                continue;
            }
            list.Add(e.Key);
        }
        return list.OrderBy(_ => random.Next()).ToList();
    }

    private static bool HasOtherPropulsionModule(
        GameState state,
        MemberState m,
        string slotKey,
        ModuleRegistry modules)
    {
        foreach (var e in MemberFittingService.Fittings(state, m))
        {
            if (slotKey.Equals(e.Key, StringComparison.Ordinal) || e.Value == null)
            {
                continue;
            }
            var other = modules.Resolve(e.Value);
            if (other?.appliesToPropulsion == true)
            {
                return true;
            }
        }
        return false;
    }
}
