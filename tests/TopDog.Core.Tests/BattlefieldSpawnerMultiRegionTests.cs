using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BattlefieldSpawnerMultiRegionTests
{
    [Test]
    public void HarvestSpawnsOneBattlefieldPerEventRegion()
    {
        var state = new GameState
        {
            members =
            {
                Member("m1", "region_a"),
                Member("m2", "region_b"),
                Member("m3", "region_b"),
            },
        };
        var entry = new CombatQueueEntry
        {
            entryId = "e1",
            combatSubtype = CombatSubtype.HARVEST,
            battlefieldSystemId = "sys1",
            friendlyMemberIds = { "m1", "m2", "m3" },
            enemyRoster =
            {
                new CombatRosterLine { displayName = "守军", hullId = "hull_bc_spear", tonnageClass = "BATTLECRUISER" },
            },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var spawned = BattlefieldSpawner.SpawnAll(state, entry, ships, modules, new Random(1));
        Assert.That(spawned, Has.Count.EqualTo(2));
        Assert.That(spawned.Any(b => b.eventRegionId == "region_a" && b.units.Count(u => u.side == UnitSide.FRIENDLY) == 1));
        Assert.That(spawned.Any(b => b.eventRegionId == "region_b" && b.units.Count(u => u.side == UnitSide.FRIENDLY) == 2));
    }

    private static MemberState Member(string id, string region) => new()
    {
        memberId = id,
        name = id,
        equippedHullId = "hull_bc_spear",
        opsDeployEventRegionId = region,
        opsDeploySystemId = "sys1",
    };
}
