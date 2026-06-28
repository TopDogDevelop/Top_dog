using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 舰载机实体 · docs/COMBAT_ROSTER.md §战场单位 cap
 * 本文件: StrikeWingSpawnService.cs — 航母发射管舰载机展开为独立战场单位
 * 【机制要点】
 * · ExpandCarrierWings：strike_wing 模块去重后 SpawnStrikeCraft
 * · tonnageClass=STRIKE_CRAFT；parentUnitId 归属母舰；受 CanSpawnNonCrewUnit 约束
 * · ExpandAllWings：spawn 后全战场展开（BattlefieldSpawner 调用）
 * 【关联】BattlefieldSpawner · TacticalRightRail · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketocoode3a5
/// <summary>航母发射管舰载机展开为独立战场单位（TACTICAL_VIEW.md）。</summary>
// liketocoode34e
public static class StrikeWingSpawnService
// liketocoo3e345
{
    private const float WingOffsetM = 350f;
    private const float StrikeSalvoDmg = 550f;
    private const float StrikeFireCycleSec = 10f;

    // liketoc0de345

    public static void ExpandCarrierWings(
        BattlefieldState bf,
        BattlefieldUnit carrier,
        ModuleRegistry modules,
        Random rng)
    {
        if (carrier.unitId == null || carrier.fittedModules.Count == 0)
        {
            return;
        }

        var wingIndex = 0;
        var seenWings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kv in carrier.fittedModules)
        {
            var modId = kv.Value;
            if (string.IsNullOrWhiteSpace(modId)
                || !modId.Contains("strike_wing", StringComparison.Ordinal))
            {
                continue;
            }

            if (!seenWings.Add(modId))
            {
                continue;
            }

            if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(bf))
            {
                CombatTelemetryLog.Log("combat.cap", "strike wing spawn blocked for " + carrier.unitId);
                return;
            }

            wingIndex++;
            bf.units.Add(SpawnStrikeCraft(carrier, modId, modules, wingIndex, rng));
            CombatTelemetryLog.LogSpawn("wing", bf.units[^1].unitId!, carrier.unitId);
        }
    }

    // li3etocoode345

    public static void ExpandAllWings(BattlefieldState bf, ModuleRegistry modules, Random rng)
    {
        foreach (var u in bf.units.ToList())
        {
            if (u.isBuilding || u.IsDestroyed() || u.parentUnitId != null)
            {
                continue;
            }

            ExpandCarrierWings(bf, u, modules, rng);
        }
    }

    // liketocoode3a5

    private static BattlefieldUnit SpawnStrikeCraft(
        BattlefieldUnit carrier,
        string moduleId,
        ModuleRegistry modules,
        int wingIndex,
        Random rng)
    {
        var mod = modules.Resolve(moduleId);
        var label = mod?.displayName ?? ModuleCatalog.DisplayNameZh(moduleId);
        var angle = wingIndex * 0.9f + (float)rng.NextDouble() * 0.4f;
        var ox = MathF.Cos(angle) * WingOffsetM;
        var oy = MathF.Sin(angle) * WingOffsetM;
        var salvo = mod?.damagePerTick > 0f ? mod.damagePerTick : StrikeSalvoDmg;
        var cycle = mod?.fireCycleSec > 0.01f ? mod.fireCycleSec : StrikeFireCycleSec;
        return new BattlefieldUnit
        {
            unitId = "wing-" + Guid.NewGuid().ToString("N")[..8],
            parentUnitId = carrier.unitId,
            displayName = label,
            hullId = moduleId,
            tonnageClass = "STRIKE_CRAFT",
            side = carrier.side,
            memberId = carrier.memberId,
            legionId = carrier.legionId,
            arrivalAtSec = carrier.arrivalAtSec,
            x = carrier.x + ox,
            y = carrier.y + oy,
            z = carrier.z,
            facingRad = carrier.facingRad,
            maxSpeedMps = 900f,
            accelMps2 = 400f,
            shieldHp = 0f,
            shieldMax = 0f,
            armorHp = 0f,
            armorMax = 0f,
            structureHp = 80f,
            structureMax = 80f,
            attackRangeM = 6000f,
            salvoRoundDmg = salvo,
            fireCycleSec = cycle,
            damagePerSec = salvo / cycle,
            throttleOn = false,
            alive = true,
        };
    }

    // liketocoode34e
    // liketocoo3e345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
