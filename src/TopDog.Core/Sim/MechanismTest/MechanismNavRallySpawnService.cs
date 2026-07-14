using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_SCENARIOS.md · mt_nav_rally
 * 本文件: MechanismNavRallySpawnService.cs — 多场景散布菠萝 spawn
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismNavRallySpawnService
{
    public static void BootstrapBattlefields(
        GameState state,
        MechanismTestScenarioDef scenario,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (state.map?.Project == null)
        {
            return;
        }

        state.battlefields.Clear();
        var slots = CollectSpawnSlots(state);
        if (slots.Count == 0)
        {
            return;
        }

        Shuffle(slots, rng);
        var playerLegion = state.legions.Find(l => l.isLocal);
        if (playerLegion?.legionId == null)
        {
            return;
        }

        var members = state.members
            .Where(m => playerLegion.legionId.Equals(m.legionId, StringComparison.Ordinal))
            .ToList();
        var spawnCount = Math.Min(members.Count, slots.Count);
        BattlefieldState? firstBf = null;

        for (var i = 0; i < spawnCount; i++)
        {
            var member = members[i];
            var slot = slots[i];
            var bf = TacticalSceneBattlefieldService.EnsureSceneBattlefield(
                state,
                slot.systemId,
                slot.eventRegionId);
            firstBf ??= bf;
            SpawnMember(state, bf, member, ships, modules, rng);
            BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        }

        state.activeBattlefieldId = firstBf?.battlefieldId;
        SkirmishDisplayNames.SyncSkirmishLabels(state);
        SeedInitialVisionFocus(state);
        MechanismTestPhaseRules.EnsureRealtimeCombat(state);
        MatchMemberBaselineService.EnsureSnapshot(state);
    }

    private static void SpawnMember(
        GameState state,
        BattlefieldState bf,
        MemberState member,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var hullId = member.equippedHullId;
        var hull = string.IsNullOrWhiteSpace(hullId) ? null : ships.FindHull(hullId);
        if (hull?.hullId == null)
        {
            return;
        }

        var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
        var dist = 500f + (float)rng.NextDouble() * 1500f;
        var u = new BattlefieldUnit
        {
            unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
            displayName = string.IsNullOrWhiteSpace(member.name) ? member.memberId ?? "?" : member.name,
            hullId = hull.hullId,
            memberId = member.memberId,
            legionId = member.legionId,
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            x = MathF.Cos(angle) * dist,
            y = 0f,
            z = MathF.Sin(angle) * dist,
            facingRad = angle + MathF.PI,
        };
        u.fittedModules = new Dictionary<string, string>(
            MemberFittingService.Fittings(state, member));
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        ModuleActivationService.EnableFieldModulesByDefault(u, modules);
        LaunchTubeStateService.InitTubeStates(u, modules);
        bf.units.Add(u);
        member.currentSolarSystemId = bf.systemId;
    }

    private static List<(string systemId, string eventRegionId)> CollectSpawnSlots(GameState state)
    {
        var list = new List<(string, string)>();
        foreach (var sys in state.map!.Project.systems)
        {
            if (sys.solarSystemId == null || sys.eventRegions == null)
            {
                continue;
            }

            foreach (var er in sys.eventRegions)
            {
                if (er.eventRegionId == null || !IsNavRallySpawnRegion(er))
                {
                    continue;
                }

                list.Add((sys.solarSystemId, er.eventRegionId));
            }
        }

        return list;
    }

    private static bool IsNavRallySpawnRegion(EventRegionDef er)
    {
        if (EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal))
        {
            return false;
        }

        return EventRegionKinds.Star.Equals(er.kind, StringComparison.Ordinal)
            || EventRegionKinds.OreBelt.Equals(er.kind, StringComparison.Ordinal)
            || EventRegionKinds.PirateRally.Equals(er.kind, StringComparison.Ordinal)
            || EventRegionKinds.Planet.Equals(er.kind, StringComparison.Ordinal);
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void SeedInitialVisionFocus(GameState state)
    {
        foreach (var bf in state.battlefields)
        {
            foreach (var u in bf.units)
            {
                if (u.side == UnitSide.FRIENDLY && !u.IsDestroyed() && u.memberId != null)
                {
                    state.tacticalCameraUnitId = u.unitId;
                    return;
                }
            }
        }
    }
}
