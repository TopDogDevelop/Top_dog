using TopDog.Content.Modules;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 导弹实体
 * 本文件: MissileLaunchService.cs — 有目标且进射程时发射弹道导弹
 * 【机制要点】
 * · 不在 spawn 时预生成导弹（避免对空浪费与约战布局飘出）
 * · 目标须存活、敌对阵营、非导弹实体
 * 【关联】MissileSpawnService · BattlefieldSystem · AutoFireTargetingService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class MissileLaunchService
{
    public static void TryLaunch(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit launcher,
        ModuleRegistry modules,
        Random rng,
        float dtSec)
    {
        if (launcher.IsDestroyed()
            || launcher.isBuilding
            || launcher.IsBallisticMissile()
            || launcher.parentUnitId != null
            || launcher.fittedModules.Count == 0)
        {
            return;
        }

        if (!HasBallisticTube(launcher, modules))
        {
            return;
        }

        launcher.missileFireCooldownSec -= dtSec;
        if (launcher.missileFireCooldownSec > 0f)
        {
            return;
        }

        var target = ResolveLaunchTarget(bf, launcher);
        if (target == null)
        {
            return;
        }

        var dx = target.x - launcher.x;
        var dy = target.y - launcher.y;
        var dz = target.z - launcher.z;
        var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        var engageRangeM = ResolveEngageRangeM(launcher, modules);
        if (dist > engageRangeM)
        {
            return;
        }

        if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(bf))
        {
            return;
        }

        if (!MissileSpawnService.TryLaunchOne(bf, launcher, modules, rng, out var missile) || missile == null)
        {
            return;
        }

        missile.targetUnitId = target.unitId;

        var cycle = launcher.fireCycleSec > 0.01f
            ? launcher.fireCycleSec
            : SalvoProfileService.DefaultFireCycleSec;
        launcher.missileFireCooldownSec = cycle;
        CombatTelemetryLog.Log("missile-launch", $"{launcher.unitId}→{target.unitId} dist={dist:0}");
    }

    /// <summary>
    /// 弹道导弹交战距离：显式 attackRangeM，否则用飞行包络 speed×maxSec；
    /// 不再误用仅 ATTACK 槽算出的默认 6 km（会导致远距离「发不出去」）。
    /// </summary>
    public static float ResolveEngageRangeM(BattlefieldUnit launcher, ModuleRegistry modules)
    {
        var max = launcher.attackRangeM;
        foreach (var modId in launcher.fittedModules.Values)
        {
            if (!MissileSpawnService.IsMissileModuleId(modId))
            {
                continue;
            }

            var mod = modules.Resolve(modId);
            var profile = MissileProjectileProfile.FromModule(mod);
            if (!profile.IsBallistic)
            {
                continue;
            }

            if (mod != null && mod.attackRangeM > 0.01f)
            {
                max = MathF.Max(max, mod.attackRangeM);
                continue;
            }

            if (profile.FlightSpeedMps > 0.01f && profile.FlightMaxSec > 0.01f)
            {
                max = MathF.Max(max, profile.FlightSpeedMps * profile.FlightMaxSec);
            }
        }

        return max;
    }

    private static BattlefieldUnit? ResolveLaunchTarget(BattlefieldState bf, BattlefieldUnit launcher)
    {
        if (!string.IsNullOrWhiteSpace(launcher.targetUnitId))
        {
            var picked = BattlefieldSystem.FindUnit(bf, launcher.targetUnitId);
            if (IsValidMissileTarget(launcher, picked, bf))
            {
                return picked;
            }
        }

        var nearestId = AutoFireTargetingService.FindNearestEnemyId(bf, launcher);
        if (nearestId == null)
        {
            return null;
        }

        var nearest = BattlefieldSystem.FindUnit(bf, nearestId);
        return IsValidMissileTarget(launcher, nearest, bf) ? nearest : null;
    }

    private static bool IsValidMissileTarget(
        BattlefieldUnit launcher,
        BattlefieldUnit? target,
        BattlefieldState bf)
    {
        if (target == null
            || target.IsDestroyed()
            || !target.Arrived(bf.timeSec)
            || target.side == launcher.side
            || target.IsBallisticMissile()
            || BattlefieldSceneProxyService.IsSceneProxy(target))
        {
            return false;
        }

        return true;
    }

    private static bool HasBallisticTube(BattlefieldUnit launcher, ModuleRegistry modules)
    {
        foreach (var modId in launcher.fittedModules.Values)
        {
            if (!MissileSpawnService.IsMissileModuleId(modId))
            {
                continue;
            }

            var profile = MissileProjectileProfile.FromModule(modules.Resolve(modId));
            if (profile.IsBallistic)
            {
                return true;
            }
        }

        return false;
    }
}
