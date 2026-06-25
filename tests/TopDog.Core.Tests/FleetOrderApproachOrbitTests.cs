using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FleetOrderApproachOrbitTests
{
    [Test]
    public void OrderApproach_SteersTowardEnemyTarget()
    {
        var bf = NewBf();
        var friendly = Unit("f1", UnitSide.FRIENDLY);
        var enemy = Unit("e1", UnitSide.ENEMY);
        bf.units.Add(friendly);
        bf.units.Add(enemy);

        var msg = FleetOrderService.OrderApproach(new GameState(), bf, "e1", null);
        Assert.That(msg, Does.Contain("接近"));
        Assert.That(friendly.aiOrder, Is.EqualTo(UnitAiOrder.APPROACH));
        Assert.That(friendly.approachTargetUnitId, Is.EqualTo("e1"));
    }

    [Test]
    public void OrderOrbit_AllFriendliesWhenNoSelection()
    {
        var bf = NewBf();
        var a = Unit("f1", UnitSide.FRIENDLY);
        var b = Unit("f2", UnitSide.FRIENDLY);
        var enemy = Unit("e1", UnitSide.ENEMY);
        bf.units.Add(a);
        bf.units.Add(b);
        bf.units.Add(enemy);

        var msg = FleetOrderService.OrderOrbit(new GameState(), bf, "e1", null);
        Assert.That(msg, Does.Contain("2"));
        Assert.That(a.aiOrder, Is.EqualTo(UnitAiOrder.ORBIT));
        Assert.That(b.aiOrder, Is.EqualTo(UnitAiOrder.ORBIT));
        Assert.That(a.orbitTargetUnitId, Is.EqualTo("e1"));
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
