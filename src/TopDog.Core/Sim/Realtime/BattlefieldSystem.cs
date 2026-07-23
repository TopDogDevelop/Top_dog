using TopDog.AgentDiag;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
using TopDog.Sim.Vision;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §3 · docs/TACTICAL_VIEW.md §0-§2
 * 本文件: BattlefieldSystem.cs — 实时战场 tick（运动·自火·salvo·胜负）
 * 【机制要点】
 * · Tick：逐 battlefield 积分 timeSec，调用 TickBattlefield + 胜负判定
 * · ApplyAiMovement：MANUAL/RETREAT/STOP/SCATTER/FOLLOW/APPROACH/AWAY/ORBIT/RALLY
 * · TickApproachOrAway：每 1s SnapHeadingToward/Away + 满引擎；可选 commandMaintainDistM 维持距
 * · TryFireSalvo：射程内开火；§4b TryConfirmSalvoTarget（失败不进 CD）；§4c 显式集火领顺序槽
 * · CheckVictory：友敌存活 + 300s timeout
 * 【实现逻辑】
 * · TickBattlefield 不调用 BattlefieldSceneProxyService（占位已密封，见 TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §3.3）
 * 【关联】ShipMotionIntegrator · FleetOrderService · FocusFireSequencer · SpecializedSalvoService · BattleReportService
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public static class BattlefieldSystem
// liketocoode3a5
{
    public const float BattleTimeoutSec = 300f;

    // liketoc0de345

    public static void Tick(GameState state, float dtSec) =>
        // liketocoode34e
        Tick(state, ModuleRegistry.LoadDefault(), ShipRegistry.LoadDefault(), dtSec);

    public static void Tick(GameState state, ModuleRegistry modules, ShipRegistry ships, float dtSec)
    {
        if (!state.combatRealtimeActive || state.battlefields.Count == 0)
        {
            return;
        }

        CombatTelemetrySessionExport.EnsureActive(state);

        HealMisfinishedShipBattlefields(state);

        TacticalWarpService.TickInTransit(state, dtSec);
        RallyNavigationService.Tick(state, ships, dtSec);

        foreach (var bf in state.battlefields)
        {
            if (bf.finished && !ShouldTickBattlefield(state, bf))
            {
                continue;
            }

            bf.timeSec += dtSec * Math.Max(0.01f, bf.timeDilation);
            var tickBegin = SceneSimBudget.Begin();
            TickBattlefield(
                state, bf, modules, ships, dtSec * Math.Max(0.01f, bf.timeDilation), tickBegin);
            SceneSimBudget.EndAndApply(bf, tickBegin);
            CombatTelemetryLog.MaybeLogPositions(bf, bf.timeSec);
            if (!bf.finished && !bf.disableAutoVictory)
            {
                if (bf.combatSubtype == CombatSubtype.BUILDING_ASSAULT)
                {
                    // BuildingCombatRules.TickBuildingWin already called in TickBattlefield
                }
                else if (HarvestCombatRules.IsHarvestBattlefield(bf))
                {
                    HarvestCombatRules.TickHarvestWin(state, bf);
                }

                if (!bf.finished)
                {
                    CheckVictory(bf);
                }
            }
        }

        state.battlefields.RemoveAll(bf => ShouldPruneBattlefield(state, bf));

        TacticalViewportFollowService.Tick(state);
    }

    // li3etocoode345

    private static void TickBattlefield(
        GameState state,
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec,
        float tickBegin)
    {
        // 附身/指令输入优先：即使本 tick 后面超时也要应用
        PossessionInputService.ApplyPending(state, bf, dtSec);
        var dense = BattlefieldScalePolicy.IsDense(bf);
        bf.spatialHash ??= new BattlefieldSpatialHash();
        bf.spatialHashTickCounter++;
        if (!dense || bf.spatialHashTickCounter % 3 == 1)
        {
            bf.spatialHash.Rebuild(bf.units, cellSize: 10_000f);
        }
        if (bf.timeSec >= bf.runtimeEffectNextTickSec)
        {
            bf.runtimeEffectNextTickSec = bf.timeSec + 1f;
            AreaModuleRuntimeService.TickOneHz(bf, modules);
            InterdictionFieldService.TickOneHz(bf, modules);
            DynamicModuleQuotaService.Tick(bf, ships, modules);
        }

        TacticalWarpService.Tick(state, bf, dtSec);
        if (!SceneSimBudget.IsOverBudget(bf, tickBegin))
        {
            AiRealtimePlayerBrain.Tick(state, bf, modules, ships, dtSec);
        }

        // 密舰队：跳过场域/登舰/后勤自动瞄准等全表服务（FLEET_SCALE_10K §5，按规模门控、非场景 ID）
        if (!SceneSimBudget.IsOverBudget(bf, tickBegin))
        {
            MissileProjectileService.Tick(state, bf, modules, ships, dtSec);
            StrikeWingRecallService.Tick(bf, modules, new Random((int)bf.timeSec ^ bf.units.Count));
            if (!dense)
            {
                BoardingModuleService.Tick(state, bf, modules, ships, dtSec);
                FieldAuraService.Tick(state, bf, modules, ships, dtSec);
                LogisticsProducerService.Tick(bf, modules, dtSec);
                RemoteRepairAutoTargetingService.TickAll(bf, modules);
                RemoteRepairSalvoService.Tick(state, bf, modules, ships, dtSec);
            }
        }

        CombatMarkService.Tick(bf);
        TickPassiveShieldRegen(bf, dtSec);

        var building = BuildingService.Find(state, bf.targetBuildingId);
        if (bf.targetBuildingId != null)
        {
            BuildingCombatRules.TickBuildingWin(bf, building);
        }

        var possessed = FindPossessedUnit(state, bf);
        var n = bf.units.Count;
        if (n == 0)
        {
            return;
        }

        // 密舰队：约 75% 单位/ tick 轮转；稀疏全量 —— 同一代码路径；墙钟超预算则提前结束留给 UI
        var processBudget = BattlefieldScalePolicy.ResolveUnitProcessBudget(bf);
        var start = dense ? bf.unitProcessLodCursor % n : 0;
        var processed = 0;
        for (var step = 0; step < n && processed < processBudget; step++)
        {
            if (SceneSimBudget.IsOverBudget(bf, tickBegin))
            {
                break;
            }

            var idx = (start + step) % n;
            var u = bf.units[idx];
            if (u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.inTacticalWarp
                || u.warpPhase == TacticalWarpPhase.PrepareInitiate
                || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            processed++;
            if (!u.IsBallisticMissile())
            {
                LogisticsAutoTargetingService.Tick(bf, u, modules);
                ApplyAiMovement(state, bf, u, possessed, dtSec);
                DamageMitigationService.Tick(u, modules, dtSec);
                DamageMitigationService.TickBlockLock(u, dtSec);
            }

            if (u.aiOrder != UnitAiOrder.MANUAL)
            {
                AutoFireTargetingService.Tick(bf, state, u);
            }

            TryShieldRepairSalvo(bf, u, dtSec);
            TryArmorRepairSalvo(bf, u, dtSec);
            MissileLaunchService.TryLaunch(state, bf, u, modules, new Random((int)(bf.timeSec * 1000) ^ u.unitId?.GetHashCode() ?? 0), dtSec);
            TryFireSalvo(bf, state, building, u, dtSec, ships, modules);
            SpecializedSalvoService.Tick(state, bf, u, dtSec, ships, modules);
        }

        if (dense)
        {
            bf.unitProcessLodCursor = (start + Math.Max(processed, 1)) % Math.Max(n, 1);
        }
    }

    // liketocoode3a5

    private static readonly Dictionary<string, float> PassiveShieldRegenNextSec = new(StringComparer.Ordinal);

    private static void TickPassiveShieldRegen(BattlefieldState bf, float dtSec)
    {
        if (bf.battlefieldId == null)
        {
            return;
        }

        if (!PassiveShieldRegenNextSec.TryGetValue(bf.battlefieldId, out var next))
        {
            next = bf.timeSec;
        }

        if (bf.timeSec + dtSec < next)
        {
            return;
        }

        PassiveShieldRegenNextSec[bf.battlefieldId] = bf.timeSec + 10f;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding || !string.IsNullOrEmpty(u.shieldFieldHostUnitId))
            {
                continue;
            }

            if (u.shieldMax <= 0f)
            {
                continue;
            }

            var before = u.shieldHp;
            u.shieldHp = Math.Min(u.shieldMax, u.shieldHp + u.shieldMax * 0.01f);
            var delta = u.shieldHp - before;
            if (delta > 0f)
            {
                QueueHpDelta(bf, u, delta, 0f, 0f, isHeal: true);
            }
        }
    }

    private static void TryShieldRepairSalvo(BattlefieldState bf, BattlefieldUnit u, float dtSec)
    {
        if (u.isBuilding || u.shieldSalvoRepair <= 0f || u.shieldHp >= u.shieldMax)
        {
            return;
        }

        u.shieldRepairCooldownSec -= dtSec;
        if (u.shieldRepairCooldownSec > 0f)
        {
            return;
        }

        var before = u.shieldHp;
        u.shieldHp = Math.Min(u.shieldMax, u.shieldHp + u.shieldSalvoRepair);
        var delta = u.shieldHp - before;
        if (delta > 0f)
        {
            QueueHpDelta(bf, u, delta, 0f, 0f, isHeal: true);
        }
        u.shieldRepairCooldownSec = u.shieldRepairCycleSec;
    }

    private static void TryArmorRepairSalvo(BattlefieldState bf, BattlefieldUnit u, float dtSec)
    {
        if (u.isBuilding || u.armorSalvoRepair <= 0f || u.armorHp >= u.armorMax)
        {
            return;
        }

        u.armorRepairCooldownSec -= dtSec;
        if (u.armorRepairCooldownSec > 0f)
        {
            return;
        }

        var before = u.armorHp;
        u.armorHp = Math.Min(u.armorMax, u.armorHp + u.armorSalvoRepair);
        var delta = u.armorHp - before;
        if (delta > 0f)
        {
            QueueHpDelta(bf, u, 0f, delta, 0f, isHeal: true);
            // #region agent log
            try
            {
                var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                var line = "{\"sessionId\":\"85a1e0\",\"runId\":\"post-fix\",\"hypothesisId\":\"L\",\"location\":\"BattlefieldSystem.TryArmorRepairSalvo\",\"message\":\"armor-regen\",\"data\":{"
                           + "\"unit\":\"" + (u.unitId ?? "") + "\""
                           + ",\"delta\":" + delta.ToString("F1")
                           + ",\"perSalvo\":" + u.armorSalvoRepair.ToString("F1")
                           + ",\"cycle\":" + u.armorRepairCycleSec.ToString("F1")
                           + ",\"armorAfter\":" + u.armorHp.ToString("F1")
                           + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                System.IO.File.AppendAllText(path, line);
            }
            catch
            {
            }
            // #endregion
        }

        u.armorRepairCooldownSec = u.armorRepairCycleSec;
    }

    // liketocoode34e

    private static void ApplyAiMovement(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit u,
        BattlefieldUnit? possessed,
        float dtSec)
    {
        if (u.aiOrder == UnitAiOrder.MANUAL)
        {
            return;
        }

        if (u.aiOrder == UnitAiOrder.RETREAT)
        {
            SteerTowardPoint(u, u.x * 2f, u.y * 2f, u.z * 2f, dtSec);
            u.throttleOn = true;
            ShipMotionIntegrator.TickUnit(u, dtSec);
            return;
        }

        if (u.aiOrder == UnitAiOrder.STOP)
        {
            u.throttleOn = false;
            ShipMotionIntegrator.TickUnit(u, dtSec);
            return;
        }

        if (StrikeWingOrderService.TryTickRecall(bf, u, dtSec))
        {
            return;
        }

        if (u.aiOrder == UnitAiOrder.SCATTER)
        {
            u.throttleOn = true;
            ShipMotionIntegrator.TickUnit(u, dtSec);
            return;
        }

        if (u.aiOrder == UnitAiOrder.FOLLOW_ATTACK && possessed != null && !ReferenceEquals(u, possessed))
        {
            SteerTowardPoint(u, possessed.x, possessed.y, possessed.z, dtSec);
            u.throttleOn = true;
            ShipMotionIntegrator.TickUnit(u, dtSec);
            return;
        }

        if (u.aiOrder == UnitAiOrder.FOLLOW)
        {
            var follow = possessed;
            if (follow == null && u.rallyPointUnitId != null)
            {
                follow = FindUnit(bf, u.rallyPointUnitId);
            }

            if (follow != null && !ReferenceEquals(u, follow))
            {
                SteerTowardPoint(u, follow.x, follow.y, follow.z, dtSec);
                u.throttleOn = true;
                ShipMotionIntegrator.TickUnit(u, dtSec);
                return;
            }
        }

        if (u.aiOrder == UnitAiOrder.APPROACH && u.approachTargetUnitId != null)
        {
            TickApproachOrAway(bf, u, dtSec, away: false);
            return;
        }

        if (u.aiOrder == UnitAiOrder.AWAY && u.approachTargetUnitId != null)
        {
            TickApproachOrAway(bf, u, dtSec, away: true);
            return;
        }

        if (u.aiOrder == UnitAiOrder.NAVIGATE)
        {
            if (ShouldDecoupleNavigationFromFireHeading(state, bf, u))
            {
                MoveTowardPointWithoutFacing(u, u.navigateX, u.navigateY, u.navigateZ, dtSec);
            }
            else
            {
                SteerTowardPoint(u, u.navigateX, u.navigateY, u.navigateZ, dtSec);
            }

            u.throttleOn = true;
            ShipMotionIntegrator.TickUnit(u, dtSec);
            return;
        }

        if (u.aiOrder == UnitAiOrder.ORBIT && u.orbitTargetUnitId != null)
        {
            var orbit = FindUnit(bf, u.orbitTargetUnitId);
            if (orbit != null)
            {
                TickOrbit(bf, u, orbit, dtSec);
                ShipMotionIntegrator.TickUnit(u, dtSec);
                return;
            }

            u.aiOrder = UnitAiOrder.IDLE;
            u.orbitTargetUnitId = null;
            u.throttleOn = false;
        }

        if (u.aiOrder == UnitAiOrder.RALLY)
        {
            var anchor = possessed ?? FindUnit(bf, u.rallyPointUnitId);
            if (anchor != null)
            {
                SteerTowardPoint(u, anchor.x, anchor.y, anchor.z, dtSec);
                u.throttleOn = true;
            }
            else
            {
                SteerTowardPoint(u, 0f, 0f, 0f, dtSec);
                u.throttleOn = true;
            }
        }

        if (StrikeWingOrderService.IsDroneWing(u) && u.aiOrder == UnitAiOrder.IDLE)
        {
            u.throttleOn = false;
        }

        ShipMotionIntegrator.TickUnit(u, dtSec);
    }

    // liketocoo3e345

    private const float MaintainDeadbandM = 200f;

    private static void TickApproachOrAway(BattlefieldState bf, BattlefieldUnit u, float dtSec, bool away)
    {
        var target = FindUnit(bf, u.approachTargetUnitId!);
        if (target == null)
        {
            u.approachTargetUnitId = null;
            u.approachHeadingTimerSec = 0f;
            return;
        }

        u.throttleOn = true;
        u.approachHeadingTimerSec -= dtSec;
        if (u.approachHeadingTimerSec <= 0f)
        {
            var maintain = u.commandMaintainDistM;
            if (maintain > 0f)
            {
                var dx = target.x - u.x;
                var dy = target.y - u.y;
                var dz = target.z - u.z;
                var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                var err = dist - maintain;
                if (MathF.Abs(err) <= MaintainDeadbandM)
                {
                    // 死区：保持当前艏向并关油门，停在默认距离附近
                    u.throttleOn = false;
                }
                else if (away)
                {
                    u.throttleOn = true;
                    if (err < -MaintainDeadbandM)
                    {
                        ShipMotionIntegrator.SnapHeadingAway(u, target.x, target.y, target.z);
                    }
                    else
                    {
                        ShipMotionIntegrator.SnapHeadingToward(u, target.x, target.y, target.z);
                    }
                }
                else if (err > MaintainDeadbandM)
                {
                    u.throttleOn = true;
                    ShipMotionIntegrator.SnapHeadingToward(u, target.x, target.y, target.z);
                }
                else
                {
                    u.throttleOn = true;
                    ShipMotionIntegrator.SnapHeadingAway(u, target.x, target.y, target.z);
                }
            }
            else if (away)
            {
                ShipMotionIntegrator.SnapHeadingAway(u, target.x, target.y, target.z);
            }
            else
            {
                ShipMotionIntegrator.SnapHeadingToward(u, target.x, target.y, target.z);
            }

            u.approachHeadingTimerSec = ShipMotionIntegrator.ApproachHeadingIntervalSec;
        }
        else if (u.commandMaintainDistM > 0f)
        {
            // 两次对准之间也维持死区关油门，避免冲过默认距离
            var dx = target.x - u.x;
            var dy = target.y - u.y;
            var dz = target.z - u.z;
            var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (MathF.Abs(dist - u.commandMaintainDistM) <= MaintainDeadbandM)
            {
                u.throttleOn = false;
            }
        }

        ShipMotionIntegrator.TickUnit(u, dtSec);
    }

    private static void TickOrbit(BattlefieldState bf, BattlefieldUnit u, BattlefieldUnit target, float dtSec)
    {
        var radius = OrbitEntryResolver.ResolveOrbitRadiusM(u);
        if (u.orbitPhase < OrbitEntryResolver.OrbitPhaseRing)
        {
            OrbitEntryResolver.ComputeEntryPoint(u, target, radius, out var ex, out var ey, out var ez);
            u.orbitEntryX = ex;
            u.orbitEntryY = ey;
            u.orbitEntryZ = ez;
            var dx = ex - u.x;
            var dy = ey - u.y;
            var dz = ez - u.z;
            var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (dist <= OrbitEntryResolver.EntryArriveThresholdM)
            {
                u.orbitPhase = OrbitEntryResolver.OrbitPhaseRing;
            }
            else
            {
                u.throttleOn = true;
                u.approachHeadingTimerSec -= dtSec;
                if (u.approachHeadingTimerSec <= 0f)
                {
                    ShipMotionIntegrator.SnapHeadingToward(u, ex, ey, ez);
                    u.approachHeadingTimerSec = ShipMotionIntegrator.ApproachHeadingIntervalSec;
                }

                return;
            }
        }

        OrbitOnRing(u, target, radius, dtSec);
    }

    private static void OrbitOnRing(BattlefieldUnit u, BattlefieldUnit target, float radius, float dtSec)
    {
        var dx = u.x - target.x;
        var dy = u.y - target.y;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy);
        if (dist < 0.01f)
        {
            dist = 0.01f;
        }

        var tangentYaw = (float)Math.Atan2(dy, dx) + (float)(Math.PI / 2);
        if (dist > radius * 1.1f)
        {
            tangentYaw = (float)Math.Atan2(target.y - u.y, target.x - u.x);
            u.throttleOn = true;
        }
        else if (dist < radius * 0.75f)
        {
            tangentYaw = (float)Math.Atan2(u.y - target.y, u.x - target.x);
            u.throttleOn = true;
        }
        else
        {
            u.throttleOn = true;
        }

        ShipMotionIntegrator.SteerToward(u, tangentYaw, 0f, dtSec);
    }

    // lik3tocoode345

    internal static void SteerTowardPoint(BattlefieldUnit u, float tx, float ty, float tz, float dtSec)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        var yaw = (float)Math.Atan2(dy, dx);
        var horiz = (float)Math.Sqrt(dx * dx + dy * dy);
        var pitch = horiz > 0.01f ? (float)Math.Atan2(dz, horiz) : 0f;
        ShipMotionIntegrator.SteerToward(u, yaw, pitch, dtSec);
    }

    /// <summary>走位时不改艏向，仅沿导航方向加速（保留炮塔对准开火）。</summary>
    internal static void MoveTowardPointWithoutFacing(BattlefieldUnit u, float tx, float ty, float tz, float dtSec)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (dist < 50f)
        {
            u.throttleOn = false;
            return;
        }

        var inv = 1f / dist;
        var ax = dx * inv * u.accelMps2;
        var ay = dy * inv * u.accelMps2;
        var az = dz * inv * u.accelMps2;
        u.vx += ax * dtSec;
        u.vy += ay * dtSec;
        u.vz += az * dtSec;
        var speed = u.SpeedMps();
        if (speed > u.maxSpeedMps && speed > 0.0001f)
        {
            var scale = u.maxSpeedMps / speed;
            u.vx *= scale;
            u.vy *= scale;
            u.vz *= scale;
        }
    }

    private static bool ShouldDecoupleNavigationFromFireHeading(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit u)
    {
        if (u.salvoRoundDmg <= 0f || u.isBuilding)
        {
            return false;
        }

        if (TryGetEngagementTarget(bf, u, out var target) && IsWithinWeaponRange(u, target!))
        {
            return true;
        }

        if (!state.autoFireEnabled)
        {
            return false;
        }

        var nearestId = AutoFireTargetingService.FindNearestEnemyId(bf, u);
        if (nearestId == null)
        {
            return false;
        }

        var nearest = FindUnit(bf, nearestId);
        return nearest != null && IsWithinWeaponRange(u, nearest);
    }

    private static bool TryGetEngagementTarget(BattlefieldState bf, BattlefieldUnit u, out BattlefieldUnit? target)
    {
        target = null;
        if (u.targetUnitId == null)
        {
            return false;
        }

        target = FindUnit(bf, u.targetUnitId);
        return target != null
            && !target.IsDestroyed()
            && target.Arrived(bf.timeSec)
            && !BattlefieldSceneProxyService.IsSceneProxy(target);
    }

    private static bool IsWithinWeaponRange(BattlefieldUnit u, BattlefieldUnit target)
    {
        var dx = target.x - u.x;
        var dy = target.y - u.y;
        var dz = target.z - u.z;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return dist <= u.attackRangeM;
    }

    // liketocoode3e5

    /// <summary>开火确认（TACTICAL_WARP §4b）：结算前目标仍存活且敌对，否则本次开火失败。</summary>
    public static bool TryConfirmSalvoTarget(BattlefieldUnit firer, BattlefieldUnit? target)
    {
        if (target == null || target.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(target)
            || !CombatHostility.AreHostile(firer, target))
        {
            return false;
        }

        return true;
    }

    private static void TryFireSalvo(
        BattlefieldState bf,
        GameState state,
        BuildingState? building,
        BattlefieldUnit u,
        float dtSec,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        if (u.isBuilding || u.salvoRoundDmg <= 0f || u.IsBallisticMissile())
        {
            return;
        }

        var target = u.targetUnitId != null ? FindUnit(bf, u.targetUnitId) : null;
        if (!TryConfirmSalvoTarget(u, target))
        {
            return;
        }

        u.fireCooldownSec -= dtSec;
        var dx = target!.x - u.x;
        var dy = target.y - u.y;
        var dz = target.z - u.z;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (dist > u.attackRangeM)
        {
            return;
        }

        // TiDi 下用墙钟补偿转向，避免 dil=0.1 时转炮永不进射界 → salvo=0
        var slewDt = dtSec;
        if (bf.timeDilation > 0.01f && bf.timeDilation < 0.999f)
        {
            slewDt = MathF.Min(0.05f, dtSec / bf.timeDilation);
        }

        if (!TrySlewWeaponTowardTarget(u, dx, dy, dz, slewDt))
        {
            return;
        }

        if (u.fireCooldownSec > 0f)
        {
            return;
        }

        // §4b 结算前再次确认；失败不进 CD
        if (!TryConfirmSalvoTarget(u, target))
        {
            return;
        }

        // §4c 显式集火顺序槽；未领到槽不进 CD
        if (!FocusFireSequencer.TryClaimVolleySlot(bf, u))
        {
            return;
        }

        var roundDmg = CombatMarkService.ScaleIncomingDamage(target, u.salvoRoundDmg);
        var applied = roundDmg;
        if (target.isBuilding)
        {
            applied = BuildingCombatRules.ClampBuildingDamage(bf, target, roundDmg);
        }
        ApplyDamage(bf, target, applied, u, state, ships, modules);
        // 特效旁路：混伤主炮弹道（不改结算）；导弹/舰载机不发
        if (!u.IsTemplateCarriedUnit())
        {
            CombatFxEmit.HybridGunTracer(bf, u, target, dist);
        }

        CombatDamageDiagnostics.LogFire(u, target, dist, roundDmg, u.fireCycleSec);
        CombatTelemetryLog.LogSalvo(u, target, roundDmg, u.fireCycleSec, applied);
        if (u.IsTemplateCarriedUnit())
        {
            var channel = "MISSILE".Equals(u.tonnageClass, StringComparison.Ordinal) ? "missile-fire" : "wing-fire";
            CombatTelemetryLog.Log(channel, $"{u.unitId}→{target.unitId} dmg={applied:0}");
            CombatTelemetryLog.LogWingDamage(u, target, applied);
        }
        if (target.isBuilding && building != null)
        {
            BuildingCombatRules.TryFinishBuildingDestroyed(bf, building, target);
        }
        u.fireCooldownSec = u.fireCycleSec > 0.01f ? u.fireCycleSec : SalvoProfileService.DefaultFireCycleSec;
    }

    private const float WeaponFireAlignmentRad = 0.087f;

    /// <returns>false 时本 tick 仅转向、不开火。</returns>
    private static bool TrySlewWeaponTowardTarget(
        BattlefieldUnit u,
        float dx,
        float dy,
        float dz,
        float dtSec)
    {
        if (u.weaponTrackingDegPerSec <= 0.001f)
        {
            return true;
        }

        var horiz = MathF.Sqrt(dx * dx + dy * dy);
        if (horiz < 0.01f && MathF.Abs(dz) < 0.01f)
        {
            return true;
        }

        var desiredYaw = MathF.Atan2(dy, dx);
        var desiredPitch = horiz > 0.01f ? MathF.Atan2(dz, horiz) : 0f;
        var yawErr = NormalizeAngleRad(desiredYaw - u.facingRad);
        var pitchErr = desiredPitch - u.pitchRad;
        var maxStep = u.weaponTrackingDegPerSec * (MathF.PI / 180f) * MathF.Max(dtSec, 0.001f);

        if (MathF.Abs(yawErr) > maxStep)
        {
            u.facingRad = NormalizeAngleRad(u.facingRad + MathF.Sign(yawErr) * maxStep);
            return false;
        }

        u.facingRad = desiredYaw;

        if (MathF.Abs(pitchErr) > maxStep)
        {
            u.pitchRad += MathF.Sign(pitchErr) * maxStep;
            return false;
        }

        u.pitchRad = desiredPitch;
        return MathF.Abs(yawErr) <= WeaponFireAlignmentRad && MathF.Abs(pitchErr) <= WeaponFireAlignmentRad;
    }

    private static float NormalizeAngleRad(float rad)
    {
        while (rad > MathF.PI)
        {
            rad -= MathF.PI * 2f;
        }

        while (rad < -MathF.PI)
        {
            rad += MathF.PI * 2f;
        }

        return rad;
    }

    // liket0coode345

    public static void ApplyDamage(BattlefieldUnit target, float dmg) =>
        ApplyDamage(null, target, dmg, null);

    public static void ApplyDamage(BattlefieldState? bf, BattlefieldUnit target, float dmg) =>
        ApplyDamage(bf, target, dmg, null);

    public static void ApplyLayerRepair(
        BattlefieldState battlefield,
        BattlefieldUnit target,
        string layer,
        float amount)
    {
        if (target.IsDestroyed() || amount <= 0f)
        {
            return;
        }

        var shield = 0f;
        var armor = 0f;
        var structure = 0f;
        switch (layer)
        {
            case "shield":
                var shieldBefore = target.shieldHp;
                target.shieldHp = Math.Min(target.shieldMax, target.shieldHp + amount);
                shield = target.shieldHp - shieldBefore;
                break;
            case "armor":
                var armorBefore = target.armorHp;
                target.armorHp = Math.Min(target.armorMax, target.armorHp + amount);
                armor = target.armorHp - armorBefore;
                break;
            case "structure":
                var structureBefore = target.structureHp;
                target.structureHp = Math.Min(target.structureMax, target.structureHp + amount);
                structure = target.structureHp - structureBefore;
                break;
        }
        if (shield > 0f || armor > 0f || structure > 0f)
        {
            QueueHpDelta(battlefield, target, shield, armor, structure, isHeal: true);
        }
    }

    /// <summary>直扣结构层，跳过盾甲（结构扰动导弹等）。</summary>
    public static void ApplyStructureOnlyDamage(
        BattlefieldState? bf,
        BattlefieldUnit target,
        float dmg,
        BattlefieldUnit? attacker = null)
    {
        if (dmg <= 0f)
        {
            return;
        }

        var wasAlive = !target.IsDestroyed();
        var before = target.structureHp;
        target.structureHp -= dmg;
        if (target.structureHp < 0f)
        {
            target.structureHp = 0f;
        }

        if (bf != null)
        {
            QueueHpDelta(bf, target, 0f, 0f, before - target.structureHp, isHeal: false);
            CombatDamageLedger.RecordHit(bf, attacker, target, before - target.structureHp);
        }

        if (target.structureHp <= 0f)
        {
            target.alive = false;
        }
    }

    /// <summary>混伤顺序盾→甲→结构（威慑炮等）；走标准 ApplyDamage 管线。</summary>
    public static void ApplyMixedDamage(
        BattlefieldState? bf,
        BattlefieldUnit target,
        float dmg,
        BattlefieldUnit? attacker,
        GameState? state = null,
        ShipRegistry? ships = null,
        ModuleRegistry? modules = null) =>
        ApplyDamage(bf, target, dmg, attacker, state, ships, modules);

    public static void ApplyDamage(
        BattlefieldState? bf,
        BattlefieldUnit target,
        float dmg,
        BattlefieldUnit? attacker,
        GameState? state = null,
        ShipRegistry? ships = null,
        ModuleRegistry? modules = null)
    {
        if (dmg <= 0f)
        {
            return;
        }

        var wasAlive = !target.IsDestroyed();

        if (target.isBuilding)
        {
            if (state != null && SkirmishBuildingRules.IsSkirmish(state) && attacker != null)
            {
                var building = BuildingService.Find(state, target.buildingId);
                if (building != null && !SkirmishBuildingRules.CanDamageBuilding(state, attacker, building, target))
                {
                    return;
                }
            }

            var before = target.structureHp;
            target.structureHp -= dmg;
            if (target.structureHp <= 0f)
            {
                target.structureHp = 0f;
                target.alive = false;
            }
            if (bf != null)
            {
                QueueHpDelta(bf, target, 0f, 0f, before - target.structureHp, isHeal: false);
                CombatDamageLedger.RecordHit(bf, attacker, target, before - target.structureHp);
            }

            if (wasAlive && target.IsDestroyed() && bf != null && target.unitId != null)
            {
                FocusFireSequencer.OnTargetDestroyed(bf, target.unitId);
            }

            if (state != null && target.structureHp <= 0f && target.buildingId != null)
            {
                var building = BuildingService.Find(state, target.buildingId);
                if (building != null)
                {
                    SkirmishBuildingRules.OnBuildingStructureZero(state, building);
                }
            }

            return;
        }

        var shipsReg = ships ?? ShipRegistry.LoadDefault();
        var modRegistry = modules ?? ModuleRegistry.LoadDefault();
        dmg = CombatMarkService.ScaleIncomingDamage(target, dmg);

        var shieldBefore = target.shieldHp;
        var armorBefore = target.armorHp;
        var structureBefore = target.structureHp;

        var ctx = FieldAuraDamageRouter.Route(bf, target, dmg, attacker, modRegistry, shipsReg);
        ctx = DamageMitigationService.ApplyMitigation(ctx, modRegistry);
        ctx.shieldDamage *= RuntimeEffectService.EffectiveLayerDamageMultiplier(
            target, RuntimeEffectKind.ShieldResistPct, modRegistry);
        ctx.armorDamage *= RuntimeEffectService.EffectiveLayerDamageMultiplier(
            target, RuntimeEffectKind.ArmorResistPct, modRegistry);

        if (bf != null)
        {
            FieldAuraDamageRouter.ApplyRoutedDamage(bf, ctx, modRegistry, shipsReg);
        }
        else
        {
            if (ctx.shieldDamage > 0f && target.shieldHp > 0f)
            {
                var absorbed = Math.Min(target.shieldHp, ctx.shieldDamage);
                target.shieldHp -= absorbed;
            }

            if (ctx.armorDamage > 0f && target.armorHp > 0f)
            {
                var absorbed = Math.Min(target.armorHp, ctx.armorDamage);
                target.armorHp -= absorbed;
            }

            if (ctx.structureDamage > 0f)
            {
                target.structureHp -= ctx.structureDamage;
            }
        }

        var shieldDelta = shieldBefore - target.shieldHp;
        var armorDelta = armorBefore - target.armorHp;
        var structureDelta = structureBefore - target.structureHp;
        if (target.structureHp <= 0f)
        {
            target.alive = false;
        }

        if (bf != null && (shieldDelta > 0f || armorDelta > 0f || structureDelta > 0f))
        {
            QueueHpDelta(bf, target, shieldDelta, armorDelta, structureDelta, isHeal: false);
            CombatDamageLedger.RecordHit(bf, attacker, target, shieldDelta + armorDelta + structureDelta);
            FieldAuraDamageRouter.AfterDamageTick(bf, target, modRegistry);
        }

        if (wasAlive && target.IsDestroyed())
        {
            if (bf != null)
            {
                CarriedUnitDeploymentService.OnParentDestroyed(bf, target, modRegistry);
            }
            if (bf != null && target.unitId != null)
            {
                FocusFireSequencer.OnTargetDestroyed(bf, target.unitId);
            }

            if (state != null && bf != null && SkirmishBuildingRules.IsSkirmish(state))
            {
                SkirmishScoreService.OnUnitDestroyed(
                    state, bf, target, attacker, ships ?? ShipRegistry.LoadDefault());
                SkirmishRespawnService.QueueRespawn(state, target);
            }

            if (target.parentUnitId != null)
            {
                CombatTelemetryLog.LogWingSummary(target);
                LaunchTubeStateService.NotifyChildDestroyed(bf, target);
            }
            if (bf != null && state != null)
            {
                BattleReportService.TryGenerateOnDestroy(
                    state, bf, target,
                    ships ?? ShipRegistry.LoadDefault(),
                    modules ?? ModuleRegistry.LoadDefault());
            }
        }
    }

    private static void QueueHpDelta(
        BattlefieldState bf,
        BattlefieldUnit target,
        float shieldDelta,
        float armorDelta,
        float structureDelta,
        bool isHeal) =>
        CombatHpDeltaQueue.Enqueue(bf, target, shieldDelta, armorDelta, structureDelta, isHeal);

    // liketocoode3a5

    private static void CheckVictory(BattlefieldState bf)
    {
        var friendlyAlive = false;
        var enemyAlive = false;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || !u.Arrived(bf.timeSec))
            {
                continue;
            }
            if (BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }
            if (u.isBuilding)
            {
                if (bf.combatSubtype == CombatSubtype.BUILDING_ASSAULT && !u.IsDestroyed())
                {
                    if (u.side == UnitSide.FRIENDLY)
                    {
                        friendlyAlive = true;
                    }
                    else
                    {
                        enemyAlive = true;
                    }
                }
                continue;
            }
            if (u.side == UnitSide.FRIENDLY)
            {
                friendlyAlive = true;
            }
            else
            {
                enemyAlive = true;
            }
        }

        if (!friendlyAlive && !enemyAlive)
        {
            return;
        }

        if (!friendlyAlive)
        {
            bf.finished = true;
            bf.winnerSide = UnitSide.ENEMY;
            return;
        }

        if (!enemyAlive)
        {
            // #region agent log
            AgentSessionDebugLog.Write(
                "H8",
                "BattlefieldSystem.CheckVictory",
                "skip_friendly_only",
                new { bfId = bf.battlefieldId, friendlyAlive, enemyAlive });
            // #endregion
            return;
        }

        if (bf.timeSec > BattleTimeoutSec && !bf.finished)
        {
            bf.finished = true;
            bf.winnerSide = friendlyAlive && !enemyAlive ? UnitSide.FRIENDLY
                : enemyAlive && !friendlyAlive ? UnitSide.ENEMY
                : null;
            bf.winReason = "timeout";
        }
    }

    /// <summary>战场仍有非建筑舰船（含跃迁中）时应继续 sim tick。</summary>
    public static bool HasShipCombatPresence(GameState state, BattlefieldState bf)
    {
        if (bf.battlefieldId == null)
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            if (u.warpPhase != TacticalWarpPhase.None || u.inTacticalWarp)
            {
                return true;
            }

            if (u.Arrived(bf.timeSec))
            {
                return true;
            }
        }

        foreach (var transit in state.tacticalWarpInTransit)
        {
            if (transit.unit.IsDestroyed() || transit.unit.isBuilding)
            {
                continue;
            }

            if (bf.battlefieldId.Equals(transit.fromBattlefieldId, StringComparison.Ordinal)
                || bf.battlefieldId.Equals(transit.toBattlefieldId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var otherBf in state.battlefields)
        {
            foreach (var u in otherBf.units)
            {
                if (u.IsDestroyed() || u.isBuilding || BattlefieldSceneProxyService.IsSceneProxy(u))
                {
                    continue;
                }

                if (u.warpPhase is not (TacticalWarpPhase.PrepareInitiate or TacticalWarpPhase.ApproachProxy))
                {
                    continue;
                }

                if (bf.battlefieldId.Equals(u.warpTargetBfId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool ShouldPruneBattlefield(GameState state, BattlefieldState bf) =>
        bf.battlefieldId != null
        && bf.finished
        && !ShouldTickBattlefield(state, bf)
        && !SkirmishPhaseRules.IsActiveMatch(state);

    public static bool ShouldTickBattlefield(GameState state, BattlefieldState bf) =>
        IsActiveRealtimeBattlefield(state, bf) || HasShipCombatPresence(state, bf);

    private static bool IsActiveRealtimeBattlefield(GameState state, BattlefieldState bf) =>
        state.combatRealtimeActive
        && state.activeBattlefieldId != null
        && bf.battlefieldId != null
        && state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal);

    /// <summary>误标 finished 的舰船战场在实时战中恢复 tick。</summary>
    private static void HealMisfinishedShipBattlefields(GameState state)
    {
        if (!state.combatRealtimeActive)
        {
            return;
        }

        foreach (var bf in state.battlefields)
        {
            if (!bf.finished || bf.winnerSide != UnitSide.FRIENDLY || !HasShipCombatPresence(state, bf))
            {
                continue;
            }

            var enemyAlive = false;
            foreach (var u in bf.units)
            {
                if (u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.isBuilding
                    || BattlefieldSceneProxyService.IsSceneProxy(u))
                {
                    continue;
                }

                if (u.side == UnitSide.ENEMY)
                {
                    enemyAlive = true;
                    break;
                }
            }

            if (!enemyAlive)
            {
                bf.finished = false;
                bf.winnerSide = null;
                bf.winReason = null;
                // #region agent log
                AgentSessionDebugLog.Write(
                    "H8",
                    "BattlefieldSystem.HealMisfinishedShipBattlefields",
                    "reopened",
                    new { bfId = bf.battlefieldId });
                // #endregion
            }
        }
    }

    public static BattlefieldUnit? FindPossessedUnit(GameState state, BattlefieldState bf)
    {
        if (state.possessingMemberId == null)
        {
            return null;
        }
        foreach (var u in bf.units)
        {
            if (state.possessingMemberId.Equals(u.memberId, StringComparison.Ordinal) && u.alive)
            {
                return u;
            }
        }
        return null;
    }

    public static BattlefieldUnit? FindUnit(BattlefieldState bf, string? id)
    {
        if (id == null)
        {
            return null;
        }
        foreach (var u in bf.units)
        {
            if (id.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            }
        }
        return null;
    }
}
