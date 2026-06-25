using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Sim.Realtime;

public static class BattlefieldSystem
{
    public const float BattleTimeoutSec = 300f;

    public static void Tick(GameState state, float dtSec)
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
            TickBattlefield(state, bf, dtSec);
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

    private static void TickBattlefield(GameState state, BattlefieldState bf, float dtSec)
    {
        PossessionInputService.ApplyPending(state, bf, dtSec);
        BoardSummonApproachService.TickWarpArrivals(bf, new Random((int)(bf.timeSec * 17)));
        TacticalWarpService.Tick(state, bf, dtSec);
        AiRealtimePlayerBrain.Tick(state, bf, dtSec);

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
            MoveAndFire(bf, state, building, u, dtSec);
        }
    }

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
            var approach = FindUnit(bf, u.approachTargetUnitId);
            if (approach != null)
            {
                var dx = approach.x - u.x;
                var dy = approach.y - u.y;
                var dz = approach.z - u.z;
                var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (dist <= u.attackRangeM * 0.9f)
                {
                    u.throttleOn = false;
                }
                else
                {
                    SteerTowardPoint(u, approach.x, approach.y, approach.z, dtSec);
                    u.throttleOn = true;
                }

                ShipMotionIntegrator.TickUnit(u, dtSec);
                return;
            }
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

    private static void MoveAndFire(
        BattlefieldState bf,
        GameState state,
        BuildingState? building,
        BattlefieldUnit u,
        float dtSec)
    {
        if (u.isBuilding)
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
        if (u.fireCooldownSec <= 0f && dist <= u.attackRangeM)
        {
            var dmg = u.damagePerSec * 0.5f;
            if (target.isBuilding)
            {
                dmg = BuildingCombatRules.ClampBuildingDamage(bf, target, dmg, dtSec);
            }
            ApplyDamage(target, dmg);
            CombatDamageDiagnostics.LogFire(u, target, dist, dmg);
            if (target.isBuilding && building != null)
            {
                BuildingCombatRules.TryFinishBuildingDestroyed(bf, building, target);
            }
            u.fireCooldownSec = 1f;
        }
    }

    public static void ApplyDamage(BattlefieldUnit target, float dmg)
    {
        if (target.isBuilding)
        {
            target.structureHp -= dmg;
            if (target.structureHp <= 0f)
            {
                target.structureHp = 0f;
            }
            return;
        }

        if (target.shieldHp > 0f)
        {
            var absorbed = Math.Min(target.shieldHp, dmg);
            target.shieldHp -= absorbed;
            dmg -= absorbed;
        }
        if (dmg > 0f && target.armorHp > 0f)
        {
            var absorbed = Math.Min(target.armorHp, dmg);
            target.armorHp -= absorbed;
            dmg -= absorbed;
        }
        if (dmg > 0f)
        {
            target.structureHp -= dmg;
        }
        if (target.structureHp <= 0f)
        {
            target.alive = false;
        }
    }

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
