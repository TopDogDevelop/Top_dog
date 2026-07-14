using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_SCENARIOS.md §spawn
 * 本文件: MechanismTestSpawnService.cs — 矿带战场 20km 对阵 bootstrap
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismTestSpawnService
{
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

        var bf = new BattlefieldState
        {
            battlefieldId = "mt_bf_" + belt.eventRegionId,
            systemId = sys.solarSystemId,
            eventRegionId = belt.eventRegionId,
            anchorAu = belt.anchorAu,
            subLocation = belt.name,
        };
        state.battlefields.Add(bf);

        var halfSep = scenario.spawnSeparationM * 0.5f;
        var legionIndex = 0;
        foreach (var legion in state.legions)
        {
            if (legion.legionId == null)
            {
                continue;
            }

            var side = legion.isLocal ? UnitSide.FRIENDLY : UnitSide.ENEMY;
            var offsetX = legionIndex == 0 ? -halfSep : halfSep;
            legionIndex++;
            SpawnLegion(state, bf, legion.legionId, side, offsetX, ships, modules);
        }

        state.activeBattlefieldId = bf.battlefieldId;
        SkirmishDisplayNames.SyncSkirmishLabels(state);
        SeedInitialVisionFocus(state);
        BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        MechanismTestPhaseRules.EnsureRealtimeCombat(state);
        MatchMemberBaselineService.EnsureSnapshot(state);
    }

    private static void SpawnLegion(
        GameState state,
        BattlefieldState bf,
        string legionId,
        UnitSide side,
        float offsetX,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        foreach (var member in state.members)
        {
            if (!legionId.Equals(member.legionId, StringComparison.Ordinal))
            {
                continue;
            }

            var hullId = member.equippedHullId;
            var hull = string.IsNullOrWhiteSpace(hullId) ? null : ships.FindHull(hullId);
            if (hull?.hullId == null)
            {
                continue;
            }

            var u = new BattlefieldUnit
            {
                unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
                displayName = string.IsNullOrWhiteSpace(member.name) ? member.memberId ?? "?" : member.name,
                hullId = hull.hullId,
                memberId = member.memberId,
                legionId = legionId,
                side = side,
                arrivalAtSec = 0f,
                x = offsetX,
                y = 0f,
                z = 0f,
                facingRad = side == UnitSide.FRIENDLY ? 0f : (float)Math.PI,
            };
            u.fittedModules = new Dictionary<string, string>(
                MemberFittingService.Fittings(state, member));
            TraitGrantedModuleService.ApplyForMember(member, u, modules);
            ModuleRuntime.ApplyToUnit(u, hull, modules);
            ModuleActivationService.EnableFieldModulesByDefault(u, modules);
            LaunchTubeStateService.InitTubeStates(u, modules);
            bf.units.Add(u);
        }
    }

    private static void SeedInitialVisionFocus(GameState state)
    {
        foreach (var legion in state.legions)
        {
            if (!legion.isLocal)
            {
                continue;
            }

            foreach (var bf in state.battlefields)
            {
                foreach (var u in bf.units)
                {
                    if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || u.memberId == null)
                    {
                        continue;
                    }

                    state.tacticalCameraUnitId = u.unitId;
                    return;
                }
            }
        }
    }
}
