using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

using TopDog.Core.Tests;

namespace TopDog.Tests;

public sealed class TacticalWarpServiceTests
{
    [Test]
    public void TryBeginWarp_SetsTransitEtaFromDistanceAndSpeed()
    {
        var state = NewDualBfState();
        var from = state.battlefields[0];
        var to = state.battlefields[1];
        var unit = FriendlyShip("u1");
        from.units.Add(unit);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);
        TacticalWarpTestHelper.PrepareInitiate(state, from, to, unit);

        var err = TacticalWarpService.TryBeginWarp(state, unit, from, to, new HullDef { warpSpeedAups = 4f }, 500_000f);
        Assert.That(err, Is.Null);
        Assert.That(unit.inTacticalWarp, Is.True);
        Assert.That(unit.warpPhase, Is.EqualTo(TacticalWarpPhase.ApproachProxy));
        Assert.That(unit.warpTargetBfId, Is.EqualTo("bf-b"));
        Assert.That(unit.warpEtaSec, Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void TryBeginWarp_RejectsCrossSystem()
    {
        var state = NewDualBfState();
        var from = state.battlefields[0];
        var to = new BattlefieldState
        {
            battlefieldId = "bf-other",
            systemId = "sys-other",
            eventRegionId = "reg-x",
            anchorAu = new[] { 1f, 0f, 0f },
        };
        state.battlefields.Add(to);
        var unit = FriendlyShip("u1");
        from.units.Add(unit);

        var err = TacticalWarpService.TryBeginWarp(state, unit, from, to, null, 500_000f);
        Assert.That(err, Does.Contain("跳桥"));
    }

    [Test]
    public void TryBeginWarp_RejectsBeyondMaxDistanceAu()
    {
        var state = NewDualBfState();
        var from = state.battlefields[0];
        var to = new BattlefieldState
        {
            battlefieldId = "bf-far",
            systemId = "sys1",
            eventRegionId = "reg-far",
            anchorAu = new[] { TacticalWarpService.MaxWarpDistanceAu + 1f, 0f, 0f },
        };
        state.battlefields.Add(to);
        state.map!.Project.systems[0].eventRegions.Add(new EventRegionDef
        {
            eventRegionId = "reg-far",
            kind = EventRegionKinds.Planet,
            anchorAu = to.anchorAu,
            radiusKm = 500,
        });
        var unit = FriendlyShip("u1");
        from.units.Add(unit);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);

        var err = TacticalWarpService.TryBeginWarp(state, unit, from, to, null, 500_000f);
        Assert.That(err, Does.Contain("目标过远"));
        Assert.That(err, Does.Contain("1000"));
    }

    [Test]
    public void TryBeginWarp_AllowsAtMaxDistanceAu()
    {
        var state = NewDualBfState();
        var from = state.battlefields[0];
        var to = new BattlefieldState
        {
            battlefieldId = "bf-edge",
            systemId = "sys1",
            eventRegionId = "reg-edge",
            anchorAu = new[] { TacticalWarpService.MaxWarpDistanceAu, 0f, 0f },
        };
        state.battlefields.Add(to);
        state.map!.Project.systems[0].eventRegions.Add(new EventRegionDef
        {
            eventRegionId = "reg-edge",
            kind = EventRegionKinds.Planet,
            anchorAu = to.anchorAu,
            radiusKm = 500,
        });
        var unit = FriendlyShip("u1");
        from.units.Add(unit);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);
        TacticalWarpTestHelper.PrepareInitiate(state, from, to, unit);

