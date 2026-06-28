using TopDog.Content.Map;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class JumpBridgeTransitServiceTests
{
    [Test]
    public void TryTransit_MovesUnitToPeerSystemJumpBridgeScene()
    {
        var state = BuildBridgeState();
        var fromBf = state.battlefields[0];
        var ship = new BattlefieldUnit
        {
            unitId = "u1",
            memberId = "m1",
            side = UnitSide.FRIENDLY,
            alive = true,
            x = 100f,
            y = 0f,
        };
        fromBf.units.Add(ship);
        JumpBridgeUnitService.SyncForBattlefield(state, fromBf);
        var gate = fromBf.units.Find(u => u.bridgeId == "br1");
        Assert.That(gate, Is.Not.Null);

        var ok = JumpBridgeTransitService.TryTransit(state, ship, fromBf, gate!, out var err);
        Assert.That(ok, Is.True, err);
        Assert.That(fromBf.units, Does.Not.Contain(ship));
        Assert.That(state.battlefields.Any(b => b.units.Contains(ship)), Is.True);
        var toBf = state.battlefields.First(b => b.units.Contains(ship));
        Assert.That(toBf.systemId, Is.EqualTo("sys-b"));
        Assert.That(toBf.eventRegionId, Is.EqualTo("gate-b"));
        Assert.That(ship.x, Is.EqualTo(0f).Within(0.01f));
        Assert.That(state.members[0].currentSolarSystemId, Is.EqualTo("sys-b"));
    }

    [Test]
    public void OrderEnterBuilding_InvalidTarget_ZeroAck()
    {
        var state = BuildBridgeState();
        var bf = state.battlefields[0];
        bf.units.Add(new BattlefieldUnit { unitId = "u1", side = UnitSide.FRIENDLY, alive = true });
        var msg = FleetOrderService.OrderEnterBuilding(state, bf, "u1", null);
        Assert.That(msg, Is.EqualTo("0 艘执行进入建筑"));
    }

    private static GameState BuildBridgeState()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            activeBattlefieldId = "bf-a",
            map = new LoadedMap(
                new MapProject
                {
                    systems =
                    {
                        new SolarSystemDef
                        {
                            solarSystemId = "sys-a",
                            eventRegions =
                            {
                                new EventRegionDef
                                {
                                    eventRegionId = "gate-a",
                                    kind = EventRegionKinds.JumpBridge,
                                    bridgeId = "br1",
                                    targetSystemId = "sys-b",
                                    anchorAu = new[] { 0f, 0f, 0f },
                                },
                            },
                        },
                        new SolarSystemDef
                        {
                            solarSystemId = "sys-b",
                            eventRegions =
                            {
                                new EventRegionDef
                                {
                                    eventRegionId = "gate-b",
                                    kind = EventRegionKinds.JumpBridge,
                                    bridgeId = "br1",
                                    targetSystemId = "sys-a",
                                    anchorAu = new[] { 0f, 0f, 0f },
                                },
                            },
                        },
                    },
                    bridges =
                    {
                        new JumpBridgeDef
                        {
                            bridgeId = "br1",
                            fromSystemId = "sys-a",
                            toSystemId = "sys-b",
                        },
                    },
                },
                securityBands: null),
            members = { new MemberState { memberId = "m1", currentSolarSystemId = "sys-a" } },
        };
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "gate-a",
            anchorAu = new[] { 0f, 0f, 0f },
        });
        JumpBridgeUnitService.SyncForBattlefield(state, state.battlefields[0]);
        return state;
    }
}
