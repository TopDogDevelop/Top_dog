using TopDog.Content.Modules;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>场域/后勤 FUNCTION 模块默认开启与 enabled 门控。</summary>
public static class ModuleActivationService
{
    public static void EnableFieldModulesByDefault(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod == null)
            {
                continue;
            }

            if (IsFieldKind(mod.moduleKind) || mod.producerConsumableKind != null)
            {
                unit.fieldAuraEnabledAtSec = Math.Max(unit.fieldAuraEnabledAtSec, 0.001f);
                break;
            }
        }
    }

    public static bool IsFunctionModuleActive(BattlefieldUnit unit, string slotKey, ModuleDef mod)
    {
        if (!slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return true;
        }

        if (!CombatModuleEnableService.IsSlotEnabled(unit, slotKey))
        {
            return false;
        }

        if (IsFieldKind(mod.moduleKind) || mod.producerConsumableKind != null)
        {
            return unit.fieldAuraEnabledAtSec > 0f;
        }

        if (BoardingModuleService.ModuleKind.Equals(mod.moduleKind, StringComparison.Ordinal))
        {
            return unit.boardingModuleEnabled;
        }

        return true;
    }

    private static bool IsFieldKind(string? moduleKind) =>
        "shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal)
        || "armor_link_field".Equals(moduleKind, StringComparison.Ordinal);
}
