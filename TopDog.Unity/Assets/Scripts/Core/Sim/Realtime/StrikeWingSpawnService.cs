using TopDog.Content.Modules;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 舰载机实体 · docs/COMBAT_ROSTER.md §战场单位 cap
 * 本文件: StrikeWingSpawnService.cs — 航母发射管舰载机展开为独立战场单位
 * 【机制要点】
 * · ExpandCarrierWings：strike_wing 发射管 Inactive 时放出；集火指令触发
 * · 不再 spawn 后全战场 ExpandAllWings（BattlefieldSpawner 已移除）
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
        foreach (var kv in carrier.fittedModules)
        {
            var modId = kv.Value;
            if (string.IsNullOrWhiteSpace(modId)
                || !IsLaunchTubeChildModule(modId))
            {
                continue;
            }

            if (!kv.Key.StartsWith("tube_", StringComparison.Ordinal))
            {
                continue;
            }

            if (carrier.tubeStates.TryGetValue(kv.Key, out var tubeState)
                && tubeState != LaunchTubeState.Inactive)
            {
                continue;
            }

            if (HasLiveWingFromTube(bf, carrier.unitId!, modId))
            {
                continue;
            }

            if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(bf))
            {
                CombatTelemetryLog.Log("combat.cap", "strike wing spawn blocked for " + carrier.unitId);
                return;
            }

            wingIndex++;
            bf.units.Add(SpawnChildCraftFromTube(carrier, modId, modules, wingIndex, rng));
            LaunchTubeStateService.OnWingLaunched(carrier, kv.Key);
            CombatTelemetryLog.LogSpawn("wing", bf.units[^1].unitId!, carrier.unitId);
        }
    }

    /// <summary>集火指令：对指挥范围内的航母放出尚未部署的舰载机。</summary>
    public static void DeployForFocusCommand(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        ModuleRegistry modules,
        Random rng)
    {
        foreach (var carrier in ResolveCarriersForFocus(bf, selectedFriendlyUnitIds))
        {
            ExpandCarrierWings(bf, carrier, modules, rng);
        }
    }

    private static IEnumerable<BattlefieldUnit> ResolveCarriersForFocus(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds)
    {
        if (selectedFriendlyUnitIds != null && selectedFriendlyUnitIds.Count > 0)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in selectedFriendlyUnitIds)
            {
                var u = BattlefieldSystem.FindUnit(bf, id);
                if (u != null && StrikeWingRecallService.IsCarrier(u) && seen.Add(u.unitId!))
                {
                    yield return u;
                }

                foreach (var wing in WingsOfCarrier(bf, id))
                {
                    if (wing.parentUnitId != null
                        && seen.Add(wing.parentUnitId)
                        && BattlefieldSystem.FindUnit(bf, wing.parentUnitId) is { } parent
                        && StrikeWingRecallService.IsCarrier(parent))
                    {
                        yield return parent;
                    }
                }
            }

            yield break;
        }

        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY
                && !u.IsDestroyed()
                && !u.isBuilding
                && u.parentUnitId == null
                && StrikeWingRecallService.IsCarrier(u))
            {
                yield return u;
            }
        }
    }

    private static IEnumerable<BattlefieldUnit> WingsOfCarrier(BattlefieldState bf, string carrierUnitId)
    {
        foreach (var u in bf.units)
        {
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
                && !u.IsDestroyed())
            {
                yield return u;
            }
        }
    }

    private static bool HasLiveWingFromTube(BattlefieldState bf, string carrierUnitId, string modId)
    {
        foreach (var u in bf.units)
        {
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && modId.Equals(u.hullId, StringComparison.Ordinal)
                && u.alive
                && !u.IsDestroyed())
            {
                return true;
            }
        }

        return false;
    }

    [Obsolete("Wings deploy on focus only; kept for tests.")]
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

    private static bool IsLaunchTubeChildModule(string modId) =>
        modId.Contains("strike_wing", StringComparison.Ordinal)
        || modId.Contains("drone", StringComparison.Ordinal)
        && !modId.Contains("queen", StringComparison.Ordinal);

    private static BattlefieldUnit SpawnChildCraftFromTube(
        BattlefieldUnit carrier,
        string moduleId,
        ModuleRegistry modules,
        int wingIndex,
        Random rng)
    {
        var isDrone = moduleId.Contains("drone", StringComparison.Ordinal)
            && !moduleId.Contains("queen", StringComparison.Ordinal);
        return isDrone
            ? SpawnDroneCraft(carrier, moduleId, modules, wingIndex, rng)
            : SpawnStrikeCraft(carrier, moduleId, modules, wingIndex, rng);
    }

    private static BattlefieldUnit SpawnDroneCraft(
        BattlefieldUnit carrier,
        string moduleId,
        ModuleRegistry modules,
        int wingIndex,
        Random rng)
    {
        var craft = SpawnStrikeCraft(carrier, moduleId, modules, wingIndex, rng);
        craft.tonnageClass = "DRONE";
        craft.unitId = "drone-" + Guid.NewGuid().ToString("N")[..8];
        return craft;
    }

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
