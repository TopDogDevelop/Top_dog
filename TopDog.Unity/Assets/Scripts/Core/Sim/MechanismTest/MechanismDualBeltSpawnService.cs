using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_INDEX.md #12 · INTERDICTION_FIELD.md
 * 本文件: MechanismDualBeltSpawnService.cs — 双矿带：双方同 mt_belt 开战，乙带作跃迁目标
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismDualBeltSpawnService
{
    public static void BootstrapBattlefields(
        GameState state,
        MechanismTestScenarioDef scenario,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        _ = rng;
        MechanismTestSpawnService.BootstrapBattlefields(state, scenario, ships, modules, rng);
        var bf = state.battlefields.FirstOrDefault();
        if (bf == null)
        {
            return;
        }

        foreach (var u in bf.units)
        {
            DisableOpeningSpecialSlots(u, modules);
        }
    }

    private static void DisableOpeningSpecialSlots(BattlefieldUnit u, ModuleRegistry modules)
    {
        foreach (var pair in u.fittedModules.ToList())
        {
            var mod = modules.Resolve(pair.Value);
            if (mod?.moduleKind is "interdiction_field_fixed"
                or "interdiction_field_mobile"
                or "armor_link_field"
                or "shield_fusion_field"
                || mod?.logicId is "logic_interdiction_field_fixed"
                or "logic_interdiction_field_mobile")
            {
                CombatModuleEnableService.SetSlotEnabled(u, pair.Key, false);
            }
        }
    }
}
