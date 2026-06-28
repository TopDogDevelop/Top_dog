using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class OrbitEntryResolverTests
{
    [Test]
    public void ComputeEntryPoint_PicksBowSideDeterministically()
    {
        var ship = new BattlefieldUnit { x = 0f, y = 0f, z = 0f, facingRad = 0f };
        var target = new BattlefieldUnit { x = 10000f, y = 0f, z = 0f };
        OrbitEntryResolver.ComputeEntryPoint(ship, target, 5000f, out var ex, out var ey, out _);
        Assert.That(ex, Is.Not.EqualTo(0f).Within(1f));
        Assert.That(ey, Is.Not.EqualTo(0f).Within(1f));
    }
}

[TestFixture]
public sealed class ApproachAwayMaintainTests
{
    [Test]
    public void OrderApproach_WithRangeKm_SetsMaintainDist()
    {
        var bf = NewBf();
        var ship = Unit("f1", UnitSide.FRIENDLY);
        var enemy = Unit("e1", UnitSide.ENEMY);
        bf.units.Add(ship);
        bf.units.Add(enemy);

        FleetOrderService.OrderApproach(new GameState(), bf, "e1", null, rangeKm: 50f);
        Assert.That(ship.commandMaintainDistM, Is.EqualTo(50_000f).Within(1f));
        Assert.That(ship.aiOrder, Is.EqualTo(UnitAiOrder.APPROACH));
    }

    private static BattlefieldState NewBf() => new() { battlefieldId = "bf1" };

    private static BattlefieldUnit Unit(string id, UnitSide side) => new()
    {
        unitId = id,
        side = side,
        alive = true,
        attackRangeM = 1000f,
    };
}
