using TopDog.Content.Modules;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: 战术场域计划 §3.3 后勤自动瞄准 · docs/MECHANISM_TEST_INDEX.md
 * 本文件: LogisticsAutoTargetingService.cs — 后勤舰无指令时接近最近开启场域友舰
 * 【机制要点】
 * · 仅装配 producerConsumableKind 的后勤舰参与
 * · 目标：最近已开启 fleet_protection_field（盾融/甲连）友舰
 * · 维持距 ≈ 生产半径 85%，便于 LogisticsProducerService 重置发射管
 * · 不覆盖玩家舰队指令（SuppressForPlayerOrder）
 * 【关联】LogisticsProducerService · FieldAuraService · FleetOrderService · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class LogisticsAutoTargetingService
{
    private const float MaintainRadiusFactor = 0.85f;

    public static void SuppressForPlayerOrder(BattlefieldUnit unit)
    {
        if (!unit.logisticsAutoAimActive)
        {
            return;
        }

        unit.logisticsAutoAimActive = false;
        CombatTelemetryLog.Log("logistics.auto-aim", $"{unit.unitId} cleared (player order)");
    }

    public static void Tick(BattlefieldState bf, BattlefieldUnit producer, ModuleRegistry modules)
    {
        if (producer.IsDestroyed() || producer.isBuilding || producer.parentUnitId != null
            || BattlefieldSceneProxyService.IsSceneProxy(producer))
        {
            return;
        }

        if (!HasProducerModule(producer, modules))
        {
            return;
        }

        if (producer.logisticsAutoAimActive && producer.aiOrder != UnitAiOrder.APPROACH)
        {
            producer.logisticsAutoAimActive = false;
            CombatTelemetryLog.Log(
                "logistics.auto-aim",
                $"{producer.unitId} interrupted order={producer.aiOrder}");
            return;
        }

        if (!producer.logisticsAutoAimActive && producer.aiOrder != UnitAiOrder.IDLE)
        {
            return;
        }

        EnsureProducerModulesEnabled(producer);

        if (producer.logisticsAutoAimActive && producer.approachTargetUnitId != null)
        {
            var current = BattlefieldSystem.FindUnit(bf, producer.approachTargetUnitId);
            if (current != null && HasActiveProtectionField(bf, current, modules))
            {
                RefreshMaintainDistance(producer, modules);
                return;
            }
        }

        var ally = FindNearestProtectionAlly(bf, producer, modules);
        if (ally == null)
        {
            if (producer.logisticsAutoAimActive)
            {
                producer.logisticsAutoAimActive = false;
                producer.aiOrder = UnitAiOrder.IDLE;
                producer.approachTargetUnitId = null;
                producer.commandMaintainDistM = 0f;
                CombatTelemetryLog.Log("logistics.auto-aim", $"{producer.unitId} no field ally");
            }

            return;
        }

        var maintainM = ResolveMaintainDistanceM(producer, modules);
        producer.logisticsAutoAimActive = true;
        producer.aiOrder = UnitAiOrder.APPROACH;
        producer.approachTargetUnitId = ally.unitId;
        producer.approachHeadingTimerSec = 0f;
        producer.commandMaintainDistM = maintainM;
        CombatTelemetryLog.Log(
            "logistics.auto-aim",
            $"{producer.unitId}→{ally.unitId} maintain={maintainM:F0}m");
    }

    public static bool HasProducerModule(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod?.producerConsumableKind != null)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasActiveProtectionField(
        BattlefieldState bf,
        BattlefieldUnit unit,
        ModuleRegistry modules)
    {
        if (unit.IsDestroyed() || unit.fieldAuraEnabledAtSec <= 0f
            || unit.fieldAuraCollapseCooldownSec > bf.timeSec)
        {
            return false;
        }

        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod == null || !IsProtectionFieldModule(mod))
            {
                continue;
            }

            if (ModuleActivationService.IsFunctionModuleActive(unit, kv.Key, mod))
            {
                return true;
            }
        }

        return false;
    }

    public static BattlefieldUnit? FindNearestProtectionAlly(
        BattlefieldState bf,
        BattlefieldUnit producer,
        ModuleRegistry modules)
    {
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var ally in bf.units)
        {
            if (ally.IsDestroyed() || ally.side != producer.side || ReferenceEquals(ally, producer)
                || ally.isBuilding || ally.parentUnitId != null
                || BattlefieldSceneProxyService.IsSceneProxy(ally))
            {
                continue;
            }

            if (!HasActiveProtectionField(bf, ally, modules))
            {
                continue;
            }

            var dist = FieldAuraService.DistanceM(producer, ally);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = ally;
            }
        }

        return best;
    }

    private static void EnsureProducerModulesEnabled(BattlefieldUnit producer)
    {
        if (producer.fieldAuraEnabledAtSec > 0f)
        {
            return;
        }

        producer.fieldAuraEnabledAtSec = 0.001f;
        CombatTelemetryLog.Log("logistics.producer-enable", $"{producer.unitId} fn modules on");
    }

    private static void RefreshMaintainDistance(BattlefieldUnit producer, ModuleRegistry modules)
    {
        var maintainM = ResolveMaintainDistanceM(producer, modules);
        if (MathF.Abs(producer.commandMaintainDistM - maintainM) > 1f)
        {
            producer.commandMaintainDistM = maintainM;
        }
    }

    private static float ResolveMaintainDistanceM(BattlefieldUnit producer, ModuleRegistry modules)
    {
        var radius = LogisticsProducerService.ResolveProducerRadiusM(producer, modules);
        return radius * MaintainRadiusFactor;
    }

    private static bool IsProtectionFieldModule(ModuleDef mod) =>
        HullLicenseCatalog.FleetProtectionFamily.Equals(mod.moduleFamily, StringComparison.Ordinal)
        || "shield_fusion_field".Equals(mod.moduleKind, StringComparison.Ordinal)
        || "armor_link_field".Equals(mod.moduleKind, StringComparison.Ordinal);
}
