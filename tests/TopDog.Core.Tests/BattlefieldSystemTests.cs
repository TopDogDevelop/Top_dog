using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BattlefieldSystemTests
{
    [Test]
    public void TickResolvesWhenOneSideEliminated()
    {
        var state = new GameState { combatRealtimeActive = true, autoFireEnabled = true };
        var friendly = Member("f1", "hull_bc_spear", "trait_direct_possess");
        state.members.Add(friendly);

        var entry = new CombatQueueEntry
        {
            entryId = "e1",
            friendlyMemberIds = { "f1" },
            enemyRoster =
            {
                new CombatRosterLine
                {
                    displayName = "AI",
                    hullId = "hull_frigate_scout",
                    tonnageClass = "FRIGATE",
                    combatPower = 10f,
                },
            },
            resolveMode = CombatResolveMode.REALTIME,
        };

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var bf = BattlefieldSpawner.Spawn(state, entry, ships, modules, new Random(1));
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        for (var i = 0; i < 800 && !bf.finished; i++)
        {
            BattlefieldSystem.Tick(state, 0.5f);
        }

        Assert.That(bf.finished, Is.True);
        Assert.That(bf.winnerSide, Is.AnyOf(UnitSide.FRIENDLY, UnitSide.ENEMY));
    }

    [Test]
    public void AutoFireRespectsToggle()
    {
        var state = new GameState { combatRealtimeActive = true, autoFireEnabled = false };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 10f };
        var friendly = new BattlefieldUnit
        {
            unitId = "u1",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            structureHp = 100f,
            structureMax = 100f,
        };
        var enemy = new BattlefieldUnit
        {
            unitId = "u2",
            side = UnitSide.ENEMY,
            alive = true,
            arrivalAtSec = 0f,
            x = 100f,
            structureHp = 100f,
            structureMax = 100f,
        };
        bf.units.Add(friendly);
        bf.units.Add(enemy);
        AutoFireTargetingService.Tick(bf, state, friendly);
        Assert.That(friendly.targetUnitId, Is.Null);
        state.autoFireEnabled = true;
        AutoFireTargetingService.Tick(bf, state, friendly);
        Assert.That(friendly.targetUnitId, Is.EqualTo("u2"));
    }

    private static MemberState Member(string id, string hull, string trait)
    {
        return new MemberState
        {
            memberId = id,
            name = id,
            equippedHullId = hull,
            traitIds = { trait },
        };
    }
}
