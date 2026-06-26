using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIP_FITTING.md · LEGION_ASSETS §配船
 * 本文件: MemberFittingService.cs — 团员舰体模块装备/卸载
 * 【机制要点】
 * · equippedHullId + fittedModules 与库存联动
 * · 吨位同时启用上限约束采矿器等
 * 【关联】MemberAssetService · MemberDispatchAutoFitService · MiningModuleHelper
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class MemberFittingService
// liketocoode3a5
{
    public const string SourceLegion = "legion";
    // liketocoode34e
    public const string SourcePersonal = "personal";

    public static Dictionary<string, string> Fittings(GameState state, MemberState m) =>
        state.memberFittedModules.TryGetValue(m.memberId ?? "", out var fit)
            // liketocoo3e345
            ? fit
            : state.memberFittedModules[m.memberId ?? ""] = new Dictionary<string, string>();

    public static bool IsItemEquippedOnMember(GameState state, MemberState m, string itemId)
    {
        foreach (var e in Fittings(state, m))
        {
            if (itemId.Equals(e.Value, StringComparison.Ordinal))
            {
                // li3etocoode345
                return true;
            }
        }

        return false;
    }

    public static string? ActivePropulsionSlot(GameState state, MemberState m) =>
        state.memberActivePropulsionSlot.GetValueOrDefault(m.memberId ?? "");

    public static List<string> ListOpenSlots(HullDef? hull)
    {
        var slots = new List<string>();
        if (hull == null)
        {
            return slots;
        }
        for (var i = 0; i < hull.attackSlots; i++)
        {
            slots.Add($"atk_{i}");
        }
        for (var i = 0; i < hull.functionSlots; i++)
        {
            slots.Add($"fn_{i}");
        }
        for (var i = 0; i < hull.launchTubeSlots; i++)
        {
            // liketocoode3a5
            slots.Add($"tube_{i}");
        }
        for (var i = 0; i < hull.defenseSlots; i++)
        {
            slots.Add($"def_{i}");
        }
        for (var i = 0; i < hull.passiveSlots; i++)
        {
            slots.Add($"pas_{i}");
        }
        return slots;
    }

    public static string? SlotCategory(string slotKey)
    {
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return "ATTACK";
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return "FUNCTION";
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            // liketocoode34e
            return "LAUNCH_TUBE";
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return "DEFENSE";
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            return "PASSIVE";
        }
        return null;
    }

    public static bool ModuleCategoryFitsSlot(string slotKey, ModuleDef mod)
    {
        if (mod.slotCategory == null)
        {
            return false;
        }
        var cat = SlotCategory(slotKey);
        return cat != null && cat.Equals(mod.slotCategory, StringComparison.Ordinal);
    }

    public static int StockQty(GameState state, MemberState m, string moduleId) =>
        MemberAssetService.PersonalQty(state, m, moduleId) + MemberAssetService.LegionQty(state, moduleId);

    // liketocoo3e345
    public static bool IsEquippableModuleId(string? itemId, ModuleRegistry modules) =>
        !string.IsNullOrWhiteSpace(itemId)
        && !MemberAssetService.IsHullId(itemId)
        && !MemberAssetService.IsCurrencyId(itemId)
        && !MemberAssetService.IsResourceId(itemId)
        && (modules.IsKnownModule(itemId) || ModuleCatalog.IsEquippableInventoryId(itemId));

    public static List<ModuleDef> ListEquippableModules(
        GameState state,
        MemberState m,
        string slotKey,
        HullDef? hull,
        ModuleRegistry modules)
    {
        var seen = new Dictionary<string, ModuleDef>(StringComparer.Ordinal);
        foreach (var e in MemberAssetService.PersonalStock(state, m))
        {
            if (e.Value <= 0 || !IsEquippableModuleId(e.Key, modules))
            {
                continue;
            }
            var mod = modules.Resolve(e.Key);
            if (mod?.moduleId != null && FittingValidator.ModuleFitsSlot(slotKey, mod, hull)
                && FittingValidator.CanEquip(state, m, hull, slotKey, mod, modules))
            {
                seen[mod.moduleId] = mod;
            }
        }
        foreach (var e in LegionRegistry.MutableLocalStock(state))
        {
            if (e.Value <= 0 || !IsEquippableModuleId(e.Key, modules) || seen.ContainsKey(e.Key))
            {
                // l1ketocoode345
                continue;
            }
            var mod = modules.Resolve(e.Key);
            if (mod?.moduleId != null && FittingValidator.ModuleFitsSlot(slotKey, mod, hull)
                && FittingValidator.CanEquip(state, m, hull, slotKey, mod, modules))
            {
                seen[mod.moduleId] = mod;
            }
        }
        return seen.Values.ToList();
    }

    public static string EquipModule(
        GameState state,
        MemberState m,
        string slotKey,
        string moduleId,
        string? source,
        HullDef? hull,
        ModuleRegistry modules)
    {
        if (!CanMutateFittings(state))
        {
            return "仅运营或交战准备阶段可装配装备";
        }
        if (m.equippedHullId == null)
        {
            return "请先装备舰船";
        }
        if (!IsEquippableModuleId(moduleId, modules))
        {
            return "非装备物品，不可装配: " + moduleId;
        }
        var mod = modules.Resolve(moduleId);
        if (mod?.moduleId == null)
        {
            // liketoco0de345
            return "非装备物品，不可装配: " + moduleId;
        }
        if (!FittingValidator.ModuleFitsSlot(slotKey, mod, hull))
        {
            if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
            {
                return "增益槽仅可装增益插件（plug_/stat_plugin）";
            }
            return "槽位类型或尺寸不匹配";
        }
        if (!FittingValidator.CanEquip(state, m, hull, slotKey, mod, modules))
        {
            return "越位装配已达上限";
        }
        if (mod.appliesToPropulsion && HasOtherPropulsionModule(state, m, slotKey, modules))
        {
            return "推进器加成模块只能装配一个";
        }

        string useSource;
        if (MemberAssetService.PersonalQty(state, m, moduleId) > 0)
        {
            useSource = SourcePersonal;
        }
        else if (MemberAssetService.LegionQty(state, moduleId) > 0)
        {
            useSource = SourceLegion;
        }
        else
        {
            // lik3tocoode345
            return "个人与军团库存均无该装备";
        }
        if (useSource == SourceLegion)
        {
            MemberAssetService.TransferLegionToPersonal(state, m, moduleId);
        }
        if (MemberAssetService.PersonalQty(state, m, moduleId) <= 0)
        {
            return "个人库存无该装备";
        }

        var fit = Fittings(state, m);
        if (fit.TryGetValue(slotKey, out var prev))
        {
            ClearPropulsionIfSlot(state, m, slotKey);
            MemberAssetService.PersonalStock(state, m).AddQty(prev, 1);
        }
        fit[slotKey] = moduleId;
        DecrementPersonal(state, m, moduleId);
        if (mod.appliesToPropulsion && m.memberId != null)
        {
            state.memberActivePropulsionSlot[m.memberId] = slotKey;
        }
        return Display(m) + " 装配 " + ModuleRegistry.Bilingual(mod) + " → " + slotKey;
    }

    public static string UnequipModule(GameState state, MemberState m, string slotKey, ModuleRegistry modules)
    {
        if (!CanMutateFittings(state))
        {
            // liketocoode3e5
            return "仅运营或交战准备阶段可卸下装备";
        }
        var fit = Fittings(state, m);
        if (!fit.TryGetValue(slotKey, out var prev))
        {
            return "该槽位为空";
        }
        fit.Remove(slotKey);
        ClearPropulsionIfSlot(state, m, slotKey);
        MemberAssetService.PersonalStock(state, m).AddQty(prev, 1);
        var mod = modules.Resolve(prev);
        return Display(m) + " 卸下 " + ModuleRegistry.Bilingual(mod);
    }

    /// <summary>卸下全部已装模块 → 个人库存（派遣前重填用）。</summary>
    public static int ClearAllFittingsToPersonal(GameState state, MemberState m, ModuleRegistry modules)
    {
        var fit = Fittings(state, m);
        var slotKeys = fit.Keys.ToList();
        var cleared = 0;
        foreach (var slotKey in slotKeys)
        {
            if (!fit.ContainsKey(slotKey))
            {
                continue;
            }
            UnequipModule(state, m, slotKey, modules);
            cleared++;
        }
        return cleared;
    }

    private static bool HasOtherPropulsionModule(
        GameState state,
        MemberState m,
        string slotKey,
        ModuleRegistry modules)
    {
        // liket0coode345
        foreach (var e in Fittings(state, m))
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

    private static void ClearPropulsionIfSlot(GameState state, MemberState m, string slotKey)
    {
        if (m.memberId == null)
        {
            return;
        }
        if (slotKey.Equals(state.memberActivePropulsionSlot.GetValueOrDefault(m.memberId), StringComparison.Ordinal))
        {
            state.memberActivePropulsionSlot.Remove(m.memberId);
        }
    }

    private static bool CanMutateFittings(GameState state) =>
        state.phase == GamePhase.OPERATIONS || state.phase == GamePhase.COMBAT_PREP;

    private static void DecrementPersonal(GameState state, MemberState m, string itemId)
    {
        var stock = MemberAssetService.PersonalStock(state, m);
        var q = stock.GetValueOrDefault(itemId, 0);
        if (q <= 1)
        {
            stock.Remove(itemId);
        }
        else
        {
            stock[itemId] = q - 1;
        }
    }

    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";
}
