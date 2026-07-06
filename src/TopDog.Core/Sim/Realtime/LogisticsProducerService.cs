using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: 战术场域计划 Phase 3.3 · mod_strike_assembly / mod_drone_queen / mod_missile_pipeline
 * 本文件: LogisticsProducerService.cs — 后勤生产 15s 重置耗尽发射管
 * 【关联】LaunchTubeStateService · FieldAuraService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class LogisticsProducerService
{
    public const float DefaultTickSec = 15f;
    public const float DefaultRadiusM = 15_000f;

    private static readonly Dictionary<string, float> AccByUnit = new(StringComparer.Ordinal);

    public static void Tick(BattlefieldState bf, ModuleRegistry modules, float dtSec)
    {
        foreach (var producer in bf.units)
        {
            if (producer.IsDestroyed() || producer.isBuilding)
            {
                continue;
            }

        foreach (var modId in producer.fittedModules)
        {
            var slotKey = modId.Key;
            var mod = modules.Resolve(modId.Value);
            if (mod == null || mod.producerConsumableKind == null)
            {
                continue;
            }

            if (!ModuleActivationService.IsFunctionModuleActive(producer, slotKey, mod))
            {
                continue;
            }

                var key = producer.unitId ?? "";
                var interval = mod.producerResetIntervalSec > 0f
                    ? mod.producerResetIntervalSec
                    : DefaultTickSec;
                var acc = AccByUnit.GetValueOrDefault(key) + dtSec;
                if (acc < interval)
                {
                    AccByUnit[key] = acc;
                    continue;
                }

                AccByUnit[key] = acc - interval;
                var radius = mod.producerRadiusM > 0f ? mod.producerRadiusM : DefaultRadiusM;
                ResetAlliesInRadius(producer, bf, mod, modules, radius);
            }
        }
    }

    public static float ResolveProducerRadiusM(BattlefieldUnit producer, ModuleRegistry modules)
    {
        var max = 0f;
        foreach (var kv in producer.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod?.producerConsumableKind == null)
            {
                continue;
            }

            if (!ModuleActivationService.IsFunctionModuleActive(producer, kv.Key, mod))
            {
                continue;
            }

            var radius = mod.producerRadiusM > 0f ? mod.producerRadiusM : DefaultRadiusM;
            max = Math.Max(max, radius);
        }

        return max > 0f ? max : DefaultRadiusM;
    }

    private static void ResetAlliesInRadius(
        BattlefieldUnit producer,
        BattlefieldState bf,
        ModuleDef producerMod,
        ModuleRegistry modules,
        float radiusM)
    {
        foreach (var ally in bf.units)
        {
            if (ally.IsDestroyed() || ally.side != producer.side || ReferenceEquals(ally, producer))
            {
                continue;
            }

            if (FieldAuraService.DistanceM(producer, ally) > radiusM)
            {
                continue;
            }

            if (LaunchTubeStateService.TryResetDepleted(producer, ally, producerMod, modules))
            {
                CombatTelemetryLog.Log(
                    "tube.reset",
                    $"{producer.unitId}→{ally.unitId} kind={producerMod.producerConsumableKind}");
            }
        }

        CombatTelemetryLog.Log(
            "logistics.producer-tick",
            $"{producer.unitId} kind={producerMod.producerConsumableKind} r={radiusM:F0}m");
    }
}
