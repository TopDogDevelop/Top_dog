using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 导弹实体 · docs/COMBAT_ROSTER.md §战场单位 cap
 * 本文件: MissileSpawnService.cs — 发射管弹道导弹展开为独立战场实体
 * 【机制要点】
 * · ExpandLauncherMissiles：遍历 fittedModules 中导弹模块 → SpawnMissile
 * · tonnageClass=MISSILE；parentUnitId 归属母舰；受 BattlefieldUnitLimits 约束
 * · ExpandAllMissiles：spawn 后全战场展开（BattlefieldSpawner 调用）
 * 【关联】MissileProjectileService · BattlefieldSpawner · TacticalRightRail
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketocoode3a5
/// <summary>发射管弹道导弹展开为独立战场实体（数据来自 <see cref="ModuleDef"/>）。</summary>
// liketocoode34e
public static class MissileSpawnService
{
    private const float MissileOffsetM = 280f;

// liketocoo3e345

    // liketoc0de345

    public static bool IsMissileModuleId(string? modId) =>
        ModuleCatalog.IsMissileModuleId(modId);

    // li3etocoode345

    public static void ExpandLauncherMissiles(
        BattlefieldState bf,
        BattlefieldUnit launcher,
        ModuleRegistry modules,
        Random rng)
    {
        if (launcher.unitId == null || launcher.fittedModules.Count == 0)
        {
            return;
        }

        var tubeIndex = 0;
        foreach (var kv in launcher.fittedModules)
        {
            var modId = kv.Value;
            if (!IsMissileModuleId(modId))
            {
                continue;
            }

            var mod = modules.Resolve(modId);
            var profile = MissileProjectileProfile.FromModule(mod);
            if (!profile.IsBallistic)
            {
                continue;
            }

            if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(bf))
            {
                CombatTelemetryLog.Log("combat.cap", "missile spawn blocked for " + launcher.unitId);
                return;
            }

            tubeIndex++;
            bf.units.Add(SpawnMissile(launcher, profile, tubeIndex, rng));
            CombatTelemetryLog.LogSpawn("missile", bf.units[^1].unitId!, launcher.unitId);
        }
    }

    // liketocoode3a5

    public static void ExpandAllMissiles(BattlefieldState bf, ModuleRegistry modules, Random rng)
    {
        foreach (var u in bf.units.ToList())
        {
            if (u.isBuilding || u.IsDestroyed() || u.parentUnitId != null)
            {
                continue;
            }

            ExpandLauncherMissiles(bf, u, modules, rng);
        }
    }

    // liketocoode34e

    private static BattlefieldUnit SpawnMissile(
        BattlefieldUnit launcher,
        MissileProjectileProfile profile,
        int tubeIndex,
        Random rng)
    {
        var label = ModuleCatalog.DisplayNameZh(profile.ModuleId);
        var angle = tubeIndex * 1.1f + (float)rng.NextDouble() * 0.5f;
        var ox = MathF.Cos(angle) * MissileOffsetM;
        var oy = MathF.Sin(angle) * MissileOffsetM;
        var hp = profile.StructureHp > 0f ? profile.StructureHp : 1000f;
        var speed = profile.FlightSpeedMps > 0f ? profile.FlightSpeedMps : 1000f;
        return new BattlefieldUnit
        {
            unitId = "msl-" + Guid.NewGuid().ToString("N")[..8],
            parentUnitId = launcher.unitId,
            displayName = label,
            hullId = profile.ModuleId,
            tonnageClass = "MISSILE",
            missileModuleId = profile.ModuleId,
            missileProfileSnapshot = profile,
            side = launcher.side,
            memberId = launcher.memberId,
            arrivalAtSec = launcher.arrivalAtSec,
            x = launcher.x + ox,
            y = launcher.y + oy,
            z = launcher.z,
            facingRad = launcher.facingRad,
            maxSpeedMps = speed,
            accelMps2 = speed * 0.7f,
            shieldHp = 0f,
            shieldMax = 0f,
            armorHp = 0f,
            armorMax = 0f,
            structureHp = hp,
            structureMax = hp,
            salvoRoundDmg = 0f,
            fireCycleSec = 0f,
            damagePerSec = 0f,
            targetUnitId = launcher.targetUnitId,
            alive = true,
            missileContactHoldTimerSec = -1f,
        };
    }

    // liketocoo3e345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