        var err = TacticalWarpService.TryBeginWarp(state, unit, from, to, new HullDef { warpSpeedAups = 5f }, 500_000f);
        Assert.That(err, Is.Null);
        Assert.That(unit.warpEtaSec, Is.EqualTo(TacticalWarpService.MaxWarpDistanceAu / 5f).Within(0.01f));
    }

    [Test]
    public void TryOrderIntraSceneWarp_RejectsUnder150km()
    {
        var state = NewDualBfState();
        var bf = state.battlefields[0];
        var unit = FriendlyShip("u1");
        bf.units.Add(unit);

        var err = TacticalWarpService.TryOrderIntraSceneWarp(state, unit, bf, 100_000f, 0f, 0f, null);
        Assert.That(err, Does.Contain("150"));
    }

    [Test]
    public void TryOrderIntraSceneWarp_CompletesOnSceneWithoutTransit()
    {
        var state = NewDualBfState();
        var bf = state.battlefields[0];
        var unit = FriendlyShip("u1");
        bf.units.Add(unit);
        ShipMotionIntegrator.SnapHeadingToward(unit, 200_000f, 0f, 0f);
        unit.vx = 80f;

        var err = TacticalWarpService.TryOrderIntraSceneWarp(state, unit, bf, 200_000f, 0f, 0f, null);
        Assert.That(err, Is.Null);
        Assert.That(unit.warpPhase, Is.EqualTo(TacticalWarpPhase.ApproachProxy));
        Assert.That(unit.warpFromBfId, Is.EqualTo("bf-a"));
        Assert.That(unit.warpTargetBfId, Is.EqualTo("bf-a"));

        for (var i = 0; i < 500 && unit.warpPhase != TacticalWarpPhase.None; i++)
        {
            TacticalWarpService.Tick(state, bf, 0.05f);
        }

        Assert.That(unit.warpPhase, Is.EqualTo(TacticalWarpPhase.None));
        Assert.That(state.tacticalWarpInTransit, Is.Empty);
        Assert.That(unit.x, Is.EqualTo(200_000f).Within(500f));
    }

    [Test]
    public void FullPseudoWarp_MovesUnitToTargetBattlefield()
    {
        var state = NewDualBfState();
        var from = state.battlefields[0];
        var to = state.battlefields[1];
        var unit = FriendlyShip("u1");
        from.units.Add(unit);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);
        BattlefieldSceneProxyService.SyncForBattlefield(state, to);
        TacticalWarpTestHelper.PrepareInitiate(state, from, to, unit);
        Assert.That(TacticalWarpService.TryBeginWarp(state, unit, from, to, null, 300_000f), Is.Null);

        for (var i = 0; i < 300 && from.units.Contains(unit); i++)
        {
            TacticalWarpService.Tick(state, from, 0.05f);
        }

        Assert.That(state.tacticalWarpInTransit, Has.Count.EqualTo(1));
        state.tacticalWarpInTransit[0].remainingSec = 0f;
        TacticalWarpService.TickInTransit(state, 0.05f);

        Assert.That(to.units.Any(u => u.unitId == "u1"), Is.True);
        unit = to.units.First(u => u.unitId == "u1");

        for (var i = 0; i < 400; i++)
        {
            TacticalWarpService.Tick(state, to, 0.05f);
            if (unit.warpPhase == TacticalWarpPhase.None)
            {
                break;
            }
        }

        Assert.That(unit.inTacticalWarp, Is.False);
        Assert.That(unit.warpPhase, Is.EqualTo(TacticalWarpPhase.None));
        var dist = MathF.Sqrt(unit.x * unit.x + unit.y * unit.y);
        Assert.That(dist, Is.EqualTo(300_000f).Within(2000f));
    }

    private static GameState NewDualBfState()
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
                            solarSystemId = "sys1",
                            eventRegions =
                            {
                                new EventRegionDef
                                {
                                    eventRegionId = "reg-a",
                                    kind = EventRegionKinds.Planet,
                                    anchorAu = new[] { 0f, 0f, 0f },
                                    radiusKm = 500,
                                },
                                new EventRegionDef
                                {
                                    eventRegionId = "reg-b",
                                    kind = EventRegionKinds.OreBelt,
                                    anchorAu = new[] { 1f, 0f, 0f },
                                    radiusKm = 500,
                                },
                            },
                        },
                    },
                },
                securityBands: null),
        };
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys1",
            eventRegionId = "reg-a",
            anchorAu = new[] { 0f, 0f, 0f },
        });
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-b",
            systemId = "sys1",
            eventRegionId = "reg-b",
            anchorAu = new[] { 1f, 0f, 0f },
        });
        return state;
    }

    private static BattlefieldUnit FriendlyShip(string id) => new()
    {
        unitId = id,
        side = UnitSide.FRIENDLY,
        memberId = "m1",
        alive = true,
        arrivalAtSec = 0f,
        structureHp = 100f,
        structureMax = 100f,
        maxSpeedMps = 100f,
    };
}
