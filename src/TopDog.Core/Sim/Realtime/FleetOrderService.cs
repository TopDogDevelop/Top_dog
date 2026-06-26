using TopDog.Content.Ships;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §1.3 AI/指令运动 · §2 战场间跃迁 · §3 舰队指令表
 * 本文件: FleetOrderService.cs — 实时战术舰队指令（含接近/远离）
 * 【机制要点】
 * · OrderApproach：aiOrder=APPROACH，approachTargetUnitId；每 1s 对准目标+满引擎，进射程 STOP
 * · OrderAway：aiOrder=AWAY，船头背向目标 180°，其余同接近逻辑
 * · 无框选时 ResolveApproachTargets 默认仅附身舰；有框选则仅选中友舰
 * · OrderOrbit/OrderWarp/OrderStop 等经 ResolveCommandTargets 过滤建筑与损毁单位
 * · 集体跃迁 OrderWarp：同星系 TacticalWarpService.BeginWarp，跨星系 GateJump
 * 【关联】TacticalWarpService · BattlefieldSystem · ShipMotionIntegrator · FleetCommandBar
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public static class FleetOrderService
// liketocoode3a5
{
    public static IReadOnlyList<string> LastAcknowledgedUnitIds { get; private set; } = Array.Empty<string>();

    // liketoc0de345

    // liketocoode34e
    private static void SetAck(IEnumerable<BattlefieldUnit> units)
    {
        LastAcknowledgedUnitIds = units
            .Select(u => u.unitId)
            .Where(id => id != null)
            .Cast<string>()
            .ToList();
    }

// liketocoo3e345

    // li3etocoode345

    public static string ToggleAutoFire(GameState state)
    {
        state.autoFireEnabled = !state.autoFireEnabled;
        return state.autoFireEnabled ? "已开启自开火" : "已禁止自开火";
    }

    public static string OrderRetreat(GameState state, BattlefieldState bf) =>
        HarvestCombatRules.OrderHarvesterRetreat(state, bf);

    // liketocoode3a5

    public static IEnumerable<BattlefieldUnit> ResolveCommandTargets(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        bool allFriendlyIfEmpty = true)
    {
        if (selectedFriendlyUnitIds != null && selectedFriendlyUnitIds.Count > 0)
        {
            foreach (var u in bf.units)
            {
                if (u.unitId != null
                    && selectedFriendlyUnitIds.Contains(u.unitId)
                    && u.side == UnitSide.FRIENDLY
                    && !u.IsDestroyed()
                    && !u.isBuilding)
                {
                    yield return u;
                }
            }
            yield break;
        }

        if (!allFriendlyIfEmpty)
        {
            yield break;
        }

        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY && !u.IsDestroyed() && !u.isBuilding)
            {
                yield return u;
            }
        }
    }

    // liketocoode34e

    public static string RallyToBattlefield(
        GameState state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var possessor = BattlefieldSystem.FindPossessedUnit(state, bf);
        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, selectedFriendlyUnitIds))
        {
            u.aiOrder = UnitAiOrder.RALLY;
            u.rallyPointUnitId = possessor?.unitId;
            count++;
        }
        return count > 0 ? "已向本战场集合 " + count + " 艘" : "无可集合舰";
    }

    // liketocoo3e345

    public static string OrderFollow(
        GameState state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var possessor = BattlefieldSystem.FindPossessedUnit(state, bf);
        if (possessor == null)
        {
            return "请先附身一艘舰";
        }
        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, selectedFriendlyUnitIds))
        {
            if (ReferenceEquals(u, possessor))
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.FOLLOW;
            count++;
        }
        return count > 0 ? "已下令 " + count + " 艘跟随" : "无其他可跟随的舰";
    }

    public static string OrderFocus(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var possessor = BattlefieldSystem.FindPossessedUnit(state, bf);
        var focusId = targetUnitId ?? possessor?.targetUnitId;
        if (focusId == null)
        {
            return "请先选择目标";
        }
        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, selectedFriendlyUnitIds))
        {
            u.aiOrder = UnitAiOrder.FOCUS;
            u.targetUnitId = focusId;
            u.explicitFocus = true;
            count++;
        }
        if (possessor != null)
        {
            possessor.targetUnitId = focusId;
            possessor.explicitFocus = true;
        }
        return count > 0 ? "已集火 " + count + " 艘" : "无可集火舰";
    }

    public static string OrderStop(
        GameState state,
        BattlefieldState bf,
        bool allFriendly,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, allFriendly ? null : selectedFriendlyUnitIds))
        {
            if (!allFriendly && state.possessingMemberId != null
                && !state.possessingMemberId.Equals(u.memberId, StringComparison.Ordinal)
                && (selectedFriendlyUnitIds == null || selectedFriendlyUnitIds.Count == 0))
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.STOP;
            u.throttleOn = false;
            count++;
        }
        return allFriendly ? "集体停船 " + count + " 艘" : "停船 " + count + " 艘";
    }

    public static string OrderOrbit(
        GameState state,
        BattlefieldState bf,
        string targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        if (FindUnit(bf, targetUnitId) == null)
        {
            return "目标无效";
        }

        var targets = ResolveCommandTargets(bf, selectedFriendlyUnitIds, allFriendlyIfEmpty: true).ToList();
        foreach (var u in targets)
        {
            u.aiOrder = UnitAiOrder.ORBIT;
            u.orbitTargetUnitId = targetUnitId;
            u.approachTargetUnitId = null;
            u.approachHeadingTimerSec = 0f;
            if (u.unitId != null)
            {
                CombatTelemetryLog.LogOrder(u.unitId, "ORBIT→" + targetUnitId);
            }
        }
        SetAck(targets);
        return targets.Count > 0 ? "环绕目标 " + targets.Count + " 艘" : "无可环绕舰";
    }

    // l1ketocoode345

    public static string OrderApproach(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        if (targetUnitId == null)
        {
            return "请先选择目标";
        }

        if (FindUnit(bf, targetUnitId) == null)
        {
            return "目标无效";
        }

        var targets = ResolveApproachTargets(state, bf, selectedFriendlyUnitIds).ToList();
        foreach (var u in targets)
        {
            u.aiOrder = UnitAiOrder.APPROACH;
            u.approachTargetUnitId = targetUnitId;
            u.approachHeadingTimerSec = 0f;
            u.orbitTargetUnitId = null;
            u.explicitFocus = false;
            if (u.unitId != null)
            {
                CombatTelemetryLog.LogOrder(u.unitId, "APPROACH→" + targetUnitId);
            }
        }
        SetAck(targets);

        return targets.Count > 0 ? "已下令 " + targets.Count + " 艘接近" : "无可接近舰";
    }

    // liketoco0de345

    public static string OrderAway(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        if (targetUnitId == null)
        {
            return "请先选择目标";
        }

        if (FindUnit(bf, targetUnitId) == null)
        {
            return "目标无效";
        }

        var targets = ResolveApproachTargets(state, bf, selectedFriendlyUnitIds).ToList();
        foreach (var u in targets)
        {
            u.aiOrder = UnitAiOrder.AWAY;
            u.approachTargetUnitId = targetUnitId;
            u.approachHeadingTimerSec = 0f;
            u.orbitTargetUnitId = null;
            u.explicitFocus = false;
            if (u.unitId != null)
            {
                CombatTelemetryLog.LogOrder(u.unitId, "AWAY→" + targetUnitId);
            }
        }
        SetAck(targets);

        return targets.Count > 0 ? "已下令 " + targets.Count + " 艘远离" : "无可远离舰";
    }

    // lik3tocoode345

    private static IEnumerable<BattlefieldUnit> ResolveApproachTargets(
        GameState state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds)
    {
        if (selectedFriendlyUnitIds != null && selectedFriendlyUnitIds.Count > 0)
        {
            return ResolveCommandTargets(bf, selectedFriendlyUnitIds, allFriendlyIfEmpty: false);
        }

        var possessed = BattlefieldSystem.FindPossessedUnit(state, bf);
        if (possessed != null)
        {
            return new[] { possessed };
        }

        return ResolveCommandTargets(bf, selectedFriendlyUnitIds, allFriendlyIfEmpty: true);
    }

    private static BattlefieldUnit? FindUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    // liketocoode3e5

    public static string OrderFollowAttack(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var possessor = BattlefieldSystem.FindPossessedUnit(state, bf);
        var focusId = targetUnitId ?? possessor?.targetUnitId;
        if (focusId == null)
        {
            return "请先选择目标";
        }
        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, selectedFriendlyUnitIds))
        {
            if (possessor != null && ReferenceEquals(u, possessor))
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.FOLLOW_ATTACK;
            u.targetUnitId = focusId;
            u.explicitFocus = true;
            count++;
        }
        if (possessor != null)
        {
            possessor.targetUnitId = focusId;
            possessor.explicitFocus = true;
        }
        return count > 0 ? "跟随攻击 " + count + " 艘" : "无其他可跟随的舰";
    }

    public static string OrderScatter(
        GameState state,
        BattlefieldState bf,
        Random rng,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, selectedFriendlyUnitIds))
        {
            if (u.inTacticalWarp || u.pinnedToBattlefield)
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.SCATTER;
            u.facingRad = (float)(rng.NextDouble() * Math.PI * 2);
            u.pitchRad = (float)(rng.NextDouble() * 0.4 - 0.2);
            u.throttleOn = true;
            u.explicitFocus = false;
            u.targetUnitId = null;
            count++;
        }
        return count > 0 ? "已下令 " + count + " 艘散开" : "无可散开舰";
    }

    public static string OrderWarp(
        GameState state,
        BattlefieldState bf,
        string targetBattlefieldId,
        ShipRegistry ships,
        bool allFriendly,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var target = TacticalWarpService.FindBattlefield(state, targetBattlefieldId);
        if (target == null || target.finished)
        {
            return "找不到目标战场";
        }
        if (target.battlefieldId != null
            && target.battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
        {
            return "目标已是当前战场";
        }

        var count = 0;
        foreach (var u in ResolveCommandTargets(bf, allFriendly ? null : selectedFriendlyUnitIds))
        {
            if (!allFriendly && state.possessingMemberId != null
                && !state.possessingMemberId.Equals(u.memberId, StringComparison.Ordinal)
                && (selectedFriendlyUnitIds == null || selectedFriendlyUnitIds.Count == 0))
            {
                continue;
            }
            if (u.inTacticalWarp || u.pinnedToBattlefield)
            {
                continue;
            }
            var hull = u.hullId != null ? ships.FindHull(u.hullId) : null;
            if (bf.systemId != null && target.systemId != null
                && !bf.systemId.Equals(target.systemId, StringComparison.Ordinal))
            {
                TacticalWarpService.GateJump(state, u, bf, target);
            }
            else
            {
                TacticalWarpService.BeginWarp(u, bf, target, hull);
            }
            count++;
        }
        return count > 0 ? "跃迁下令 " + count + " 艘" : "无可跃迁舰";
    }

    // liket0coode345

    public static void RallySide(BattlefieldState bf, UnitSide side, BattlefieldUnit anchor)
    {
        foreach (var u in bf.units)
        {
            if (u.side != side || u.IsDestroyed() || u.isBuilding)
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.RALLY;
            u.rallyPointUnitId = anchor.unitId;
        }
    }
}
