using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 舰载机 · docs/TACTICAL_WARP_AND_ORDERS.md
 * 本文件: StrikeWingRecallService.cs — 航母不交战时收起舰载机
 * 【机制要点】
 * · 舰载机仅在集火指令时展开；非集火时收回 STRIKE_CRAFT
 * 【关联】StrikeWingSpawnService · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class StrikeWingRecallService
{
    public static void Tick(BattlefieldState bf, ModuleRegistry modules, Random rng)
    {
        foreach (var u in bf.units.ToList())
        {
            if (u.IsDestroyed() || u.isBuilding || u.parentUnitId != null || u.unitId == null)
            {
                continue;
            }

            if (!IsCarrier(u))
            {
                continue;
            }

            if (!CarrierWantsWingsDeployed(u) && !HasRecallingWings(bf, u.unitId))
            {
                RecallWings(bf, u);
            }
        }
    }

    internal static bool CarrierWantsWingsDeployed(BattlefieldUnit u) =>
        u.explicitFocus && u.aiOrder == UnitAiOrder.FOCUS;

    internal static bool IsCarrier(BattlefieldUnit u)
    {
        if ("CARRIER".Equals(u.tonnageClass, StringComparison.OrdinalIgnoreCase)
            || "SUPERCARRIER".Equals(u.tonnageClass, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var modId in u.fittedModules.Values)
        {
            if (!string.IsNullOrWhiteSpace(modId)
                && modId.Contains("strike_wing", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsEngaged(BattlefieldUnit u) =>
        CarrierWantsWingsDeployed(u);

    private static bool HasDeployedWings(BattlefieldState bf, string carrierUnitId)
    {
        foreach (var u in bf.units)
        {
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
                && !u.IsDestroyed())
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasRecallingWings(BattlefieldState bf, string carrierUnitId)
    {
        foreach (var u in bf.units)
        {
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && StrikeWingOrderService.IsDroneWing(u)
                && u.aiOrder == UnitAiOrder.RECALL)
            {
                return true;
            }
        }

        return false;
    }

    private static void RecallWings(BattlefieldState bf, BattlefieldUnit carrier)
    {
        var carrierUnitId = carrier.unitId;
        if (carrierUnitId == null)
        {
            return;
        }

        for (var i = bf.units.Count - 1; i >= 0; i--)
        {
            var u = bf.units[i];
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal))
            {
                bf.units.RemoveAt(i);
            }
        }

        LaunchTubeStateService.ResetStrikeWingTubesToInactive(carrier);
    }
}
