using TopDog.Content.Ships;
using TopDog.AgentDiag;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §1.3 AI/指令运动 · §2 战场间跃迁 · §3 舰队指令表
 * 本文件: FleetOrderService.cs — 实时战术舰队指令（含接近/远离）
 * 【机制要点】
 * · OrderApproach/OrderAway：每 1s 对准 + 满引擎；可选 commandMaintainDistM；不设距不限距 STOP
 * · OrderOrbit：OrbitEntryResolver 几何切入点 + 圆轨道
 * · OrderEnterBuilding：跨星系跳桥无延迟到对端
 * · 集体跃迁 OrderWarp：同星系 AU 伪跃迁；跨星系仅跳桥（OrderEnterBuilding）
 * 【实现逻辑】
 * · EnsureCommandSceneReady：仅 SeedSceneProxies（本场景 + proxy 目标场景懒加载）；禁止 Sync/remove proxy
 * 【关联】TacticalWarpService · BattlefieldSystem · ShipMotionIntegrator · FleetCommandBar
 * ══
 */

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §1.3 AI/指令运动 · §2 战场间跃迁 · §3 舰队指令表
 * 本文件: FleetOrderService.cs — 实时战术舰队指令（含接近/远离）
 * 【机制要点】
 * · OrderApproach：aiOrder=APPROACH，approachTargetUnitId；每 1s 对准目标+满引擎，进射程 STOP
 * · OrderAway：aiOrder=AWAY，船头背向目标 180°，其余同接近逻辑
 * · 无框选时「全体」= 当前场景本方团员各一艘主舰（非全场全部友方单位）；集合 Rally 除外
 * · OrderOrbit/OrderWarp/OrderStop 等经 ResolveCommandTargets(state,…) 过滤建筑与损毁单位
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

    private static string FormatOrderAck(int count, string verb) =>
        count > 0 ? $"已下令 {count} 艘{verb}" : $"0 艘执行{verb}";

