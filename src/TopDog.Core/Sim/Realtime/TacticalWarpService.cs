using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.AgentDiag;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §2 战场间跃迁 · docs/VISION.md §8
 * 本文件: TacticalWarpService.cs — 同星系跃迁 + 跨星系星门
 * 【机制要点】
 * · BeginWarp：ETA = DistanceAu / warpSpeedAups；跨场景 ≤ MaxWarpDistanceAu (1000 AU)
 * · TryOrderIntraSceneWarp：同 bf、≥150 km 世界坐标冲刺，无 AU 在途
 * · RecordLandingClearance：起跳/EnterTransit 时落点阻挡扫描（预留）
 * · GateJump：JumpBridgeResolver 对端锚点瞬移
 * 【关联】FleetOrderService · JumpBridgeResolver · BattlefieldAnchorResolver
 * ══
 */


namespace TopDog.Sim.Realtime;

/// <summary>
/// 战术战场间跃迁：低速门控 → 伪跃迁飞向出口占位 → AU 在途 → 对端占位高速入场 → 减速落点。
/// </summary>
public static class TacticalWarpService
// liketocoode3a5
{
    public const float DefaultWarpSpeedAups = 5f;
    // liketocoode34e
    public const float MinWarpDistanceAu = 0.05f;
    public const float MaxWarpDistanceAu = 1000f;
    public const float PseudoWarpSpeedMps = 100_000f;
    public const float ApproachTimeoutSec = 10f;
    public const float EntryBurstSec = 10f;
    public const float LandingDecelSec = 2f;
    public const float ProxyArriveThresholdM = 120f;
    /// <summary>同场景内高速跃迁最短距离（米）。</summary>
    public const float MinIntraSceneWarpDistM = 150_000f;

    public static bool IsIntraSceneWarp(BattlefieldUnit unit) =>
        unit.warpFromBfId != null
        && unit.warpTargetBfId != null
        && unit.warpFromBfId.Equals(unit.warpTargetBfId, StringComparison.Ordinal);

    public static float ResolveWarpSpeedAups(HullDef? hull) =>
        hull is { warpSpeedAups: > 0f } ? hull.warpSpeedAups : DefaultWarpSpeedAups;

    public static float DistanceAu(BattlefieldState from, BattlefieldState to)
    {
        var a = from.anchorAu;
        var b = to.anchorAu;
        if (a.Length < 3 || b.Length < 3)
        {
            return MinWarpDistanceAu;
        }

        var dx = a[0] - b[0];
        var dy = a[1] - b[1];
        // li3etocoode345
        var dz = a[2] - b[2];
        var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return Math.Max(dist, MinWarpDistanceAu);
    }

    public static bool CanInitiateWarp(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit unit,
        HullDef? hull = null,
        float proxyX = 0f,
        float proxyY = 0f,
        float proxyZ = 0f,
        bool requireProxy = false,
        ModuleRegistry? modules = null)
    {
        if (unit.inTacticalWarp || unit.warpPhase == TacticalWarpPhase.PrepareInitiate
            || unit.pinnedToBattlefield || unit.IsDestroyed())
        {
            return false;
        }

        if (requireProxy && !TacticalWarpInitiateRules.PassesHeadingCheck(unit, proxyX, proxyY, proxyZ))
        {
            return false;
        }

        if (!TacticalWarpInitiateRules.PassesForwardSpeedCheck(unit))
        {
            return false;
        }

        return TacticalWarpInitiateRules.PassesWarpScramCheck(state, bf, unit, hull, modules);
    }

    public static string? TryBeginWarp(
        GameState state,
        // liketocoode34e
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        HullDef? hull,
        float landingDistM) =>
        TryOrderWarp(state, unit, fromBf, toBf, hull, landingDistM, autoPrepare: false);

    /// <summary>
    /// 下达跃迁：门控已满足则立即进入 ApproachProxy；艏向/航速未满足则进入 PrepareInitiate（仅调艏+开引擎）。
    /// </summary>
    public static string? TryOrderWarp(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        HullDef? hull,
        float landingDistM,
        bool autoPrepare = true)
    {
        var err = ValidateWarpRequest(state, fromBf, toBf, out var px, out var py, out var pz);
        if (err != null)
        {
            return err;
        }

        if (unit.warpPhase is TacticalWarpPhase.ApproachProxy
            or TacticalWarpPhase.EntryBurst
            or TacticalWarpPhase.LandingDecel
            or TacticalWarpPhase.InTransit)
        {
            return "已在跃迁中";
        }

        if (unit.inTacticalWarp && unit.warpPhase != TacticalWarpPhase.PrepareInitiate)
        {
            return "已在跃迁中";
        }

        if (unit.pinnedToBattlefield || unit.IsDestroyed())
        {
            return "当前无法跃迁";
        }

        var fail = TacticalWarpInitiateRules.Evaluate(state, fromBf, unit, hull, px, py, pz);
        if (fail is TacticalWarpInitiateRules.FailReason.Heading or TacticalWarpInitiateRules.FailReason.ForwardSpeed)
        {
            if (!autoPrepare)
            {
                return TacticalWarpInitiateRules.MessageFor(fail);
            }

            QueuePrepareInitiate(unit, fromBf, toBf, hull, px, py, pz, landingDistM);
            AgentSessionDebugLog.Write(
                "W2",
                "TacticalWarpService.TryOrderWarp",
                "prepare_initiate",
                new
                {
                    unitId = unit.unitId,
                    phase = unit.warpPhase.ToString(),
                    fail = fail.ToString(),
                    fromBfId = fromBf.battlefieldId,
                    toBfId = toBf.battlefieldId,
                });
            return null;
        }

        if (fail != TacticalWarpInitiateRules.FailReason.None)
        {
            return TacticalWarpInitiateRules.MessageFor(fail);
        }

        BeginWarpAfterGate(state, unit, fromBf, toBf, hull, px, py, pz, landingDistM);
        return null;
    }

    /// <summary>同场景内 &gt;150km 高速跃迁（飞向世界坐标，不经场景边缘占位）。</summary>
    public static string? TryOrderIntraSceneWarp(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState bf,
        float targetX,
        float targetY,
        float targetZ,
        HullDef? hull,
        bool autoPrepare = true)
    {
        if (bf.battlefieldId == null)
        {
            return "战场无效";
        }

        var dx = targetX - unit.x;
        var dy = targetY - unit.y;
        var dz = targetZ - unit.z;
        var distM = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distM < MinIntraSceneWarpDistM)
        {
            return "同场景跃迁需超过150km";
        }

        if (unit.warpPhase is TacticalWarpPhase.ApproachProxy
            or TacticalWarpPhase.EntryBurst
            or TacticalWarpPhase.LandingDecel
            or TacticalWarpPhase.InTransit)
        {
            return "已在跃迁中";
        }

        if (unit.inTacticalWarp && unit.warpPhase != TacticalWarpPhase.PrepareInitiate)
        {
            return "已在跃迁中";
        }

        if (unit.pinnedToBattlefield || unit.IsDestroyed())
        {
            return "当前无法跃迁";
        }

        var fail = TacticalWarpInitiateRules.Evaluate(state, bf, unit, hull, targetX, targetY, targetZ);
        if (fail is TacticalWarpInitiateRules.FailReason.Heading or TacticalWarpInitiateRules.FailReason.ForwardSpeed)
        {
            if (!autoPrepare)
            {
                return TacticalWarpInitiateRules.MessageFor(fail);
            }

            QueuePrepareInitiate(unit, bf, bf, hull, targetX, targetY, targetZ, 0f);
            AgentSessionDebugLog.Write(
                "H3",
                "TacticalWarpService.TryOrderIntraSceneWarp",
                "prepare_initiate",
                new { unitId = unit.unitId, distM, bfId = bf.battlefieldId });
            return null;
        }

        if (fail != TacticalWarpInitiateRules.FailReason.None)
        {
            return TacticalWarpInitiateRules.MessageFor(fail);
        }

        BeginWarpAfterGate(state, unit, bf, bf, hull, targetX, targetY, targetZ, 0f);
        AgentSessionDebugLog.Write(
            "H3",
            "TacticalWarpService.TryOrderIntraSceneWarp",
            "approach_target",
            new { unitId = unit.unitId, distM, targetX, targetY, targetZ });
        return null;
    }

    /// <summary>取消 PrepareInitiate（停船等指令调用）。</summary>
    public static void CancelWarpPrep(BattlefieldUnit unit)
    {
        if (unit.warpPhase != TacticalWarpPhase.PrepareInitiate)
        {
            return;
        }

        CompleteWarp(unit);
    }

    private static string? ValidateWarpRequest(
        GameState state,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        out float px,
        out float py,
        out float pz)
    {
        px = py = pz = 0f;
        if (toBf.battlefieldId == null || fromBf.battlefieldId == null)
        {
            return "目标战场无效";
        }

        if (fromBf.battlefieldId.Equals(toBf.battlefieldId, StringComparison.Ordinal))
        {
            return "目标已是当前战场";
        }

        if (fromBf.systemId == null || toBf.systemId == null
            || !fromBf.systemId.Equals(toBf.systemId, StringComparison.Ordinal))
        {
            return "跨星系须使用跳桥，无法跃迁";
        }

        if (toBf.eventRegionId == null)
        {
            return "目标场景无效";
        }

        var distanceAu = DistanceAu(fromBf, toBf);
        if (distanceAu > MaxWarpDistanceAu)
        {
            return $"目标过远（{distanceAu:0.#} AU，上限 {MaxWarpDistanceAu:0} AU）";
        }

        if (!BattlefieldSceneProxyService.TryResolveProxyPosition(
                state, fromBf, toBf.systemId, toBf.eventRegionId, out px, out py, out pz))
        {
            return "找不到出口占位";
        }

        return null;
    }

    private static void QueuePrepareInitiate(
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        HullDef? hull,
        float px,
        float py,
        float pz,
        float landingDistM)
    {
        unit.inTacticalWarp = false;
        unit.warpPhase = TacticalWarpPhase.PrepareInitiate;
        unit.warpTargetBfId = toBf.battlefieldId;
        unit.warpFromBfId = fromBf.battlefieldId;
        unit.warpEtaSec = ResolveWarpEtaSec(unit, fromBf, toBf, hull, px, py, pz);
        unit.warpPhaseTimerSec = 0f;
        unit.warpProxyX = px;
        unit.warpProxyY = py;
        unit.warpProxyZ = pz;
        unit.warpLandingDistM = landingDistM > 0f
            ? TacticalWarpLandingService.ClampLandingDistM(landingDistM)
            : 0f;
        unit.aiOrder = UnitAiOrder.WARP;
        unit.targetUnitId = null;
        unit.explicitFocus = false;
        unit.approachTargetUnitId = null;
        unit.orbitTargetUnitId = null;
    }

    private static float ResolveWarpEtaSec(
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        HullDef? hull,
        float targetX,
        float targetY,
        float targetZ)
    {
        if (fromBf.battlefieldId != null
            && toBf.battlefieldId != null
            && fromBf.battlefieldId.Equals(toBf.battlefieldId, StringComparison.Ordinal))
        {
            var dx = targetX - unit.x;
            var dy = targetY - unit.y;
            var dz = targetZ - unit.z;
            var distM = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            return Math.Max(0.05f, distM / PseudoWarpSpeedMps);
        }

        return DistanceAu(fromBf, toBf) / ResolveWarpSpeedAups(hull);
    }

    private static void BeginWarpAfterGate(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        HullDef? hull,
        float px,
        float py,
        float pz,
        float landingDistM)
    {
        unit.inTacticalWarp = true;
        unit.warpPhase = TacticalWarpPhase.ApproachProxy;
        unit.warpTargetBfId = toBf.battlefieldId;
        unit.warpFromBfId = fromBf.battlefieldId;
        unit.warpEtaSec = ResolveWarpEtaSec(unit, fromBf, toBf, hull, px, py, pz);
        unit.warpPhaseTimerSec = 0f;
        unit.warpProxyX = px;
        unit.warpProxyY = py;
        unit.warpProxyZ = pz;
        unit.warpLandingDistM = landingDistM > 0f
            ? TacticalWarpLandingService.ClampLandingDistM(landingDistM)
            : 0f;
        unit.aiOrder = UnitAiOrder.WARP;
        unit.throttleOn = false;
        unit.targetUnitId = null;
        unit.explicitFocus = false;
        unit.approachTargetUnitId = null;
        unit.orbitTargetUnitId = null;
        RecordLandingClearance(state, unit, fromBf, toBf);
        AgentSessionDebugLog.Write(
            "W2",
            "TacticalWarpService.BeginWarpAfterGate",
            "approach_proxy",
            new
            {
                unitId = unit.unitId,
                phase = unit.warpPhase.ToString(),
                fromBfId = fromBf.battlefieldId,
                toBfId = toBf.battlefieldId,
                intraScene = IsIntraSceneWarp(unit),
                obstructed = unit.warpLandingObstructed,
            });
    }

    private static void RecordLandingClearance(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf)
    {
        unit.warpLandingObstructed = false;
        unit.warpLandingObstructUnitId = null;

        if (IsIntraSceneWarp(unit))
        {
            var radiusSq = TacticalWarpLandingService.LandingObstructionRadiusM
                * TacticalWarpLandingService.LandingObstructionRadiusM;
            foreach (var blocker in toBf.units)
            {
                if (blocker.IsDestroyed()
                    || BattlefieldSceneProxyService.IsSceneProxy(blocker)
                    || ReferenceEquals(blocker, unit))
                {
                    continue;
                }

                var dx = blocker.x - unit.warpProxyX;
                var dy = blocker.y - unit.warpProxyY;
                var dz = blocker.z - unit.warpProxyZ;
                if (dx * dx + dy * dy + dz * dz <= radiusSq)
                {
                    unit.warpLandingObstructed = true;
                    unit.warpLandingObstructUnitId = blocker.unitId;
                    break;
                }
            }

            AgentSessionDebugLog.Write(
                "H4",
                "TacticalWarpService.RecordLandingClearance",
                "intra_scene",
                new
                {
                    unitId = unit.unitId,
                    obstructed = unit.warpLandingObstructed,
                    obstructId = unit.warpLandingObstructUnitId,
                });
            return;
        }

        var landingDist = TacticalWarpLandingService.ClampLandingDistM(
            unit.warpLandingDistM > 0f ? unit.warpLandingDistM : state.tacticalWarpLandingDistM);
        var obstructed = TacticalWarpLandingService.EvaluateLandingClearance(
            state,
            fromBf,
            toBf,
            landingDist,
            out _,
            out _,
            out _,
            out var obstructId);
        unit.warpLandingObstructed = obstructed;
        unit.warpLandingObstructUnitId = obstructId;
        AgentSessionDebugLog.Write(
            "H4",
            "TacticalWarpService.RecordLandingClearance",
            "cross_scene",
            new
            {
                unitId = unit.unitId,
                obstructed,
                obstructId,
                landingDistM = landingDist,
            });
    }

    public static void Tick(GameState state, BattlefieldState bf, float dtSec)
    {
        TickScenePhases(state, bf, dtSec);
    }

    public static void TickInTransit(GameState state, float dtSec)
    {
        for (var i = state.tacticalWarpInTransit.Count - 1; i >= 0; i--)
        {
            var entry = state.tacticalWarpInTransit[i];
            entry.remainingSec -= dtSec;
            if (entry.remainingSec > 0f)
            {
                continue;
            }

            var toBf = FindBattlefield(state, entry.toBattlefieldId);
            var fromBf = FindBattlefield(state, entry.fromBattlefieldId);
            if (fromBf == null || !IsResolvableWarpTarget(state, toBf))
            {
                AgentSessionDebugLog.Write(
                    "W6",
                    "TacticalWarpService.TickInTransit",
                    "target_bf_missing",
                    new
                    {
                        unitId = entry.unit.unitId,
                        toBfId = entry.toBattlefieldId,
                        skirmish = SkirmishPhaseRules.IsActiveMatch(state),
                    });
                AbortTransitEntry(state, i, entry);
                continue;
            }

            SpawnEntry(state, entry.unit, fromBf, toBf, entry.landingDistM);
            state.tacticalWarpInTransit.RemoveAt(i);
        }
    }

    private static void TickScenePhases(GameState state, BattlefieldState bf, float dtSec)
    {
        for (var i = bf.units.Count - 1; i >= 0; i--)
        {
            var u = bf.units[i];
            if (u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            switch (u.warpPhase)
            {
                case TacticalWarpPhase.PrepareInitiate:
                    TickPrepareInitiate(state, bf, u, dtSec);
                    break;
                case TacticalWarpPhase.ApproachProxy:
                    TickApproachProxy(state, bf, u, dtSec, i);
                    break;
                case TacticalWarpPhase.EntryBurst:
                    TickEntryBurst(u, dtSec);
                    break;
                case TacticalWarpPhase.LandingDecel:
                    TickLandingDecel(u, dtSec);
                    break;
            }
        }
    }

    private static void TickPrepareInitiate(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit u,
        float dtSec)
    {
        u.warpPhaseTimerSec += dtSec;
        if (u.warpTargetBfId == null)
        {
            CompleteWarp(u);
            return;
        }

        var toBf = FindBattlefield(state, u.warpTargetBfId);
        if (!IsResolvableWarpTarget(state, toBf))
        {
            AgentSessionDebugLog.Write(
                "W6",
                "TacticalWarpService.TickPrepareInitiate",
                "target_bf_missing",
                new { unitId = u.unitId, warpTargetBfId = u.warpTargetBfId });
            CompleteWarp(u);
            return;
        }

        var hull = u.hullId != null ? ShipRegistry.LoadDefault().FindHull(u.hullId) : null;
        var hullResist = TacticalWarpInitiateRules.ResolveWarpScramResist(u, hull);
        if (TacticalWarpDisruptionService.IsWarpScrambled(bf, u, hullResist))
        {
            CancelWarp(u, "跃迁被扰断");
            return;
        }

        u.throttleOn = true;
        SteerTowardProxy(u, u.warpProxyX, u.warpProxyY, u.warpProxyZ, dtSec);
        ShipMotionIntegrator.TickUnit(u, dtSec);

        var fail = TacticalWarpInitiateRules.Evaluate(
            state, bf, u, hull, u.warpProxyX, u.warpProxyY, u.warpProxyZ);
        if (fail != TacticalWarpInitiateRules.FailReason.None)
        {
            if (u.warpPhaseTimerSec >= ApproachTimeoutSec)
            {
                AgentSessionDebugLog.Write(
                    "W5",
                    "TacticalWarpService.TickPrepareInitiate",
                    "timeout_force_warp",
                    new
                    {
                        unitId = u.unitId,
                        fail = fail.ToString(),
                        timer = u.warpPhaseTimerSec,
                        bfId = bf.battlefieldId,
                    });
                BeginWarpAfterGate(
                    state,
                    u,
                    bf,
                    toBf,
                    hull,
                    u.warpProxyX,
                    u.warpProxyY,
                    u.warpProxyZ,
                    u.warpLandingDistM);
                return;
            }

            return;
        }

        BeginWarpAfterGate(
            state,
            u,
            bf,
            toBf,
            hull,
            u.warpProxyX,
            u.warpProxyY,
            u.warpProxyZ,
            u.warpLandingDistM);
    }

    private static void SteerTowardProxy(BattlefieldUnit u, float tx, float ty, float tz, float dtSec)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        var yaw = MathF.Atan2(dy, dx);
        var horiz = MathF.Sqrt(dx * dx + dy * dy);
        var pitch = horiz > 0.01f ? MathF.Atan2(dz, horiz) : 0f;
        ShipMotionIntegrator.SteerToward(u, yaw, pitch, dtSec);
    }

    private static void TickApproachProxy(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit u,
        float dtSec,
        int unitIndex)
    {
        u.warpPhaseTimerSec += dtSec;
        var hullResist = TacticalWarpInitiateRules.ResolveWarpScramResist(
            u, u.hullId != null ? Content.Ships.ShipRegistry.LoadDefault().FindHull(u.hullId) : null);
        if (TacticalWarpDisruptionService.IsWarpScrambled(bf, u, hullResist))
        {
            CancelWarp(u, "跃迁被扰断");
            return;
        }

        var arrived = MoveToward(
            u,
            u.warpProxyX,
            u.warpProxyY,
            u.warpProxyZ,
            PseudoWarpSpeedMps,
            dtSec,
            out _);

        if (arrived
            || u.warpPhaseTimerSec >= ApproachTimeoutSec
            || DistToProxy(u) <= ProxyArriveThresholdM)
        {
            if (IsIntraSceneWarp(u))
            {
                u.x = u.warpProxyX;
                u.y = u.warpProxyY;
                u.z = u.warpProxyZ;
                u.vx = u.vy = u.vz = 0f;
                CompleteWarp(u);
                AgentSessionDebugLog.Write(
                    "H3",
                    "TacticalWarpService.TickApproachProxy",
                    "intra_scene_arrived",
                    new { unitId = u.unitId, bfId = bf.battlefieldId });
                return;
            }

            EnterTransit(state, bf, u, unitIndex);
        }
    }

    private static void EnterTransit(GameState state, BattlefieldState bf, BattlefieldUnit u, int unitIndex)
    {
        var landing = TacticalWarpLandingService.ClampLandingDistM(
            u.warpLandingDistM > 0f ? u.warpLandingDistM : state.tacticalWarpLandingDistM);

        var toBf = FindBattlefield(state, u.warpTargetBfId);
        var fromBf = FindBattlefield(state, u.warpFromBfId ?? bf.battlefieldId);
        if (fromBf != null && toBf != null)
        {
            RecordLandingClearance(state, u, fromBf, toBf);
        }

        state.tacticalWarpInTransit.Add(new TacticalWarpTransitEntry
        {
            unit = u,
            fromBattlefieldId = u.warpFromBfId ?? bf.battlefieldId,
            toBattlefieldId = u.warpTargetBfId,
            remainingSec = Math.Max(0.05f, u.warpEtaSec),
            landingDistM = landing,
        });

        bf.units.RemoveAt(unitIndex);
        u.warpPhase = TacticalWarpPhase.InTransit;
        u.vx = u.vy = u.vz = 0f;
        u.throttleOn = false;
        AgentSessionDebugLog.Write(
            "W3",
            "TacticalWarpService.EnterTransit",
            "removed_from_source",
            new
            {
                unitId = u.unitId,
                fromBfId = bf.battlefieldId,
                toBfId = u.warpTargetBfId,
                etaSec = u.warpEtaSec,
                transitCount = state.tacticalWarpInTransit.Count,
            });
    }

    private static void SpawnEntry(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        float landingDistM)
    {
        if (fromBf.systemId != null && toBf.systemId != null
            && !fromBf.systemId.Equals(toBf.systemId, StringComparison.Ordinal))
        {
            CompleteWarp(unit);
            return;
        }

        if (fromBf.eventRegionId == null
            || !BattlefieldSceneProxyService.TryResolveProxyPosition(
                state, toBf, fromBf.systemId!, fromBf.eventRegionId, out var ex, out var ey, out var ez))
        {
            ex = unit.side == UnitSide.FRIENDLY ? -50_000f : 50_000f;
            ey = 0f;
            ez = 0f;
        }

        BattlefieldSceneOriginService.Resolve(state, toBf, out var ox, out var oy, out var oz);
        TacticalWarpLandingService.ComputeLandingPoint(
            ox,
            oy,
            oz,
            ex,
            ey,
            ez,
            landingDistM,
            out var lx,
            out var ly,
            out var lz);

        var burstDistM = TacticalWarpEtaEstimator.EstimateBurstToLandingDistM(ox, oy, oz, ex, ey, ez, landingDistM);
        var estLandingSec = TacticalWarpEtaEstimator.EstimatePostEntryLandingSec(burstDistM);

        unit.x = ex;
        unit.y = ey;
        unit.z = ez;
        unit.vx = unit.vy = unit.vz = 0f;
        unit.warpLandingX = lx;
        unit.warpLandingY = ly;
        unit.warpLandingZ = lz;
        unit.warpLandingDistM = landingDistM;
        unit.warpPhase = TacticalWarpPhase.EntryBurst;
        unit.warpPhaseTimerSec = 0f;
        unit.inTacticalWarp = true;
        unit.warpFromBfId = fromBf.battlefieldId;
        unit.warpTargetBfId = toBf.battlefieldId;
        unit.aiOrder = UnitAiOrder.WARP;
        unit.throttleOn = false;
        toBf.units.Add(unit);
        AgentSessionDebugLog.Write(
            "W5",
            "TacticalWarpService.SpawnEntry",
            "entry_burst",
            new
            {
                unitId = unit.unitId,
                toBfId = toBf.battlefieldId,
                phase = unit.warpPhase.ToString(),
                landingDistM,
                burstDistM,
                estLandingSec,
                entryX = ex,
                entryY = ey,
                entryZ = ez,
                landingX = lx,
                landingY = ly,
                landingZ = lz,
            });
    }

    private static void TickEntryBurst(BattlefieldUnit u, float dtSec)
    {
        u.warpPhaseTimerSec += dtSec;
        var arrived = MoveToward(
            u,
            u.warpLandingX,
            u.warpLandingY,
            u.warpLandingZ,
            PseudoWarpSpeedMps,
            dtSec,
            out var remaining);

        if (u.warpPhaseTimerSec >= EntryBurstSec)
        {
            if (remaining > ProxyArriveThresholdM)
            {
                // #region agent log
                AgentSessionDebugLog.Write(
                    "W5",
                    "TacticalWarpService.TickEntryBurst",
                    "burst_timeout",
                    new
                    {
                        unitId = u.unitId,
                        timerSec = u.warpPhaseTimerSec,
                        remainingM = remaining,
                        landingDistM = u.warpLandingDistM,
                    });
                // #endregion
            }

            u.warpPhase = TacticalWarpPhase.LandingDecel;
            u.warpPhaseTimerSec = 0f;
            return;
        }

        if (arrived)
        {
            u.warpPhase = TacticalWarpPhase.LandingDecel;
            u.warpPhaseTimerSec = 0f;
        }
    }

    private static void TickLandingDecel(BattlefieldUnit u, float dtSec)
    {
        u.warpPhaseTimerSec += dtSec;
        var t = Math.Clamp(u.warpPhaseTimerSec / LandingDecelSec, 0f, 1f);
        var speed = PseudoWarpSpeedMps * (1f - t);
        var arrived = MoveToward(u, u.warpLandingX, u.warpLandingY, u.warpLandingZ, speed, dtSec, out var dist);

        if (t >= 1f || (arrived && dist <= ProxyArriveThresholdM))
        {
            if (dist > ProxyArriveThresholdM)
            {
                // #region agent log
                AgentSessionDebugLog.Write(
                    "W5",
                    "TacticalWarpService.TickLandingDecel",
                    "snap_landing",
                    new
                    {
                        unitId = u.unitId,
                        distM = dist,
                        timerSec = u.warpPhaseTimerSec,
                        landingDistM = u.warpLandingDistM,
                    });
                // #endregion
            }

            u.x = u.warpLandingX;
            u.y = u.warpLandingY;
            u.z = u.warpLandingZ;
            u.vx = 0f;
            u.vy = 0f;
            u.vz = 0f;
            CompleteWarp(u);
            // #region agent log
            AgentSessionDebugLog.Write(
                "W5",
                "TacticalWarpService.TickLandingDecel",
                "landed",
                new { unitId = u.unitId, landingDistM = u.warpLandingDistM });
            // #endregion
        }
    }

    private static void CompleteWarp(BattlefieldUnit unit)
    {
        unit.inTacticalWarp = false;
        unit.warpPhase = TacticalWarpPhase.None;
        unit.warpTargetBfId = null;
        unit.warpFromBfId = null;
        unit.warpEtaSec = 0f;
        unit.warpPhaseTimerSec = 0f;
        unit.warpProxyX = unit.warpProxyY = unit.warpProxyZ = 0f;
        unit.warpLandingX = unit.warpLandingY = unit.warpLandingZ = 0f;
        unit.aiOrder = UnitAiOrder.IDLE;
        unit.throttleOn = false;
    }

    private static void CancelWarp(BattlefieldUnit unit, string? reason = null)
    {
        CompleteWarp(unit);
        if (reason != null && unit.displayName != null && !unit.displayName.Contains(reason, StringComparison.Ordinal))
        {
            // 保留 displayName；播报由 FleetOrderService 汇总
        }
    }

    private static void AbortTransitEntry(GameState state, int index, TacticalWarpTransitEntry entry)
    {
        entry.unit.inTacticalWarp = false;
        entry.unit.warpPhase = TacticalWarpPhase.None;
        entry.unit.alive = true;
        state.tacticalWarpInTransit.RemoveAt(index);
    }

    private static bool MoveToward(
        BattlefieldUnit u,
        float tx,
        float ty,
        float tz,
        float speedMps,
        float dtSec,
        out float remainingDist)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        remainingDist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (remainingDist <= ProxyArriveThresholdM)
        {
            u.x = tx;
            u.y = ty;
            u.z = tz;
            u.vx = 0f;
            u.vy = 0f;
            u.vz = 0f;
            if (remainingDist > 0.01f)
            {
                u.facingRad = MathF.Atan2(dy, dx);
            }
            return true;
        }

        var step = Math.Max(speedMps * dtSec, 0f);
        if (step >= remainingDist)
        {
            u.x = tx;
            u.y = ty;
            u.z = tz;
            u.vx = 0f;
            u.vy = 0f;
            u.vz = 0f;
            remainingDist = 0f;
            u.facingRad = MathF.Atan2(dy, dx);
            return true;
        }

        var inv = 1f / remainingDist;
        u.x += dx * inv * step;
        u.y += dy * inv * step;
        u.z += dz * inv * step;
        u.vx = dx * inv * speedMps;
        u.vy = dy * inv * speedMps;
        u.vz = dz * inv * speedMps;
        u.facingRad = MathF.Atan2(dy, dx);
        remainingDist -= step;
        return false;
    }

    private static float DistToProxy(BattlefieldUnit u)
    {
        var dx = u.warpProxyX - u.x;
        var dy = u.warpProxyY - u.y;
        var dz = u.warpProxyZ - u.z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static BattlefieldState? FindBattlefield(GameState state, string? battlefieldId)
    {
        if (battlefieldId == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }

    internal static bool IsResolvableWarpTarget(GameState state, BattlefieldState? toBf) =>
        toBf != null
        && toBf.battlefieldId != null
        && (!toBf.finished || SkirmishPhaseRules.IsActiveMatch(state));

    /// <summary>供拦截泡等后续机制改写在途/入场落点。</summary>
    public static void OverrideTransitLandingDist(GameState state, string unitId, float landingDistM)
    {
        var dist = TacticalWarpLandingService.ClampLandingDistM(landingDistM);
        foreach (var entry in state.tacticalWarpInTransit)
        {
            if (unitId.Equals(entry.unit.unitId, StringComparison.Ordinal))
            {
                entry.landingDistM = dist;
                entry.unit.warpLandingDistM = dist;
            }
        }

        foreach (var bf in state.battlefields)
        {
            foreach (var u in bf.units)
            {
                if (unitId.Equals(u.unitId, StringComparison.Ordinal)
                    && u.warpPhase is TacticalWarpPhase.EntryBurst or TacticalWarpPhase.LandingDecel)
                {
                    u.warpLandingDistM = dist;
                    var fromBf = FindBattlefield(state, u.warpFromBfId);
                    if (fromBf?.systemId != null && fromBf.eventRegionId != null
                        && BattlefieldSceneProxyService.TryResolveProxyPosition(
                            state, bf, fromBf.systemId, fromBf.eventRegionId, out var ex, out var ey, out var ez))
                    {
                        BattlefieldSceneOriginService.Resolve(state, bf, out var ox, out var oy, out var oz);
                        TacticalWarpLandingService.ComputeLandingPoint(
                            ox, oy, oz, ex, ey, ez, dist, out u.warpLandingX, out u.warpLandingY, out u.warpLandingZ);
                    }
                }
            }
        }
    }
}
