using TopDog.Content.Modules;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §6.1 · docs/TACTICAL_NAVIGATION.md §5
 * 本文件: RemoteRepairAutoTargetingService.cs — 无玩家指令时自动维修最近场域持有舰
 * 【机制要点】
 * · 仅装配 remote_repair 的舰参与
 * · 目标：最近已开启 fleet_protection_field（盾融/甲连）友舰
 * · 维持 pendingRepairRounds ≥ 1，由 RemoteRepairSalvoService 按射程执行
 * · 不覆盖玩家舰队指令（SuppressForPlayerOrder）；玩家 OrderRepairTarget 优先
 * 【关联】RemoteRepairSalvoService · LogisticsAutoTargetingService · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class RemoteRepairAutoTargetingService
{
    public static void SuppressForPlayerOrder(BattlefieldUnit unit)
    {
        if (!unit.remoteRepairAutoActive)
        {
            return;
        }

        unit.remoteRepairAutoActive = false;
        CombatTelemetryLog.Log("repair.auto-aim", $"{unit.unitId} cleared (player order)");
    }

    public static void TickAll(BattlefieldState bf, ModuleRegistry modules)
    {
        foreach (var u in bf.units)
        {
            Tick(bf, u, modules);
        }
    }

    public static void Tick(BattlefieldState bf, BattlefieldUnit healer, ModuleRegistry modules)
    {
        if (healer.IsDestroyed() || healer.isBuilding || healer.parentUnitId != null
            || BattlefieldSceneProxyService.IsSceneProxy(healer))
        {
            return;
        }

        if (!RemoteRepairSalvoService.HasRemoteRepairModule(healer, modules))
        {
            return;
        }

        if (healer.remoteRepairAutoActive && healer.aiOrder != UnitAiOrder.IDLE)
        {
            healer.remoteRepairAutoActive = false;
            CombatTelemetryLog.Log(
                "repair.auto-aim",
                $"{healer.unitId} interrupted order={healer.aiOrder}");
            return;
        }

        if (!healer.remoteRepairAutoActive && healer.aiOrder != UnitAiOrder.IDLE)
        {
            return;
        }

        if (!healer.remoteRepairAutoActive && healer.pendingRepairRounds > 0
            && healer.targetUnitId != null)
        {
            return;
        }

        if (healer.remoteRepairAutoActive && healer.targetUnitId != null)
        {
            var current = BattlefieldSystem.FindUnit(bf, healer.targetUnitId);
            if (current != null
                && LogisticsAutoTargetingService.HasActiveProtectionField(bf, current, modules))
            {
                EnsureRepairQueued(healer);
                return;
            }
        }

        var ally = LogisticsAutoTargetingService.FindNearestProtectionAlly(bf, healer, modules);
        if (ally?.unitId == null)
        {
            if (healer.remoteRepairAutoActive)
            {
                healer.remoteRepairAutoActive = false;
                healer.targetUnitId = null;
                CombatTelemetryLog.Log("repair.auto-aim", $"{healer.unitId} no field ally");
            }

            return;
        }

        healer.remoteRepairAutoActive = true;
        healer.targetUnitId = ally.unitId;
        EnsureRepairQueued(healer);
        CombatTelemetryLog.Log(
            "repair.auto-aim",
            $"{healer.unitId}→{ally.unitId} rounds={healer.pendingRepairRounds}");
    }

    private static void EnsureRepairQueued(BattlefieldUnit healer)
    {
        if (healer.pendingRepairRounds < 1)
        {
            healer.pendingRepairRounds = 1;
        }
    }
}
