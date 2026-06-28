using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FleetOrderAuthorityTests
{
    [Test]
    public void OrderFocus_OnSceneProxy_AcknowledgesButUnitsStillFocus()
    {
        var bf = NewBf();
        var ship = Unit("f1", UnitSide.FRIENDLY);
        var proxy = new BattlefieldUnit
        {
            unitId = "proxy-1",
            side = UnitSide.FRIENDLY,
            isSceneProxy = true,
            tonnageClass = BattlefieldSceneProxyService.TonnageClass,
            alive = true,
        };
        bf.units.Add(ship);
        bf.units.Add(proxy);

        var msg = FleetOrderService.OrderFocus(new GameState(), bf, "proxy-1", null);
        Assert.That(msg, Does.StartWith("已下令"));
        Assert.That(ship.aiOrder, Is.EqualTo(UnitAiOrder.FOCUS));
        Assert.That(ship.targetUnitId, Is.EqualTo("proxy-1"));
    }

    [Test]
    public void OrderApproach_WithoutTarget_ReturnsZeroAck()
    {
        var bf = NewBf();
        bf.units.Add(Unit("f1", UnitSide.FRIENDLY));
        var msg = FleetOrderService.OrderApproach(new GameState(), bf, null, null);
        Assert.That(msg, Is.EqualTo("0 艘执行接近"));
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
