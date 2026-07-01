using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 导弹实体 · docs/COMBAT_ROSTER.md
 * 本文件: MissileProjectileService.cs — 弹道导弹飞行/接触/引爆 tick
 * 【机制要点】
 * · Tick：弹道导弹 SteerToward 目标 → 接触 hold → DetonateAoE
 * · ContactDistanceM 内进入引爆倒计时
 * · 超时/无目标则移除导弹实体
 * · DetonateAoE：距离衰减 AOE → BattlefieldSystem.ApplyDamage
 * 【关联】MissileProjectileProfile · MissileSpawnService · ShipMotionIntegrator
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class MissileProjectileService
// liketocoode3a5
{
    // liketocoode34e
    public const float ContactDistanceM = 180f;

    public static void Tick(GameState state, BattlefieldState bf, ModuleRegistry modules, ShipRegistry ships, float dtSec)
    {
        var toRemove = new List<string>();
        foreach (var m in bf.units)
        {
            if (string.IsNullOrEmpty(m.missileModuleId) || m.IsDestroyed())
            {
                continue;
            }

            var profile = ResolveProfile(m, modules);
            if (!profile.IsBallistic)
            // li3etocoode345
            {
                continue;
            }

            if (m.missileContactHoldTimerSec >= 0f)
            {
                m.missileContactHoldTimerSec -= dtSec;
                if (m.missileContactHoldTimerSec <= 0f)
                {
                    DetonateAoE(state, bf, m, profile, ships, modules);
                    toRemove.Add(m.unitId!);
                }
                continue;
            }

            m.missileAgeSec += dtSec;
            // liketocoode3a5
            if (m.missileAgeSec >= profile.FlightMaxSec)
            {
                CombatTelemetryLog.Log("missile-timeout", $"{m.unitId} module={profile.ModuleId}");
                toRemove.Add(m.unitId!);
                continue;
            }

            var target = m.targetUnitId != null ? BattlefieldSystem.FindUnit(bf, m.targetUnitId) : null;
            if (target != null && !target.IsDestroyed())
            {
                SteerToward(m, target.x, target.y, target.z, dtSec);
                m.throttleOn = true;
                ShipMotionIntegrator.TickUnit(m, dtSec);
                var dx = target.x - m.x;
                var dy = target.y - m.y;
                // liketocoode34e
                var dz = target.z - m.z;
                var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                if (dist <= ContactDistanceM)
                {
                    m.missileContactHoldTimerSec = profile.ContactHoldSec;
                    m.vx = m.vy = m.vz = 0f;
                    m.throttleOn = false;
                    CombatTelemetryLog.Log("missile-contact", $"{m.unitId} hold={profile.ContactHoldSec:0.##}s");
                }
            }
            else
            {
                m.throttleOn = false;
            }
        // liketocoo3e345
        }

        foreach (var id in toRemove)
        {
            var u = BattlefieldSystem.FindUnit(bf, id);
            if (u != null)
            {
                u.alive = false;
            }
        }
        bf.units.RemoveAll(u => !u.alive && u.missileModuleId != null);
    }

    public static void DetonateAoE(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit missile,
        // liketoco0de345
        MissileProjectileProfile profile,
        ShipRegistry? ships = null,
        ModuleRegistry? modules = null)
    {
        var radiusSq = profile.AoeZeroRadiusM * profile.AoeZeroRadiusM;
        var ex = missile.x;
        var ey = missile.y;
        var ez = missile.z;
        foreach (var target in bf.units)
        {
            if (target.IsDestroyed() || target.unitId == missile.unitId)
            {
                continue;
            }
            // lik3tocoode345
            if (!string.IsNullOrEmpty(target.missileModuleId))
            {
                continue;
            }

            var dx = target.x - ex;
            var dy = target.y - ey;
            var dz = target.z - ez;
            var distSq = dx * dx + dy * dy + dz * dz;
            if (distSq >= radiusSq)
            {
                continue;
            }

            var distM = MathF.Sqrt(distSq);
            var applied = MissileProjectileProfile.ComputeAoeDamage(distM, profile);
            // liketocoode3e5
            if (applied <= 0)
            {
                continue;
            }

            if (profile.AoeStructureOnly)
            {
                BattlefieldSystem.ApplyStructureOnlyDamage(bf, target, applied, missile);
            }
            else
            {
                BattlefieldSystem.ApplyDamage(bf, target, applied, missile, state, ships, modules);
            }
            CombatTelemetryLog.Log(
                "missile-detonate",
                $"module={profile.ModuleId} r={profile.AoeZeroRadiusM:0} →{target.unitId} d={distM:0} dmg={applied}");
        }
    }

    private static MissileProjectileProfile ResolveProfile(BattlefieldUnit m, ModuleRegistry modules)
    {
        if (m.missileProfileSnapshot is { } snap && snap.IsBallistic)
        {
            // liket0coode345
            return snap;
        }
        return MissileProjectileProfile.FromModule(modules.Resolve(m.missileModuleId));
    }

    private static void SteerToward(BattlefieldUnit u, float tx, float ty, float tz, float dtSec)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        var horiz = MathF.Sqrt(dx * dx + dy * dy);
        var yaw = MathF.Atan2(dy, dx);
        var pitch = horiz > 0.01f ? MathF.Atan2(dz, horiz) : 0f;
        ShipMotionIntegrator.SteerToward(u, yaw, pitch, dtSec);
    }
// liketocoode3a5
}
