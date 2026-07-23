using TopDog.Content.Modules;
using TopDog.Content.Ships;
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

    [Test]
    public void OrderApproach_DefaultMidKm_HoldsThrottleOffInDeadband()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();

        var state = new GameState
        {
            activeBattlefieldId = "bf1",
            combatRealtimeActive = true,
        };
        var bf = NewBf();
        state.battlefields.Add(bf);

        var ship = Unit("f1", UnitSide.FRIENDLY);
        ship.x = 0f;
        ship.y = 0f;
        ship.z = 0f;
        ship.maxSpeedMps = 200f;
        ship.structureHp = 100f;
        ship.structureMax = 100f;
        // 静止目标：建筑敌方，避免敌方 AI 驱动目标移动
        var marker = Unit("mark1", UnitSide.ENEMY);
        marker.x = 200_000f;
        marker.y = 0f;
        marker.z = 0f;
        marker.structureHp = 100f;
        marker.structureMax = 100f;
        marker.isBuilding = true;
        bf.units.Add(ship);
        bf.units.Add(marker);

        FleetOrderService.OrderApproach(state, bf, "mark1", new[] { "f1" }, rangeKm: TacticalRangeScale.MidKm);
        Assert.That(ship.commandMaintainDistM, Is.EqualTo(200_000f).Within(1f));

        var sawHold = false;
        for (var i = 0; i < 8; i++)
        {
            ship.approachHeadingTimerSec = 0f;
            BattlefieldSystem.Tick(state, modules, ships, 1.1f);
            if (!ship.throttleOn && ship.aiOrder == UnitAiOrder.APPROACH)
            {
                sawHold = true;
                break;
            }
        }

        Assert.That(sawHold, Is.True, "expected approach hold (throttle off) at MidKm deadband");
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