// liketocoo3e345

    // li3etocoode345

    public static string ToggleAutoFire(GameState state)
    {
        state.autoFireEnabled = !state.autoFireEnabled;
        return state.autoFireEnabled ? "已开启自开火" : "已禁止自开火";
    }

    public static bool TryResolveWarpTargetScene(
        GameState state,
        BattlefieldState bf,
        string? selectedUnitId,
        out string systemId,
        out string eventRegionId)
    {
        systemId = "";
        eventRegionId = "";
        var u = FindUnit(bf, selectedUnitId);
        if (u != null
            && BattlefieldSceneProxyService.TryGetTargetScene(u, out systemId, out eventRegionId))
        {
            return IsSameSystemWarpTarget(bf, systemId);
        }

        if (!BattlefieldSceneProxyService.TryParseProxyUnitId(selectedUnitId, bf.systemId, out systemId, out eventRegionId))
        {
            return false;
        }

        if (!IsSameSystemWarpTarget(bf, systemId))
        {
            systemId = "";
            eventRegionId = "";
            return false;
        }

        BattlefieldSceneProxyService.EnsureProxyUnit(state, bf, systemId, eventRegionId);
        return true;
    }

    public static bool TryResolveWarpTargetScene(
        BattlefieldState bf,
        string? selectedUnitId,
        out string systemId,
        out string eventRegionId)
    {
        systemId = "";
        eventRegionId = "";
        var u = FindUnit(bf, selectedUnitId);
        if (!BattlefieldSceneProxyService.TryGetTargetScene(u, out systemId, out eventRegionId))
        {
            return false;
        }

        return IsSameSystemWarpTarget(bf, systemId);
    }

    private static bool IsSameSystemWarpTarget(BattlefieldState bf, string systemId) =>
        bf.systemId != null && bf.systemId.Equals(systemId, StringComparison.Ordinal);

    public static string? ResolveWarpTargetBattlefieldId(
        GameState state,
        BattlefieldState bf,
        string? selectedUnitId)
    {
        EnsureCommandSceneReady(state, bf, selectedUnitId);
        if (!TryResolveWarpTargetScene(state, bf, selectedUnitId, out var systemId, out var eventRegionId))
        {
            return null;
        }

        return TacticalSceneBattlefieldService.EnsureSceneBattlefield(state, systemId, eventRegionId).battlefieldId;
    }

    /// <summary>跃迁/接近前确保目标场景战场已懒加载且占位已 seed（不重复 sync）。</summary>
    public static void EnsureCommandSceneReady(GameState state, BattlefieldState bf, string? targetUnitId = null)
    {
        if (!state.combatRealtimeActive || bf.battlefieldId == null || state.map?.Project == null)
        {
            return;
        }

        BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        if (targetUnitId != null
            && BattlefieldSceneProxyService.TryParseProxyUnitId(targetUnitId, bf.systemId, out var sys, out var region))
        {
            BattlefieldSceneProxyService.EnsureProxyUnit(state, bf, sys, region);
            var dest = TacticalSceneBattlefieldService.EnsureSceneBattlefield(state, sys, region);
            BattlefieldSceneProxyService.SeedSceneProxies(state, dest);
            return;
        }

        if (targetUnitId != null
            && FindUnit(bf, targetUnitId) is { } t
            && BattlefieldSceneProxyService.IsSceneProxy(t)
            && t.sceneProxyTargetSystemId != null
            && t.sceneProxyTargetEventRegionId != null)
        {
            var dest = TacticalSceneBattlefieldService.EnsureSceneBattlefield(
                state, t.sceneProxyTargetSystemId, t.sceneProxyTargetEventRegionId);
            BattlefieldSceneProxyService.SeedSceneProxies(state, dest);
        }
    }

    public static bool IsCommandTarget(BattlefieldState bf, string? unitId, out BattlefieldUnit? target)
    {
        target = unitId != null ? FindUnit(bf, unitId) : null;
        return target != null && !target.IsDestroyed();
    }

    /// <summary>接近/环绕/集火：支持场景 proxy 的 UI fallback unitId（懒创建占位）。</summary>
    public static bool TryResolveCommandTarget(
        GameState state,
        BattlefieldState bf,
        string? unitId,
        out BattlefieldUnit? target)
    {
        EnsureCommandSceneReady(state, bf, unitId);
        if (IsCommandTarget(bf, unitId, out target))
        {
            return true;
        }

        if (!BattlefieldSceneProxyService.TryParseProxyUnitId(unitId, bf.systemId, out var sys, out var region))
        {
            target = null;
            return false;
        }

        target = BattlefieldSceneProxyService.EnsureProxyUnit(state, bf, sys, region);
        return target != null && !target.IsDestroyed();
    }

    private static bool IsValidFireTarget(BattlefieldUnit? target) =>
        target != null && !target.IsDestroyed() && !BattlefieldSceneProxyService.IsSceneProxy(target);

    public static string OrderRetreat(GameState state, BattlefieldState bf) =>
        HarvestCombatRules.OrderHarvesterRetreat(state, bf);

    // liketocoode3a5

    public static IEnumerable<BattlefieldUnit> ResolveCommandTargets(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        bool allFriendlyIfEmpty = true) =>
        ResolveCommandTargets(null, bf, selectedFriendlyUnitIds, allFriendlyIfEmpty, sceneMembersWhenEmpty: false);

    public static IEnumerable<BattlefieldUnit> ResolveCommandTargets(
        GameState? state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        bool allFriendlyIfEmpty = true,
        bool sceneMembersWhenEmpty = true)
    {
        foreach (var u in ResolveRawCommandTargets(
                     state,
                     bf,
                     selectedFriendlyUnitIds,
                     allFriendlyIfEmpty,
                     sceneMembersWhenEmpty))
        {
            if (AcceptsFleetMovementOrder(u))
            {
                yield return u;
            }
        }
    }

    public static IEnumerable<BattlefieldUnit> ResolveFocusTargets(
        GameState? state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        bool allFriendlyIfEmpty = true,
        bool sceneMembersWhenEmpty = true)
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

        foreach (var u in ResolveRawCommandTargets(
                     state,
                     bf,
                     selectedFriendlyUnitIds,
                     allFriendlyIfEmpty,
                     sceneMembersWhenEmpty))
        {
            if (AcceptsFleetMovementOrder(u))
            {
                yield return u;
            }
        }
    }

    public static IEnumerable<BattlefieldUnit> ResolveFocusTargets(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        bool allFriendlyIfEmpty = true) =>
        ResolveFocusTargets(null, bf, selectedFriendlyUnitIds, allFriendlyIfEmpty, sceneMembersWhenEmpty: false);

    private static IEnumerable<BattlefieldUnit> ResolveSceneMemberCommandUnits(
        GameState state,
        BattlefieldState bf)
    {
        var picked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in state.members)
        {
            if (member.memberId == null || member.legionId == null)
            {
                continue;
            }

            if (!IsLocalLegionMember(state, member))
            {
                continue;
            }

            BattlefieldUnit? ship = null;
            foreach (var u in bf.units)
            {
                if (!member.memberId.Equals(u.memberId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || u.isBuilding)
                {
                    continue;
                }

                if (!AcceptsFleetMovementOrder(u))
                {
                    continue;
                }

                ship = u;
                break;
            }

            if (ship != null && picked.Add(member.memberId))
            {
                yield return ship;
            }
        }

        // #region agent log
        AgentSessionDebugLog.Write(
            "H5",
            "FleetOrderService.ResolveSceneMemberCommandUnits",
            "resolved",
            new { bfId = bf.battlefieldId, count = picked.Count });
        // #endregion
    }

    private static bool IsLocalLegionMember(GameState state, MemberState member)
    {
        if (member.legionId == null)
        {
            return false;
        }

        foreach (var legion in state.legions)
        {
            if (legion.isLocal && member.legionId.Equals(legion.legionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<BattlefieldUnit> ResolveRawCommandTargets(
        GameState? state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        bool allFriendlyIfEmpty,
        bool sceneMembersWhenEmpty)
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

        if (state != null && sceneMembersWhenEmpty)
        {
            foreach (var u in ResolveSceneMemberCommandUnits(state, bf))
            {
                yield return u;
            }

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

    private static bool AcceptsFleetMovementOrder(BattlefieldUnit u) =>
        !u.IsBallisticMissile()
        && !BattlefieldSceneProxyService.IsSceneProxy(u)
        && (u.parentUnitId == null
            || !("STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
                || BoardSummonWingService.WingTonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal)));

    // liketocoode34e

    public static string RallyToBattlefield(
        GameState state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var possessor = BattlefieldSystem.FindPossessedUnit(state, bf);
        var count = 0;
        foreach (var u in ResolveCommandTargets(state, bf, selectedFriendlyUnitIds))
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
        foreach (var u in ResolveCommandTargets(state, bf, selectedFriendlyUnitIds))
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
        EnsureCommandSceneReady(state, bf, targetUnitId);
        var possessor = BattlefieldSystem.FindPossessedUnit(state, bf);
        var focusId = targetUnitId ?? possessor?.targetUnitId;
        var targets = ResolveFocusTargets(state, bf, selectedFriendlyUnitIds).ToList();
        if (focusId != null)
        {
            foreach (var u in targets)
            {
                u.aiOrder = UnitAiOrder.FOCUS;
                u.targetUnitId = focusId;
                u.explicitFocus = true;
            }

            if (possessor != null)
            {
                possessor.targetUnitId = focusId;
                possessor.explicitFocus = true;
            }
        }

        SetAck(targets);
        var wingMsg = StrikeWingOrderService.OrderFocusWings(bf, focusId, selectedFriendlyUnitIds);
        return FormatOrderAck(targets.Count, "集火")
            + (wingMsg.Contains('0') ? "" : "；" + wingMsg);
    }

    public static string OrderStop(
        GameState state,
        BattlefieldState bf,
        bool allFriendly,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var count = 0;
        foreach (var u in ResolveCommandTargets(state, bf, allFriendly ? null : selectedFriendlyUnitIds))
        {
            if (!allFriendly && state.possessingMemberId != null
                && !state.possessingMemberId.Equals(u.memberId, StringComparison.Ordinal)
                && (selectedFriendlyUnitIds == null || selectedFriendlyUnitIds.Count == 0))
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.STOP;
            u.approachTargetUnitId = null;
            u.orbitTargetUnitId = null;
            u.targetUnitId = null;
            u.explicitFocus = false;
            u.commandMaintainDistM = 0f;
            u.orbitRadiusM = 0f;
            u.orbitPhase = 0f;
            u.approachHeadingTimerSec = 0f;
            TacticalWarpService.CancelWarpPrep(u);
            u.throttleOn = false;
            u.vx = 0f;
            u.vy = 0f;
            u.vz = 0f;
            count++;
        }

        EnsureCommandSceneReady(state, bf);
        return allFriendly ? "集体停船 " + count + " 艘" : "停船 " + count + " 艘";
    }

    public static string OrderCeaseFire(
        GameState state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null) =>
        StrikeWingOrderService.OrderCeaseFireWings(bf, selectedFriendlyUnitIds);

    public static string OrderOrbit(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null,
        float? rangeKm = null)
    {
        EnsureCommandSceneReady(state, bf, targetUnitId);
        var targets = ResolveCommandTargets(state, bf, selectedFriendlyUnitIds, allFriendlyIfEmpty: true).ToList();
        if (!TryResolveCommandTarget(state, bf, targetUnitId, out var orbitTarget))
        {
            return FormatOrderAck(0, "环绕");
        }

        var orbitRadiusM = rangeKm.HasValue ? TacticalRangeScale.KmToMeters(rangeKm.Value) : 0f;
        var count = 0;
        foreach (var u in targets)
        {
            u.aiOrder = UnitAiOrder.ORBIT;
            u.orbitTargetUnitId = targetUnitId;
            u.approachTargetUnitId = null;
            u.approachHeadingTimerSec = 0f;
            u.orbitPhase = OrbitEntryResolver.OrbitPhaseSeek;
            u.orbitRadiusM = orbitRadiusM;
            u.explicitFocus = false;
            ShipMotionIntegrator.SnapHeadingTowardWhenTicking(state, bf, u, orbitTarget!.x, orbitTarget.y, orbitTarget.z);

            if (u.unitId != null)
            {
                CombatTelemetryLog.LogOrder(u.unitId, "ORBIT→" + targetUnitId);
            }

            count++;
        }

        SetAck(targets.Take(count).ToList());
        return FormatOrderAck(count, "环绕");
    }

    // l1ketocoode345

    public static string OrderApproach(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null,
        float? rangeKm = null)
    {
        EnsureCommandSceneReady(state, bf, targetUnitId);
        var targets = ResolveApproachTargets(state, bf, selectedFriendlyUnitIds).ToList();
        var maintainM = rangeKm.HasValue ? TacticalRangeScale.KmToMeters(rangeKm.Value) : 0f;
        if (!TryResolveCommandTarget(state, bf, targetUnitId, out var approachTarget))
        {
            AgentSessionDebugLog.Write(
                "W1",
                "FleetOrderService.OrderApproach",
                "target_missing",
                new { targetUnitId, bfId = bf.battlefieldId });
            return FormatOrderAck(0, "接近");
        }

        var count = 0;
        foreach (var u in targets)
        {
            u.aiOrder = UnitAiOrder.APPROACH;
            u.approachTargetUnitId = targetUnitId;
            u.approachHeadingTimerSec = 0f;
            u.commandMaintainDistM = maintainM;
            u.orbitTargetUnitId = null;
            u.explicitFocus = false;
            u.throttleOn = true;
            ShipMotionIntegrator.SnapHeadingTowardWhenTicking(state, bf, u, approachTarget!.x, approachTarget.y, approachTarget.z);

            if (u.unitId != null)
            {
                CombatTelemetryLog.LogOrder(u.unitId, "APPROACH→" + targetUnitId);
            }

            count++;
        }

        AgentSessionDebugLog.Write(
            "W1",
            "FleetOrderService.OrderApproach",
            "result",
            new { targetUnitId, count, proxy = BattlefieldSceneProxyService.IsSceneProxy(approachTarget) });

        SetAck(targets.Take(count).ToList());
        return FormatOrderAck(count, "接近");
    }

    // liketoco0de345

    public static string OrderAway(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null,
        float? rangeKm = null)
    {
        EnsureCommandSceneReady(state, bf, targetUnitId);
        var targets = ResolveApproachTargets(state, bf, selectedFriendlyUnitIds).ToList();
        var maintainM = rangeKm.HasValue ? TacticalRangeScale.KmToMeters(rangeKm.Value) : 0f;
        if (!TryResolveCommandTarget(state, bf, targetUnitId, out var awayTarget))
        {
            return FormatOrderAck(0, "远离");
        }

        var count = 0;
        foreach (var u in targets)
        {
            u.aiOrder = UnitAiOrder.AWAY;
            u.approachTargetUnitId = targetUnitId;
            u.approachHeadingTimerSec = 0f;
            u.commandMaintainDistM = maintainM;
            u.orbitTargetUnitId = null;
            u.explicitFocus = false;
            u.throttleOn = true;
            ShipMotionIntegrator.SnapHeadingAwayWhenTicking(state, bf, u, awayTarget!.x, awayTarget.y, awayTarget.z);

            if (u.unitId != null)
            {
                CombatTelemetryLog.LogOrder(u.unitId, "AWAY→" + targetUnitId);
            }

            count++;
        }

        SetAck(targets.Take(count).ToList());
        return FormatOrderAck(count, "远离");
    }

    // lik3tocoode345

    private static IEnumerable<BattlefieldUnit> ResolveApproachTargets(
        GameState state,
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds) =>
        ResolveCommandTargets(state, bf, selectedFriendlyUnitIds, allFriendlyIfEmpty: true);

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
        var targets = ResolveCommandTargets(state, bf, selectedFriendlyUnitIds).ToList();
        var count = 0;
        foreach (var u in targets)
        {
            if (possessor != null && ReferenceEquals(u, possessor))
            {
                continue;
            }

            if (focusId == null)
            {
                continue;
            }

            u.aiOrder = UnitAiOrder.FOLLOW_ATTACK;
            u.targetUnitId = focusId;
            u.explicitFocus = true;
            count++;
        }

        if (possessor != null && focusId != null)
        {
            possessor.targetUnitId = focusId;
            possessor.explicitFocus = true;
        }

        return FormatOrderAck(count, "跟随攻击");
    }

    public static string OrderScatter(
        GameState state,
        BattlefieldState bf,
        Random rng,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        var count = 0;
        foreach (var u in ResolveCommandTargets(state, bf, selectedFriendlyUnitIds))
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

    /// <summary>同场景 &gt;150km 高速跃迁至世界坐标（不经场景边缘占位）。</summary>
    public static string OrderIntraSceneWarp(
        GameState state,
        BattlefieldState bf,
        float targetX,
        float targetY,
        float targetZ,
        ShipRegistry ships,
        bool allFriendly,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        EnsureCommandSceneReady(state, bf);
        var count = 0;
        string? lastError = null;
        foreach (var u in ResolveCommandTargets(state, bf, allFriendly ? null : selectedFriendlyUnitIds))
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

            var err = TacticalWarpService.TryOrderIntraSceneWarp(
                state,
                u,
                bf,
                targetX,
                targetY,
                targetZ,
                u.hullId != null ? ships.FindHull(u.hullId) : null);
            if (err == null)
            {
                count++;
            }
            else
            {
                lastError ??= err;
            }
        }

        return count > 0 ? FormatOrderAck(count, "同场景跃迁") : (lastError ?? FormatOrderAck(0, "同场景跃迁"));
    }

    public static string OrderWarpToSceneTarget(
        GameState state,
        BattlefieldState bf,
        string? sceneOrUnitTargetId,
        ShipRegistry ships,
        bool allFriendly,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null,
        float? landingKm = null)
    {
        AgentSessionDebugLog.Write(
            "H5",
            "FleetOrderService.OrderWarpToSceneTarget",
            "entry",
            new
            {
                bfId = bf.battlefieldId,
                systemId = bf.systemId,
                targetId = sceneOrUnitTargetId,
                allFriendly,
                selCount = selectedFriendlyUnitIds?.Count ?? 0,
                combatRealtime = state.combatRealtimeActive,
            });

        EnsureCommandSceneReady(state, bf, sceneOrUnitTargetId);
        if (sceneOrUnitTargetId != null
            && FindUnit(bf, sceneOrUnitTargetId) is { } sameBfUnit
            && !BattlefieldSceneProxyService.IsSceneProxy(sameBfUnit)
            && !sameBfUnit.isBuilding)
        {
            var intraSource = ResolveWarpSourceBattlefield(state, bf);
            return OrderIntraSceneWarp(
                state,
                intraSource,
                sameBfUnit.x,
                sameBfUnit.y,
                sameBfUnit.z,
                ships,
                allFriendly,
                selectedFriendlyUnitIds);
        }

        var targetBfId = ResolveWarpTargetBattlefieldId(state, bf, sceneOrUnitTargetId);
        if (targetBfId == null)
        {
            var fail = "请先选中「其他场景」跃迁目标";
            AgentSessionDebugLog.Write(
                "H5",
                "FleetOrderService.OrderWarpToSceneTarget",
                "resolve_failed",
                new { msg = fail, targetId = sceneOrUnitTargetId });
            return fail;
        }

        var sourceBf = ResolveWarpSourceBattlefield(state, bf);
        if (sourceBf.battlefieldId != null
            && bf.battlefieldId != null
            && !sourceBf.battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
        {
            EnsureCommandSceneReady(state, sourceBf);
        }

        var msg = OrderWarp(
            state,
            sourceBf,
            targetBfId,
            ships,
            allFriendly,
            selectedFriendlyUnitIds,
            landingKm);

        if (msg.StartsWith("已下令", StringComparison.Ordinal)
            && sourceBf.battlefieldId != null
            && bf.battlefieldId != null
            && !sourceBf.battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
        {
            FocusWarpExecutionBattlefield(state, sourceBf);
        }

        AgentSessionDebugLog.Write(
            "H5",
            "FleetOrderService.OrderWarpToSceneTarget",
            "result",
            new { msg, targetBfId, sourceBfId = sourceBf.battlefieldId });

        return msg;
    }

    /// <summary>从其他场景视野下令跃迁后，切到舰船所在战场并跟随跃迁单位。</summary>
    private static void FocusWarpExecutionBattlefield(GameState state, BattlefieldState sourceBf)
    {
        if (sourceBf.battlefieldId != null)
        {
            state.activeBattlefieldId = sourceBf.battlefieldId;
        }

        BattlefieldUnit? focus = null;
        foreach (var u in sourceBf.units)
        {
            if (u.side == UnitSide.FRIENDLY
                && !u.IsDestroyed()
                && u.warpPhase != TacticalWarpPhase.None
                && u.unitId != null)
            {
                focus = u;
                break;
            }
        }

        if (focus?.unitId != null)
        {
            state.tacticalCameraUnitId = focus.unitId;
        }
    }

    /// <summary>
    /// 跃迁下令战场：当前视野场景无友舰时，回退到同星系内有可跃迁友舰的战场（右栏切场后点场景跃迁）。
    /// </summary>
    internal static BattlefieldState ResolveWarpSourceBattlefield(GameState state, BattlefieldState viewBf)
    {
        if (HasWarpEligibleFriendlies(state, viewBf))
        {
            return viewBf;
        }

        if (state.possessingMemberId != null)
        {
            foreach (var candidate in EnumerateSameSystemBattlefields(state, viewBf))
            {
                if (FindMemberShip(candidate, state.possessingMemberId) != null)
                {
                    return candidate;
                }
            }
        }

        BattlefieldState? best = null;
        var bestCount = 0;
        foreach (var candidate in EnumerateSameSystemBattlefields(state, viewBf))
        {
            var count = CountWarpEligibleFriendlies(state, candidate);
            if (count > bestCount)
            {
                bestCount = count;
                best = candidate;
            }
        }

        return bestCount > 0 ? best! : viewBf;
    }

    private static IEnumerable<BattlefieldState> EnumerateSameSystemBattlefields(GameState state, BattlefieldState viewBf)
    {
        if (viewBf.systemId == null)
        {
            yield break;
        }

        foreach (var bf in state.battlefields)
        {
            if (bf.finished || bf.systemId == null)
            {
                continue;
            }

            if (viewBf.systemId.Equals(bf.systemId, StringComparison.Ordinal))
            {
                yield return bf;
            }
        }
    }

    private static bool HasWarpEligibleFriendlies(GameState state, BattlefieldState bf) =>
        CountWarpEligibleFriendlies(state, bf) > 0;

    private static int CountWarpEligibleFriendlies(GameState state, BattlefieldState bf)
    {
        var count = 0;
        foreach (var _ in ResolveCommandTargets(state, bf, null))
        {
            count++;
        }

        return count;
    }

    private static BattlefieldUnit? FindMemberShip(BattlefieldState bf, string memberId)
    {
        foreach (var u in bf.units)
        {
            if (memberId.Equals(u.memberId, StringComparison.Ordinal)
                && u.side == UnitSide.FRIENDLY
                && !u.IsDestroyed()
                && AcceptsFleetMovementOrder(u))
            {
                return u;
            }
        }

        return null;
    }

    public static string OrderWarp(
        GameState state,
        BattlefieldState bf,
        string targetBattlefieldId,
        ShipRegistry ships,
        bool allFriendly,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null,
        float? landingKm = null)
    {
        EnsureCommandSceneReady(state, bf);
        var target = TacticalWarpService.FindBattlefield(state, targetBattlefieldId);
        if (target == null || target.finished)
        {
            return "目标战场无效或已结束";
        }

        if (target.battlefieldId != null
            && target.battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
        {
            return "目标已是当前战场";
        }

        if (bf.systemId != null && target.systemId != null
            && !bf.systemId.Equals(target.systemId, StringComparison.Ordinal))
        {
            return "跨星系须使用跳桥，无法跃迁";
        }

        if (landingKm.HasValue)
        {
            state.tacticalWarpLandingDistM = TacticalWarpLandingService.ClampLandingDistM(
                TacticalRangeScale.KmToMeters(landingKm.Value));
        }

        var count = 0;
        string? lastWarpError = null;
        foreach (var u in ResolveCommandTargets(state, bf, allFriendly ? null : selectedFriendlyUnitIds))
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

            var unitLanding = landingKm.HasValue
                ? TacticalRangeScale.KmToMeters(landingKm.Value)
                : u.warpLandingDistM >= TacticalWarpLandingService.MinLandingDistM
                    ? u.warpLandingDistM
                    : TacticalWarpLandingService.ResolveLandingDistM(state);
            var err = TacticalWarpService.TryOrderWarp(
                state,
                u,
                bf,
                target,
                hull: u.hullId != null ? ships.FindHull(u.hullId) : null,
                unitLanding);
            if (err == null)
            {
                count++;
            }
            else
            {
                lastWarpError ??= err;
            }
        }

        if (count > 0)
        {
            return FormatOrderAck(count, "跃迁");
        }

        return lastWarpError ?? FormatOrderAck(0, "跃迁");
    }

    public static string OrderEnterBuilding(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null)
    {
        if (targetUnitId == null)
        {
            return FormatOrderAck(0, "进入建筑");
        }

        var gate = FindUnit(bf, targetUnitId);
        if (!JumpBridgeUnitService.IsJumpBridgeBuilding(gate))
        {
            return FormatOrderAck(0, "进入建筑");
        }

        var count = 0;
        foreach (var u in ResolveCommandTargets(state, bf, selectedFriendlyUnitIds))
        {
            if (JumpBridgeTransitService.TryTransit(state, u, bf, gate!, out _))
            {
                count++;
            }
        }

        return FormatOrderAck(count, "进入建筑");
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

            if (u.aiOrder is UnitAiOrder.STOP or UnitAiOrder.MANUAL or UnitAiOrder.RECALL)
            {
                continue;
            }

            u.aiOrder = UnitAiOrder.RALLY;
            u.rallyPointUnitId = anchor.unitId;
        }
    }
}
