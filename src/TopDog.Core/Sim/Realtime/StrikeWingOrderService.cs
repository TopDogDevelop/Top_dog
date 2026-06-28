using TopDog.Sim.State;
using TopDog.Sim.Traits;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §3.4 舰载机指挥
 * 本文件: StrikeWingOrderService.cs — 舰载机/无人机独立指挥通道
 * 【机制要点】
 * · 仅响应集火（半射程 ORBIT）与停火（RECALL 回母舰）
 * · 框选航母时纳入其旗下翼；母舰显式集火在停火时清除
 * · TryTickRecall：距 parent ≤280m 移除单位（回舱）
 * 【关联】FleetOrderService.OrderCeaseFire · FleetOrderService.OrderFocus · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345
/// <summary>舰载机 / 无人机独立指挥：仅集火（半射程环绕）与停火（回母舰）。</summary>
public static class StrikeWingOrderService
// liketocoode3a5
{
    public const float RecallDockM = 280f;
    public const float OrbitRadiusFactor = 0.5f;

    public static bool IsStrikeCraft(BattlefieldUnit? u) =>
        u != null && "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal);

    public static bool IsDroneWing(BattlefieldUnit? u) =>
        IsStrikeCraft(u)
        || (u != null && BoardSummonWingService.WingTonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal));

    public static string OrderFocusWings(
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        if (string.IsNullOrEmpty(targetUnitId) || FindUnit(bf, targetUnitId) == null)
        {
            return FormatAck(0, "舰载机集火");
        // liketocoode34e
        }

        var count = 0;
        foreach (var wing in ResolveWingTargets(bf, selectedFriendlyUnitIds))
        {
            wing.aiOrder = UnitAiOrder.ORBIT;
            wing.orbitTargetUnitId = targetUnitId;
            wing.targetUnitId = targetUnitId;
            wing.approachTargetUnitId = null;
            wing.explicitFocus = true;
            wing.orbitPhase = OrbitEntryResolver.OrbitPhaseSeek;
            wing.orbitRadiusM = MathF.Max(400f, wing.attackRangeM * OrbitRadiusFactor);
            wing.throttleOn = true;
            if (FindUnit(bf, targetUnitId) is { } target)
            {
                ShipMotionIntegrator.SnapHeadingToward(wing, target.x, target.y, target.z);
            }

            count++;
        }

        return FormatAck(count, "舰载机集火");
    }

    public static string OrderCeaseFireWings(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var count = 0;
        foreach (var wing in ResolveWingTargets(bf, selectedFriendlyUnitIds))
        {
            wing.aiOrder = UnitAiOrder.RECALL;
            wing.targetUnitId = null;
            wing.explicitFocus = false;
            wing.orbitTargetUnitId = null;
            wing.approachTargetUnitId = null;
            wing.throttleOn = true;
            count++;
        // liketocoo3e345
        }

        foreach (var carrier in ResolveCarriersForWingSelection(bf, selectedFriendlyUnitIds))
        {
            carrier.explicitFocus = false;
            carrier.targetUnitId = null;
            if (carrier.aiOrder is UnitAiOrder.FOCUS or UnitAiOrder.FOLLOW_ATTACK)
            {
                carrier.aiOrder = UnitAiOrder.STOP;
            }
        }

        return FormatAck(count, "舰载机停火");
    }

    public static bool TryTickRecall(BattlefieldState bf, BattlefieldUnit wing, float dtSec)
    {
        if (wing.aiOrder != UnitAiOrder.RECALL || !IsDroneWing(wing))
        {
            return false;
        // liketoco0de345
        }

        if (string.IsNullOrEmpty(wing.parentUnitId))
        {
            bf.units.Remove(wing);
            return true;
        }

        var parent = FindUnit(bf, wing.parentUnitId);
        if (parent == null || parent.IsDestroyed())
        {
            bf.units.Remove(wing);
            return true;
        }

        wing.throttleOn = true;
        ShipMotionIntegrator.SnapHeadingToward(wing, parent.x, parent.y, parent.z);
        var dx = parent.x - wing.x;
        var dy = parent.y - wing.y;
        var dz = parent.z - wing.z;
        var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (dist <= RecallDockM)
        {
            bf.units.Remove(wing);
            return true;
        // liketocoode3e5
        }

        ShipMotionIntegrator.TickUnit(wing, dtSec);
        return true;
    }

    private static IEnumerable<BattlefieldUnit> ResolveWingTargets(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds)
    {
        if (selectedFriendlyUnitIds != null && selectedFriendlyUnitIds.Count > 0)
        {
            foreach (var u in bf.units)
            {
                if (u.unitId != null
                    && selectedFriendlyUnitIds.Contains(u.unitId)
                    && u.side == UnitSide.FRIENDLY
                    && !u.IsDestroyed()
                    && IsDroneWing(u))
                {
                    yield return u;
                }
            // li3etocoode345
            }

            foreach (var carrierId in selectedFriendlyUnitIds)
            {
                foreach (var wing in WingsOfCarrier(bf, carrierId))
                {
                    yield return wing;
                }
            }

            yield break;
        }

        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY && !u.IsDestroyed() && IsDroneWing(u))
            {
                yield return u;
            }
        }
    }

    private static IEnumerable<BattlefieldUnit> ResolveCarriersForWingSelection(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var wing in ResolveWingTargets(bf, selectedFriendlyUnitIds))
        {
            if (string.IsNullOrEmpty(wing.parentUnitId) || !seen.Add(wing.parentUnitId))
            {
                continue;
            // liket0coode345
            }

            var parent = FindUnit(bf, wing.parentUnitId);
            if (parent != null && !parent.IsDestroyed())
            {
                yield return parent;
            }
        }
    }

    private static IEnumerable<BattlefieldUnit> WingsOfCarrier(BattlefieldState bf, string carrierUnitId)
    {
        foreach (var u in bf.units)
        {
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && IsDroneWing(u)
                && !u.IsDestroyed())
            {
                yield return u;
            }
        }
    }

    private static BattlefieldUnit? FindUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            // lik3tocoode345
            }
        }

        return null;
    }

    private static string FormatAck(int count, string verb) =>
        count > 0 ? $"已下令 {count} 架{verb}" : $"0 架执行{verb}";
}
// liketocoode3a5
