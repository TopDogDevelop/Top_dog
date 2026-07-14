using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Ship;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BOARDING_MODULE.md §3.3 · docs/SHIP_FITTING.md §6
 * 本文件: CombatModuleEnableService.cs — 战斗内模块槽位启用/禁用
 * 【机制要点】
 * · disabledModuleSlots：关闭的槽不参与 salvo/场域/速度加成
 * · ApplyBoardingEngageQuota：登录接战态保留登录槽 + 推进器
 * 【关联】BoardingModuleService · SalvoProfileService · ModuleActivationService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class CombatModuleEnableService
{
    public static bool IsSlotEnabled(BattlefieldUnit unit, string slotKey) =>
        !unit.disabledModuleSlots.Contains(slotKey);

    public static void SetSlotEnabled(BattlefieldUnit unit, string slotKey, bool enabled)
    {
        if (enabled)
        {
            unit.disabledModuleSlots.Remove(slotKey);
        }
        else
        {
            unit.disabledModuleSlots.Add(slotKey);
        }
    }

    public static int CountEnabledEquipped(
        BattlefieldUnit unit,
        IReadOnlyDictionary<string, string> fit)
    {
        var count = 0;
        foreach (var kv in fit)
        {
            if (!string.IsNullOrWhiteSpace(kv.Value) && IsSlotEnabled(unit, kv.Key))
            {
                count++;
            }
        }

        return count;
    }

    public static void ClearDisabledSlots(BattlefieldUnit unit) =>
        unit.disabledModuleSlots.Clear();

    /// <summary>登录接战态：关其它模块，尽量启用推进器（见 BOARDING_MODULE.md §3.3）。</summary>
    public static void ApplyBoardingEngageQuota(
        BattlefieldUnit unit,
        HullDef hull,
        ModuleRegistry modules)
    {
        var limit = FittingEnableSummary.SimultaneousEnableLimit(
            hull,
            FittingEnableSummary.Compute(hull, unit.fittedModules).SlotCount);
        if (limit <= 0)
        {
            limit = unit.fittedModules.Count;
        }

        string? boardingSlot = null;
        string? bestPropulsionSlot = null;
        var bestPropulsionSpeed = float.MinValue;

        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod == null)
            {
                continue;
            }

            if (BoardingModuleService.ModuleKind.Equals(mod.moduleKind, StringComparison.Ordinal))
            {
                boardingSlot = kv.Key;
                continue;
            }

            if (!mod.appliesToPropulsion)
            {
                continue;
            }

            var bonus = mod.speedBonusMps + hull.baseSpeedMps * mod.speedBonusPctWhenEnabled;
            if (bonus > bestPropulsionSpeed)
            {
                bestPropulsionSpeed = bonus;
                bestPropulsionSlot = kv.Key;
            }
        }

        var keep = new HashSet<string>(StringComparer.Ordinal);
        if (boardingSlot != null)
        {
            keep.Add(boardingSlot);
        }

        if (bestPropulsionSlot != null && (keep.Count < limit || keep.Count == 0))
        {
            keep.Add(bestPropulsionSlot);
        }

        unit.disabledModuleSlots.Clear();
        foreach (var slotKey in unit.fittedModules.Keys)
        {
            if (!keep.Contains(slotKey))
            {
                unit.disabledModuleSlots.Add(slotKey);
            }
        }

        unit.fieldAuraEnabledAtSec = 0f;
        unit.fieldAuraShieldDominant = false;
        unit.fieldAuraArmorDominant = false;
        unit.fieldAuraShieldSuppressed = false;
        unit.fieldAuraArmorSuppressed = false;

        ApplyPropulsionSpeed(unit, hull, modules);
        SalvoProfileService.ApplyToUnit(unit, hull, modules);
    }

    public static void RestoreAllEnabled(
        BattlefieldUnit unit,
        HullDef? hull,
        ModuleRegistry modules)
    {
        unit.disabledModuleSlots.Clear();
        if (hull != null)
        {
            ApplyPropulsionSpeed(unit, hull, modules);
            SalvoProfileService.ApplyToUnit(unit, hull, modules);
        }
    }

    public static void ApplyPropulsionSpeed(
        BattlefieldUnit unit,
        HullDef hull,
        ModuleRegistry modules)
    {
        var speed = hull.baseSpeedMps;
        foreach (var kv in unit.fittedModules)
        {
            if (!IsSlotEnabled(unit, kv.Key))
            {
                continue;
            }

            var mod = modules.Resolve(kv.Value);
            if (mod == null)
            {
                continue;
            }

            speed += mod.speedBonusMps;
            if (mod.appliesToPropulsion && mod.speedBonusPctWhenEnabled > 0f)
            {
                speed += hull.baseSpeedMps * mod.speedBonusPctWhenEnabled;
            }
        }

        unit.maxSpeedMps = Math.Max(80f, speed);
    }
}
