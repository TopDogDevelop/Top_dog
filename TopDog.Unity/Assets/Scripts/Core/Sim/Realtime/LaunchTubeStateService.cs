using TopDog.Content.Modules;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIP_FITTING.md §发射管三态
 * 本文件: LaunchTubeStateService.cs — 发射管槽位运行时三态
 * 【机制要点】
 * · spawn 时 tube_* → Inactive
 * · 发射导弹/翼 → Activated；消耗品销毁 → Depleted
 * · 后勤生产模块 15km 内重置 Depleted → Inactive
 * 【关联】LogisticsProducerService · MissileLaunchService · StrikeWingSpawnService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class LaunchTubeStateService
{
    public static void InitTubeStates(BattlefieldUnit unit, ModuleRegistry? modules = null)
    {
        unit.tubeStates.Clear();
        unit.bayStates.Clear();
        foreach (var kv in unit.fittedModules)
        {
            if (!kv.Key.StartsWith("tube_", StringComparison.Ordinal))
            {
                continue;
            }

            if (modules != null && !IsTubeConsumable(modules.Resolve(kv.Value)))
            {
                continue;
            }

            unit.tubeStates[kv.Key] = LaunchTubeState.Inactive;
            if (modules != null
                && modules.Resolve(kv.Value) is { } module
                && CarriedUnitDeploymentService.IsCarriedUnitBay(module))
            {
                unit.bayStates[kv.Key] = new BayRuntimeState();
            }
        }
    }

    public static void OnMissileLaunched(BattlefieldUnit launcher, string slotKey)
    {
        if (launcher.tubeStates.TryGetValue(slotKey, out var state)
            && state == LaunchTubeState.Inactive)
        {
            launcher.tubeStates[slotKey] = LaunchTubeState.Activated;
        }
    }

    public static void OnWingLaunched(BattlefieldUnit carrier, string slotKey)
    {
        OnMissileLaunched(carrier, slotKey);
    }

    public static void OnWingRecalled(BattlefieldUnit launcher, string slotKey)
    {
        if (launcher.tubeStates.TryGetValue(slotKey, out var state)
            && state == LaunchTubeState.Activated)
        {
            launcher.tubeStates[slotKey] = LaunchTubeState.Inactive;
        }
    }

    public static void ResetStrikeWingTubesToInactive(
        BattlefieldUnit launcher,
        ModuleRegistry? modules = null)
    {
        foreach (var kv in launcher.tubeStates.ToList())
        {
            if (!kv.Key.StartsWith("tube_", StringComparison.Ordinal))
            {
                continue;
            }

            if (!launcher.fittedModules.TryGetValue(kv.Key, out var modId))
            {
                continue;
            }

            var module = modules?.Resolve(modId);
            if (module != null
                && CarriedUnitDeploymentService.IsCarriedUnitBay(module)
                && kv.Value == LaunchTubeState.Activated)
            {
                launcher.tubeStates[kv.Key] = LaunchTubeState.Inactive;
                if (launcher.bayStates.TryGetValue(kv.Key, out var bay))
                {
                    bay.state = CarriedUnitLifecycle.Stored;
                    bay.childUnitId = null;
                    bay.reservedCapacity = 0;
                }
            }
        }
    }

    public static void OnConsumableDestroyed(BattlefieldUnit launcher, string slotKey)
    {
        if (launcher.tubeStates.ContainsKey(slotKey))
        {
            launcher.tubeStates[slotKey] = LaunchTubeState.Depleted;
            if (launcher.unitId != null)
            {
                CombatTelemetryLog.LogTubeDepleted(launcher.unitId, slotKey);
            }
        }
    }

    public static void NotifyChildDestroyed(BattlefieldState bf, BattlefieldUnit child)
    {
        if (child.parentUnitId == null)
        {
            return;
        }

        var parent = BattlefieldSystem.FindUnit(bf, child.parentUnitId);
        if (parent == null)
        {
            return;
        }

        var modId = child.missileModuleId ?? child.hullId;
        if (modId == null)
        {
            return;
        }

        var slot = !string.IsNullOrWhiteSpace(child.carriedSourceSlot)
            ? child.carriedSourceSlot
            : !string.IsNullOrWhiteSpace(child.missileLaunchTubeSlot)
                ? child.missileLaunchTubeSlot
            : FindTubeSlotForModule(parent, modId);
        if (slot != null)
        {
            if (parent.carriedShipsBySlot.TryGetValue(slot, out var payload)
                && payload.shipInstanceId.Equals(child.shipInstanceId, StringComparison.Ordinal))
            {
                payload.shieldHp = child.shieldHp;
                payload.armorHp = child.armorHp;
                payload.structureHp = child.structureHp;
                payload.destroyed = true;
            }
            if (parent.bayStates.TryGetValue(slot, out var bay))
            {
                bay.state = CarriedUnitLifecycle.Lost;
                bay.childUnitId = null;
                bay.reservedCapacity = 0;
            }
            if (BoardSummonWingService.IsTempBoardTube(slot))
            {
                BoardSummonWingService.RemoveTempTube(parent, slot);
            }
            else
            {
                OnConsumableDestroyed(parent, slot);
            }
        }
    }

    public static bool TryResetDepleted(
        BattlefieldUnit producer,
        BattlefieldUnit ally,
        ModuleDef producerMod,
        ModuleRegistry modules)
    {
        if (producerMod.producerConsumableKind == null)
        {
            return false;
        }

        var reset = false;
        foreach (var kv in ally.tubeStates.ToList())
        {
            if (kv.Value != LaunchTubeState.Depleted)
            {
                continue;
            }

            if (!ally.fittedModules.TryGetValue(kv.Key, out var modId))
            {
                continue;
            }

            var tubeMod = modules.Resolve(modId);
            if (tubeMod == null || !MatchesProducerKind(producerMod, tubeMod))
            {
                continue;
            }

            if (!TonnageWithinProducerMax(ally, producerMod))
            {
                continue;
            }

            ally.tubeStates[kv.Key] = LaunchTubeState.Inactive;
            reset = true;
        }

        return reset;
    }

    public static string? FindTubeSlotForModule(BattlefieldUnit unit, string moduleId)
    {
        foreach (var kv in unit.fittedModules)
        {
            if (!kv.Key.StartsWith("tube_", StringComparison.Ordinal))
            {
                continue;
            }

            if (moduleId.Equals(kv.Value, StringComparison.Ordinal))
            {
                return kv.Key;
            }
        }

        return null;
    }

    private static bool IsTubeConsumable(ModuleDef? mod)
    {
        if (mod == null)
        {
            return false;
        }

        return CarriedUnitDeploymentService.IsCarriedUnitBay(mod)
               || mod.missileStructureHp > 0f;
    }

    private static bool MatchesProducerKind(ModuleDef producer, ModuleDef tubeMod)
    {
        var kind = producer.producerConsumableKind;
        if (kind == null)
        {
            return false;
        }

        if ("strike_craft".Equals(kind, StringComparison.Ordinal))
        {
            return CarriedUnitDeploymentService.IsCarriedUnitBay(tubeMod);
        }

        if ("missile".Equals(kind, StringComparison.Ordinal))
        {
            return tubeMod.missileStructureHp > 0f;
        }

        if ("drone".Equals(kind, StringComparison.Ordinal))
        {
            return CarriedUnitDeploymentService.IsCarriedUnitBay(tubeMod);
        }

        return false;
    }

    private static bool TonnageWithinProducerMax(BattlefieldUnit ally, ModuleDef producer)
    {
        if (string.IsNullOrWhiteSpace(producer.producerResetTonnageMax))
        {
            return true;
        }

        var maxRank = RankOfTonnageMax(producer.producerResetTonnageMax);
        var allyRank = Combat.CombatPowerCalculator.TonnageRankOf(ally.tonnageClass);
        return allyRank <= maxRank;
    }

    private static int RankOfTonnageMax(string tonnageMax) => tonnageMax.ToUpperInvariant() switch
    {
        "SMALL" or "S" => 3,
        "MEDIUM" or "M" => 5,
        "LARGE" or "L" => 7,
        "XL" or "YOULIANG" => 10,
        _ => 99,
    };
}
