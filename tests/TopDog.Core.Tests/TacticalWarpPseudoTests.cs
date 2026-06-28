using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TacticalWarpPseudoTests
{
    [Test]
    public void TryBeginWarp_RejectsHighSpeed()
    {
        var state = NewState();
        var from = state.battlefields[0];
        var to = state.battlefields[1];
        var u = FriendlyShip("f1");
        u.vx = 200f;
        from.units.Add(u);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);

        var err = TacticalWarpService.TryBeginWarp(state, u, from, to, null, 500_000f);
        Assert.That(err, Does.Contain("减速"));
    }

    [Test]
    public void ApproachProxy_EntersTransitAfterArrive()
    {
        var state = NewState();
        var from = state.battlefields[0];
        var to = state.battlefields[1];
        var u = FriendlyShip("f1");
        u.x = 0f;
        u.y = 0f;
        from.units.Add(u);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);
        Assert.That(TacticalWarpService.TryBeginWarp(state, u, from, to, null, 250_000f), Is.Null);

        for (var i = 0; i < 200 && from.units.Contains(u); i++)
        {
            TacticalWarpService.Tick(state, from, 0.05f);
        }

        Assert.That(from.units, Does.Not.Contain(u));
        Assert.That(state.tacticalWarpInTransit.Count, Is.EqualTo(1));
        Assert.That(state.tacticalWarpInTransit[0].landingDistM, Is.EqualTo(250_000f).Within(1f));
    }

    [Test]
    public void InTransit_SpawnsEntryOnDestination()
    {
        var state = NewState();
        var from = state.battlefields[0];
        var to = state.battlefields[1];
        var u = FriendlyShip("f1");
        from.units.Add(u);
        BattlefieldSceneProxyService.SyncForBattlefield(state, from);
        BattlefieldSceneProxyService.SyncForBattlefield(state, to);
        Assert.That(TacticalWarpService.TryBeginWarp(state, u, from, to, null, 120_000f), Is.Null);
        u.warpPhaseTimerSec = TacticalWarpService.ApproachTimeoutSec;
        TacticalWarpService.Tick(state, from, 0.05f);

        Assert.That(state.tacticalWarpInTransit.Count, Is.EqualTo(1));
        state.tacticalWarpInTransit[0].remainingSec = 0.01f;
        TacticalWarpService.TickInTransit(state, 0.02f);

        Assert.That(to.units, Does.Contain(u));
        Assert.That(u.warpPhase, Is.EqualTo(TacticalWarpPhase.EntryBurst));
        var landingDist = MathF.Sqrt(u.warpLandingX * u.warpLandingX + u.warpLandingY * u.warpLandingY);
        Assert.That(landingDist, Is.EqualTo(120_000f).Within(500f));
    }

    private static GameState NewState()
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
                                    eventRegionId = "reg-a",
                                    kind = EventRegionKinds.Planet,
                                    anchorAu = new[] { 0f, 0f, 0f },
                                    radiusKm = 500,
                                },
                                new EventRegionDef
                                {
                                    eventRegionId = "reg-b",
                                    kind = EventRegionKinds.OreBelt,
                                    name = "矿带",
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
            systemId = "sys-a",
            eventRegionId = "reg-a",
            anchorAu = new[] { 0f, 0f, 0f },
        });
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-b",
            systemId = "sys-a",
            eventRegionId = "reg-b",
            subLocation = "矿带",
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
        maxSpeedMps = 120f,
    };
}
