/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md · docs/MECHANISM_TEST_SCENARIOS.md · docs/SHIPS.md §配舰
 * 本文件: MechanismTestStressSpawnService.cs — mapMode=stress_10k_icons 的开场舰队/地图布置
 * 【机制要点】
 * · 与其它机制测相同：只读 JSON 开场状态/规模/散布；运行时无按本关特判
 * · 每舰 FittingValidator 合法填满开槽（同约战 AI）
 * ══
 */

using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Ship;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.Sim.MechanismTest;

public static class MechanismTestStressSpawnService
{
    public const float DefaultScatterRadiusM = 100_000f;

    public static void BootstrapBattlefields(
        GameState state,
        MechanismTestScenarioDef scenario,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (state.map?.Project.systems.Count == 0)
        {
            return;
        }

        state.battlefields.Clear();
        var sys = state.map.Project.systems[0];
        var belt = sys.eventRegions.Find(er =>
            MechanismMapGenerator.BeltRegionId.Equals(er.eventRegionId, StringComparison.Ordinal));
        if (belt?.eventRegionId == null)
        {
            return;
        }

        var regionRadiusM = belt.radiusKm * 1000.0;
        var scatterR = scenario.scatterRadiusM > 0f ? scenario.scatterRadiusM : DefaultScatterRadiusM;
        if (regionRadiusM < scatterR)
        {
            throw new InvalidOperationException(
                $"散布半径 {scatterR}m 超过事件区 {regionRadiusM}m");
        }

        var bf = new BattlefieldState
        {
            battlefieldId = "mt_bf_" + (scenario.scenarioId ?? "scenario") + "_" + belt.eventRegionId,
            systemId = sys.solarSystemId,
            eventRegionId = belt.eventRegionId,
            anchorAu = belt.anchorAu,
            subLocation = belt.name,
        };
        MechanismTestOpeningState.ApplyToBattlefield(bf, scenario);
        state.battlefields.Add(bf);

        var hulls = ships.AllHulls()
            .Where(h => h?.hullId != null
                        && !"SCENE_PROXY".Equals(h.tonnageClass, StringComparison.OrdinalIgnoreCase)
                        && !"BUILDING".Equals(h.tonnageClass, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (hulls.Count == 0)
        {
            throw new InvalidOperationException("无可用 hull");
        }

        var fitCache = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);
        EnsureLegions(state, scenario);
        var total = Math.Max(1, scenario.stressUnitCount > 0 ? scenario.stressUnitCount : 10_000);
        var factions = Math.Max(2, scenario.factionCount > 0 ? scenario.factionCount : 4);
        var per = total / factions;
        var remainder = total - per * factions;

        for (var f = 0; f < factions; f++)
        {
            var count = per + (f < remainder ? 1 : 0);
            var legionId = f == 0 ? "mt_stress_player" : $"mt_stress_enemy_{f}";
            for (var i = 0; i < count; i++)
            {
                var hull = hulls[rng.Next(hulls.Count)];
                SampleInBall(rng, scatterR, out var x, out var y, out var z);
                var u = new BattlefieldUnit
                {
                    unitId = $"stress-{f}-{i:D5}",
                    displayName = $"Stress F{f}-{i}",
                    hullId = hull.hullId,
                    memberId = f == 0 && i == 0 ? EnsurePlayerMember(state, legionId) : null,
                    legionId = legionId,
                    combatFactionId = f,
                    side = CombatHostility.SideForFaction(f),
                    arrivalAtSec = 0f,
                    x = x,
                    y = y,
                    z = z,
                    facingRad = (float)(rng.NextDouble() * Math.PI * 2),
                    fittedModules = FitRandomFillAllSlots(hull, modules, rng, fitCache),
                };
                ModuleRuntime.ApplyToUnit(u, hull, modules);
                bf.units.Add(u);
            }
        }

        state.autoFireEnabled = true;
        state.activeBattlefieldId = bf.battlefieldId;
        SkirmishDisplayNames.SyncSkirmishLabels(state);
        state.tacticalCameraUnitId = bf.units.FirstOrDefault(u => u.combatFactionId == 0)?.unitId;
        BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        MechanismTestPhaseRules.EnsureRealtimeCombat(state);
        MatchMemberBaselineService.EnsureSnapshot(state);
    }

    /// <summary>
    /// 按船体开槽：在 <see cref="FittingValidator.ModuleFitsSlot"/> 合法候选中随机选一，直到可填槽位塞满。
    /// 舰队防护模块每舰最多 1。
    /// </summary>
    public static Dictionary<string, string> FitRandomFillAllSlots(
        HullDef hull,
        ModuleRegistry modules,
        Random rng,
        Dictionary<string, Dictionary<string, List<string>>>? cache = null)
    {
        var fitted = new Dictionary<string, string>(StringComparer.Ordinal);
        var bySlot = ResolveSlotCandidates(hull, modules, cache);
        var slots = MemberFittingService.ListOpenSlots(hull);
        var hasFleetProtection = false;

        foreach (var slotKey in slots)
        {
            if (!bySlot.TryGetValue(slotKey, out var candidates) || candidates.Count == 0)
            {
                continue;
            }

            string? pick = null;
            for (var attempt = 0; attempt < 8 && pick == null; attempt++)
            {
                var modId = candidates[rng.Next(candidates.Count)];
                var mod = modules.Resolve(modId);
                if (mod == null)
                {
                    continue;
                }

                if (HullLicenseCatalog.IsFleetProtectionModule(mod))
                {
                    if (hasFleetProtection)
                    {
                        continue;
                    }

                    hasFleetProtection = true;
                }

                pick = modId;
            }

            if (pick == null)
            {
                foreach (var modId in candidates)
                {
                    var mod = modules.Resolve(modId);
                    if (mod == null)
                    {
                        continue;
                    }

                    if (HullLicenseCatalog.IsFleetProtectionModule(mod) && hasFleetProtection)
                    {
                        continue;
                    }

                    if (HullLicenseCatalog.IsFleetProtectionModule(mod))
                    {
                        hasFleetProtection = true;
                    }

                    pick = modId;
                    break;
                }
            }

            if (pick != null)
            {
                fitted[slotKey] = pick;
            }
        }

        return fitted;
    }

    private static Dictionary<string, List<string>> ResolveSlotCandidates(
        HullDef hull,
        ModuleRegistry modules,
        Dictionary<string, Dictionary<string, List<string>>>? cache)
    {
        var hullId = hull.hullId ?? "";
        if (cache != null && cache.TryGetValue(hullId, out var cached))
        {
            return cached;
        }

        var bySlot = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var slots = MemberFittingService.ListOpenSlots(hull);
        foreach (var slotKey in slots)
        {
            bySlot[slotKey] = new List<string>();
        }

        foreach (var mod in modules.All().Values)
        {
            if (mod?.moduleId == null || mod.slotCategory == null)
            {
                continue;
            }

            foreach (var slotKey in slots)
            {
                if (!FittingValidator.ModuleFitsSlot(slotKey, mod, hull))
                {
                    continue;
                }

                bySlot[slotKey].Add(mod.moduleId);
            }
        }

        if (cache != null)
        {
            cache[hullId] = bySlot;
        }

        return bySlot;
    }

    private static void EnsureLegions(GameState state, MechanismTestScenarioDef scenario)
    {
        state.legions.Clear();
        var factions = Math.Max(2, scenario.factionCount > 0 ? scenario.factionCount : 4);
        for (var f = 0; f < factions; f++)
        {
            state.legions.Add(new LegionState
            {
                legionId = f == 0 ? "mt_stress_player" : $"mt_stress_enemy_{f}",
                displayName = f == 0 ? "压力·我方" : $"压力·敌{f}",
                isLocal = f == 0,
                isAiControlled = f != 0,
            });
        }
    }

    private static string EnsurePlayerMember(GameState state, string legionId)
    {
        var mid = "stress_player_01";
        if (state.members.Exists(m => mid.Equals(m.memberId, StringComparison.Ordinal)))
        {
            return mid;
        }

        state.members.Add(new MemberState
        {
            memberId = mid,
            legionId = legionId,
            name = "压力队长",
            equippedHullId = "hull_frigate_shortlegwolf",
            traitIds = { "trait_direct_possess" },
        });
        return mid;
    }

    /// <summary>体积均匀球面采样。</summary>
    public static void SampleInBall(Random rng, float radiusM, out float x, out float y, out float z)
    {
        var u = rng.NextDouble();
        var v = rng.NextDouble();
        var w = rng.NextDouble();
        var theta = 2 * Math.PI * u;
        var phi = Math.Acos(2 * v - 1);
        var r = radiusM * Math.Pow(w, 1.0 / 3.0);
        var sinPhi = Math.Sin(phi);
        x = (float)(r * sinPhi * Math.Cos(theta));
        y = (float)(r * sinPhi * Math.Sin(theta));
        z = (float)(r * Math.Cos(phi));
    }
}
