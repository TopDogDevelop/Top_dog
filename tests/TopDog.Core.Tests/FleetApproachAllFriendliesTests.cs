using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FleetApproachAllFriendliesTests
{
    [Test]
    public void OrderApproach_WithPossessorAndNoSelection_OrdersAllFriendlies()
    {
        var state = new GameState { possessingMemberId = "m1" };
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var a = Friendly("f1", "m1");
        var b = Friendly("f2", "m2");
        var target = new BattlefieldUnit
        {
            unitId = "b1",
            side = UnitSide.FRIENDLY,
            alive = true,
            isBuilding = true,
            x = 5000f,
        };
        bf.units.Add(a);
        bf.units.Add(b);
        bf.units.Add(target);

        var msg = FleetOrderService.OrderApproach(state, bf, "b1", null);
        Assert.That(msg, Does.Contain("2"));
        Assert.That(a.aiOrder, Is.EqualTo(UnitAiOrder.APPROACH));
        Assert.That(b.aiOrder, Is.EqualTo(UnitAiOrder.APPROACH));
        Assert.That(a.approachTargetUnitId, Is.EqualTo("b1"));
    }

    private static BattlefieldUnit Friendly(string unitId, string memberId) => new()
    {
        unitId = unitId,
        memberId = memberId,
        side = UnitSide.FRIENDLY,
        alive = true,
        attackRangeM = 1000f,
    };
}
