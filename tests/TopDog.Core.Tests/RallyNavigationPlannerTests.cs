using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class RallyNavigationPlannerTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void SameBattlefield_ShipPosition_PlansNavigateOnly()
    {
        var state = new GameState();
        var bf = new BattlefieldState
        {
            battlefieldId = "bf1",
            systemId = "sys_a",
            eventRegionId = "er_a",
        };
        var unit = new BattlefieldUnit
        {
            unitId = "u1",
            side = UnitSide.FRIENDLY,
            alive = true,
            x = 100f,
        };
        bf.units.Add(unit);
        state.battlefields.Add(bf);

        var anchor = new RallyAnchor
        {
            Kind = RallyAnchorKind.ShipPosition,
            SystemId = "sys_a",
            EventRegionId = "er_a",
            BattlefieldId = "bf1",
            X = 10f,
            Y = 0f,
            Z = 20f,
        };

        var steps = RallyNavigationPlanner.PlanRoute(state, unit, anchor);
        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(RallyStepCodec.TryParseNavigate(steps[0], out var x, out var y, out var z), Is.True);
        Assert.That(x, Is.EqualTo(10f).Within(0.1f));
        Assert.That(z, Is.EqualTo(20f).Within(0.1f));
        _ = y;
    }

    [Test]
    public void PlanBridgePath_FindsShortestRoute()
    {
        var state = new GameState
        {
            map = new LoadedMap(new MapProject
            {
                systems =
                {
                    new SolarSystemDef { solarSystemId = "s0" },
                    new SolarSystemDef { solarSystemId = "s1" },
                    new SolarSystemDef { solarSystemId = "s2" },
                },
                bridges =
                {
                    new JumpBridgeDef { bridgeId = "b01", fromSystemId = "s0", toSystemId = "s1" },
                    new JumpBridgeDef { bridgeId = "b12", fromSystemId = "s1", toSystemId = "s2" },
                },
            }, null),
        };

        var path = RallyNavigationPlanner.PlanBridgePath(state, "s0", "s2");
        Assert.That(path, Is.EqualTo(new[] { "s0", "s1", "s2" }));
    }
}
