using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0-§2 战斗视野 · §接近指令 · docs/TACTICAL_WARP_AND_ORDERS.md §1.3
 * 本文件: BattlefieldSystem.cs — 实时战场 tick（运动·自火·salvo·胜负）
 * 【机制要点】
 * · Tick：逐 battlefield 积分 timeSec，调用 TickBattlefield + 胜负判定
 * · ApplyAiMovement：MANUAL/RETREAT/STOP/SCATTER/FOLLOW/APPROACH/AWAY/ORBIT/RALLY
 * · TickApproachOrAway：每 1s SnapHeadingToward/Away + 满引擎；进射程 STOP
 * · TryFireSalvo：射程内开火 → ApplyDamage + CombatDamageLedger + BattleReport
 * · CheckVictory：友敌存活 + 300s timeout
 * 【关联】ShipMotionIntegrator · FleetOrderService · MissileProjectileService · BattleReportService
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

        foreach (var bf in state.battlefields)
        {
            if (bf.finished)
            {
                continue;
            }

            bf.timeSec += dtSec;
            TickBattlefield(state, bf, modules, ships, dtSec);
            CombatTelemetryLog.MaybeLogPositions(bf, bf.timeSec);
            if (!bf.finished)
            {
                if (bf.combatSubtype == CombatSubtype.BUILDING_ASSAULT)
                {
                    // BuildingCombatRules.TickBuildingWin already called in TickBattlefield
                }
                else if (HarvestCombatRules.IsHarvestBattlefield(bf))
                {
                    HarvestCombatRules.TickHarvestWin(state, bf);
                }
                else
                {
                    CheckVictory(bf);
                }
            }
        }

        state.battlefields.RemoveAll(bf =>
            bf.finished && bf.battlefieldId != null
            && !bf.battlefieldId.Equals(state.activeBattlefieldId, StringComparison.Ordinal));
    }

    // li3etocoode345

    private static void TickBattlefield(
        GameState state,
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec)
    {
        PossessionInputService.ApplyPending(state, bf, dtSec);
        TacticalWarpService.Tick(state, bf, dtSec);
        AiRealtimePlayerBrain.Tick(state, bf, dtSec);
        MissileProjectileService.Tick(state, bf, modules, ships, dtSec);

        var building = BuildingService.Find(state, bf.targetBuildingId);
        if (bf.targetBuildingId != null)
        {
            BuildingCombatRules.TickBuildingWin(bf, building);
        }

        var possessed = FindPossessedUnit(state, bf);
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.inTacticalWarp)
            {
                continue;
            }

            ApplyAiMovement(state, bf, u, possessed, dtSec);
            if (u.aiOrder != UnitAiOrder.MANUAL)
            {
                AutoFireTargetingService.Tick(bf, state, u);
            }
            TryShieldRepairSalvo(bf, u, dtSec);
            TryFireSalvo(bf, state, building, u, dtSec, ships, modules);
        }
    }

    // liketocoode3a5

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

        if (u.aiOrder == UnitAiOrder.FOLLOW && possessed != null && !ReferenceEquals(u, possessed))
        {
            SteerTowardPoint(u, possessed.x, possessed.y, possessed.z, dtSec);
            u.throttleOn = true;
            ShipMotionIntegrator.TickUnit(u, dtSec);
            return;
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

        if (u.aiOrder == UnitAiOrder.ORBIT && u.orbitTargetUnitId != null)
        {
            var orbit = FindUnit(bf, u.orbitTargetUnitId);
            if (orbit != null)
            {
                OrbitTarget(u, orbit, u.attackRangeM * 0.85f, dtSec);
                ShipMotionIntegrator.TickUnit(u, dtSec);
                return;
            }
        }

        if (u.aiOrder == UnitAiOrder.RALLY)
        {
            var anchor = possessed ?? FindUnit(bf, u.rallyPointUnitId);
            if (anchor != null)
            {
                SteerTowardPoint(u, anchor.x, anchor.y, anchor.z, dtSec);
                u.throttleOn = true;
            }
        }

        ShipMotionIntegrator.TickUnit(u, dtSec);
    }

    // liketocoo3e345

    private static void TickApproachOrAway(BattlefieldState bf, BattlefieldUnit u, float dtSec, bool away)
    {
        var target = FindUnit(bf, u.approachTargetUnitId!);
        if (target == null)
        {
            u.approachTargetUnitId = null;
            u.approachHeadingTimerSec = 0f;
            return;
        }

        if (!away)
        {
            var dx = target.x - u.x;
            var dy = target.y - u.y;
            var dz = target.z - u.z;
            var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var arriveDist = Math.Min(500f, u.attackRangeM * 0.95f);
            if (dist <= arriveDist)
            {
                u.aiOrder = UnitAiOrder.STOP;
                u.approachTargetUnitId = null;
                u.approachHeadingTimerSec = 0f;
                u.throttleOn = false;
                ShipMotionIntegrator.TickUnit(u, dtSec);
                return;
            }
        }

        u.throttleOn = true;
        u.approachHeadingTimerSec -= dtSec;
        if (u.approachHeadingTimerSec <= 0f)
        {
            if (away)
            {
                ShipMotionIntegrator.SnapHeadingAway(u, target.x, target.y, target.z);
            }
            else
            {
                ShipMotionIntegrator.SnapHeadingToward(u, target.x, target.y, target.z);
            }
            u.approachHeadingTimerSec = ShipMotionIntegrator.ApproachHeadingIntervalSec;
        }

        ShipMotionIntegrator.TickUnit(u, dtSec);
    }

    // liketoco0de345

    private static void OrbitTarget(BattlefieldUnit u, BattlefieldUnit target, float radius, float dtSec)
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

    private static void SteerTowardPoint(BattlefieldUnit u, float tx, float ty, float tz, float dtSec)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        var yaw = (float)Math.Atan2(dy, dx);
        var horiz = (float)Math.Sqrt(dx * dx + dy * dy);
        var pitch = horiz > 0.01f ? (float)Math.Atan2(dz, horiz) : 0f;
        ShipMotionIntegrator.SteerToward(u, yaw, pitch, dtSec);
    }

    // liketocoode3e5

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
        if (target == null || target.IsDestroyed())
        {
            return;
        }

        u.fireCooldownSec -= dtSec;
        var dx = target.x - u.x;
        var dy = target.y - u.y;
        var dz = target.z - u.z;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (u.fireCooldownSec > 0f || dist > u.attackRangeM)
        {
            return;
        }

        var roundDmg = u.salvoRoundDmg;
        var applied = roundDmg;
        if (target.isBuilding)
        {
            applied = BuildingCombatRules.ClampBuildingDamage(bf, target, roundDmg);
        }
        ApplyDamage(bf, target, applied, u, state, ships, modules);
        CombatDamageDiagnostics.LogFire(u, target, dist, roundDmg, u.fireCycleSec);
        CombatTelemetryLog.LogSalvo(u, target, roundDmg, u.fireCycleSec, applied);
        if (u.parentUnitId != null)
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

    // liket0coode345

    public static void ApplyDamage(BattlefieldUnit target, float dmg) =>
        ApplyDamage(null, target, dmg, null);

    public static void ApplyDamage(BattlefieldState? bf, BattlefieldUnit target, float dmg) =>
        ApplyDamage(bf, target, dmg, null);

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
            var before = target.structureHp;
            target.structureHp -= dmg;
            if (target.structureHp <= 0f)
            {
                target.structureHp = 0f;
            }
            if (bf != null)
            {
                QueueHpDelta(bf, target, 0f, 0f, before - target.structureHp, isHeal: false);
                CombatDamageLedger.RecordHit(bf, attacker, target, before - target.structureHp);
            }
            return;
        }

        var shieldDelta = 0f;
        var armorDelta = 0f;
        var structureDelta = 0f;

        if (target.shieldHp > 0f)
        {
            var absorbed = Math.Min(target.shieldHp, dmg);
            target.shieldHp -= absorbed;
            shieldDelta = absorbed;
            dmg -= absorbed;
        }
        if (dmg > 0f && target.armorHp > 0f)
        {
            var absorbed = Math.Min(target.armorHp, dmg);
            target.armorHp -= absorbed;
            armorDelta = absorbed;
            dmg -= absorbed;
        }
        if (dmg > 0f)
        {
            target.structureHp -= dmg;
            structureDelta = dmg;
        }
        if (target.structureHp <= 0f)
        {
            target.alive = false;
        }

        if (bf != null && (shieldDelta > 0f || armorDelta > 0f || structureDelta > 0f))
        {
            QueueHpDelta(bf, target, shieldDelta, armorDelta, structureDelta, isHeal: false);
            CombatDamageLedger.RecordHit(bf, attacker, target, shieldDelta + armorDelta + structureDelta);
        }

        if (wasAlive && target.IsDestroyed())
        {
            if (target.parentUnitId != null)
            {
                CombatTelemetryLog.LogWingSummary(target);
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
        bool isHeal)
    {
        bf.pendingHpDeltas.Add(new CombatHpDeltaEvent
        {
            targetUnitId = target.unitId,
            worldX = target.x,
            worldY = target.y,
            worldZ = target.z,
            shieldDelta = shieldDelta,
            armorDelta = armorDelta,
            structureDelta = structureDelta,
            isHeal = isHeal,
            isBuilding = target.isBuilding,
            battleTimeSec = bf.timeSec,
        });
    }

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
            if (u.isBuilding)
            {
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

        if (!friendlyAlive || !enemyAlive)
        {
            bf.finished = true;
            bf.winnerSide = friendlyAlive && !enemyAlive ? UnitSide.FRIENDLY
                : enemyAlive && !friendlyAlive ? UnitSide.ENEMY
                : null;
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
