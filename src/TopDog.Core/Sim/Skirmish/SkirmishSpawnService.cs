using TopDog.Content.Balance;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public static class SkirmishSpawnService
{
    public static void BootstrapBattlefields(
        GameState state,
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
        foreach (var er in sys.eventRegions)
        {
            if (er.eventRegionId == null || EventRegionKinds.IsStar(er.kind))
            {
                continue;
            }

            var bf = new BattlefieldState
            {
                battlefieldId = "skirmish_bf_" + er.eventRegionId,
                systemId = sys.solarSystemId,
                eventRegionId = er.eventRegionId,
                anchorAu = er.anchorAu,
                subLocation = er.name,
            };

            foreach (var building in state.buildings)
            {
                if (building.eventRegionId != null
                    && building.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal))
                {
                    SkirmishBuildingRules.SpawnBuildingUnit(state, bf, building);
                }
            }

            state.battlefields.Add(bf);
        }

        foreach (var legion in state.legions)
        {
            if (legion.legionId == null)
            {
                continue;
            }

            var fortressRegion = FindLegionFortressRegion(state, legion.legionId);
            var bf = state.battlefields.Find(b =>
                fortressRegion != null
                && fortressRegion.Equals(b.eventRegionId, StringComparison.Ordinal));
            if (bf == null)
            {
                continue;
            }

            SpawnLegionRoster(state, bf, legion, ships, modules, rng);
        }

        if (state.battlefields.Count > 0)
        {
            state.activeBattlefieldId = state.battlefields[0].battlefieldId;
        }

        state.phase = GamePhase.COMBAT;
        state.combatRealtimeActive = true;
        state.combatPrepStep = CombatPrepStep.CHOOSE_STANCE;
        MatchMemberBaselineService.EnsureSnapshot(state);
    }

    public static void SpawnLegionRoster(
        GameState state,
        BattlefieldState bf,
        LegionState legion,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var balance = SkirmishBalanceConfig.LoadDefault();
        var radius = balance.spawnRadiusM;
        var side = legion.isLocal ? UnitSide.FRIENDLY : UnitSide.ENEMY;

        foreach (var member in state.members)
        {
            if (member.legionId != null && !member.legionId.Equals(legion.legionId, StringComparison.Ordinal))
            {
                continue;
            }

            if (member.equippedHullId == null)
            {
                continue;
            }

            var hull = ships.FindHull(member.equippedHullId);
            if (hull?.hullId == null)
            {
                continue;
            }

            var angle = (float)(rng.NextDouble() * Math.PI * 2);
            var dist = (float)(rng.NextDouble() * radius);
            var u = new BattlefieldUnit
            {
                unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
                displayName = string.IsNullOrWhiteSpace(member.name) ? member.memberId ?? "?" : member.name,
                hullId = member.equippedHullId,
                memberId = member.memberId,
                legionId = legion.legionId,
                side = side,
                arrivalAtSec = 0f,
                x = MathF.Cos(angle) * dist,
                y = MathF.Sin(angle) * dist,
                facingRad = side == UnitSide.FRIENDLY ? 0f : (float)Math.PI,
            };
            u.fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, member));
            ModuleRuntime.ApplyToUnit(u, hull, modules);
            bf.units.Add(u);
        }
    }

    private static string? FindLegionFortressRegion(GameState state, string legionId)
    {
        foreach (var building in state.buildings)
        {
            if (building.legionId != null
                && building.legionId.Equals(legionId, StringComparison.Ordinal)
                && string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
            {
                return building.eventRegionId;
            }
        }

        return null;
    }
}
