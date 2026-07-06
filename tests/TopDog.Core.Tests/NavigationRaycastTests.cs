using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class NavigationRaycastTests
{
    [Test]
    public void RayPlaneIntersect_HitsFocusPlane()
    {
        var ok = TacticalRaycastMath.TryRayPlaneIntersect(
            0f, 0f, -100f,
            0f, 0f, 1f,
            0f, 0f, 0f,
            0f, 0f, 1f,
            out var x, out var y, out var z);
        Assert.That(ok, Is.True);
        Assert.That(z, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void ScreenToFocusPlane_CenterHitsOrigin()
    {
        var ok = TacticalRaycastMath.TryScreenToFocusPlaneOffset(
            400f,
            300f,
            800f,
            600f,
            60f,
            0f,
            (float)(Math.PI * 0.5),
            40_000f,
            out var dx,
            out var dy,
            out var dz);
        Assert.That(ok, Is.True);
        Assert.That(dx, Is.EqualTo(0f).Within(1f));
        Assert.That(dy, Is.EqualTo(0f).Within(1f));
        Assert.That(dz, Is.EqualTo(0f).Within(1f));
    }

    [Test]
    public void ScreenToFocusPlane_OffCenterHitsAwayFromOrigin()
    {
        var ok = TacticalRaycastMath.TryScreenToFocusPlaneOffset(
            600f,
            200f,
            800f,
            600f,
            60f,
            0f,
            (float)(Math.PI * 0.5),
            40_000f,
            out var dx,
            out var dy,
            out var dz);
        Assert.That(ok, Is.True);
        var dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        Assert.That(dist, Is.GreaterThan(100f));
    }

    [Test]
    public void WorldViewOffset_RoundTrip()
    {
        TacticalRaycastMath.WorldOffsetToViewOffset(1200f, -800f, 300f, 0.2f, 1.4f, out var vx, out var vy, out var vz);
        TacticalRaycastMath.ViewOffsetToWorldOffset(vx, vy, vz, 0.2f, 1.4f, out var dx, out var dy, out var dz);
        Assert.That(dx, Is.EqualTo(1200f).Within(0.5f));
        Assert.That(dy, Is.EqualTo(-800f).Within(0.5f));
        Assert.That(dz, Is.EqualTo(300f).Within(0.5f));
    }

    [Test]
    public void OrderNavigateToPoint_AssignsNavigateOrder()
    {
        var state = new GameState();
        state.battlefields.Add(new BattlefieldState { battlefieldId = "bf1" });
        var bf = state.battlefields[0];
        var ship = new BattlefieldUnit
        {
            unitId = "s1",
            side = UnitSide.FRIENDLY,
            alive = true,
        };
        bf.units.Add(ship);

        var msg = FleetOrderService.OrderNavigateToPoint(state, bf, 1000f, 2000f, 0f, null);
        Assert.That(msg, Does.Contain("1"));
        Assert.That(ship.aiOrder, Is.EqualTo(UnitAiOrder.NAVIGATE));
        Assert.That(state.tacticalNavVisible, Is.True);
    }

    [Test]
    public void OrderApproach_HidesTacticalNavMarker()
    {
        var state = new GameState();
        state.battlefields.Add(new BattlefieldState { battlefieldId = "bf1" });
        var bf = state.battlefields[0];
        var ship = new BattlefieldUnit
        {
            unitId = "s1",
            side = UnitSide.FRIENDLY,
            alive = true,
        };
        var enemy = new BattlefieldUnit
        {
            unitId = "e1",
            side = UnitSide.ENEMY,
            alive = true,
        };
        bf.units.Add(ship);
        bf.units.Add(enemy);

        FleetOrderService.OrderNavigateToPoint(state, bf, 1000f, 2000f, 0f, null);
        Assert.That(state.tacticalNavVisible, Is.True);

        FleetOrderService.OrderApproach(state, bf, "e1", null);
        Assert.That(state.tacticalNavVisible, Is.False);
    }

    [Test]
    public void OrderFocus_KeepsTacticalNavMarker()
    {
        var state = new GameState();
        state.battlefields.Add(new BattlefieldState { battlefieldId = "bf1" });
        var bf = state.battlefields[0];
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "s1",
            side = UnitSide.FRIENDLY,
            alive = true,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "e1",
            side = UnitSide.ENEMY,
            alive = true,
        });

        FleetOrderService.OrderNavigateToPoint(state, bf, 1000f, 2000f, 0f, null);
        FleetOrderService.OrderFocus(state, bf, "e1", null);
        Assert.That(state.tacticalNavVisible, Is.True);
    }
}
