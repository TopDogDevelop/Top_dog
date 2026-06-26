using TopDog.Content.Modules;
using TopDog.Content.Ships;using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class PlaytestFixBatchTests
{
    [Test]
    public void SalvoProfile_NoAttackModules_ZeroDamage()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        var unit = new BattlefieldUnit { fittedModules = new Dictionary<string, string>() };
        SalvoProfileService.ApplyToUnit(unit, hull, modules);
        Assert.That(unit.salvoRoundDmg, Is.EqualTo(0f));
    }

    [Test]
    public void SalvoProfile_WithHybridGun_UsesModuleDamageOnly()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        var unit = new BattlefieldUnit
        {
            fittedModules = { ["attack_m1"] = "mod_hybrid_gun_m" },
        };
        SalvoProfileService.ApplyToUnit(unit, hull, modules);
        Assert.That(unit.salvoRoundDmg, Is.EqualTo(1000f));
        Assert.That(unit.fireCycleSec, Is.EqualTo(10f).Within(0.01f));
    }

    [Test]
    public void OrderApproach_SnapsHeadingImmediatelyAndThrottlesFull()
    {
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var friendly = new BattlefieldUnit
        {
            unitId = "f1",
            side = UnitSide.FRIENDLY,
            alive = true,
            attackRangeM = 1000f,
            maxSpeedMps = 2000f,
            accelMps2 = 800f,
            x = 0f,
            y = 0f,
            facingRad = MathF.PI,
        };
        var enemy = new BattlefieldUnit
        {
            unitId = "e1",
            side = UnitSide.ENEMY,
            alive = true,
            x = 2000f,
            y = 0f,
        };
        bf.units.Add(friendly);
        bf.units.Add(enemy);

        FleetOrderService.OrderApproach(new GameState(), bf, "e1", new[] { "f1" });
        var state = new GameState { combatRealtimeActive = true, activeBattlefieldId = bf.battlefieldId };
        state.battlefields.Add(bf);
        BattlefieldSystem.Tick(state, 0.01f);

        Assert.That(friendly.throttleOn, Is.True);
        Assert.That(friendly.facingRad, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void OrderApproach_CompletesToStop_WhenInRange()
    {
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var friendly = new BattlefieldUnit
        {
            unitId = "f1",
            side = UnitSide.FRIENDLY,
            alive = true,
            attackRangeM = 1000f,
            maxSpeedMps = 2000f,
            accelMps2 = 800f,
            x = 0f,
            y = 0f,
        };
        var enemy = new BattlefieldUnit
        {
            unitId = "e1",
            side = UnitSide.ENEMY,
            alive = true,
            x = 400f,
            y = 0f,
        };
        bf.units.Add(friendly);
        bf.units.Add(enemy);

        FleetOrderService.OrderApproach(new GameState(), bf, "e1", new[] { "f1" });
        var state = new GameState { combatRealtimeActive = true, activeBattlefieldId = bf.battlefieldId };
        state.battlefields.Add(bf);

        for (var i = 0; i < 500; i++)
        {
            BattlefieldSystem.Tick(state, 0.05f);
            if (friendly.aiOrder == UnitAiOrder.STOP)
            {
                break;
            }
        }

        Assert.That(friendly.aiOrder, Is.EqualTo(UnitAiOrder.STOP));
        Assert.That(friendly.approachTargetUnitId, Is.Null);
    }

    [Test]
    public void SalvoProfile_DreadWithFourLGuns_MatchesFirstPack()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_dread_ironcoffin");
        Assert.That(hull, Is.Not.Null);
        var unit = new BattlefieldUnit
        {
            fittedModules =
            {
                ["attack_l1"] = "mod_hybrid_gun_l",
                ["attack_l2"] = "mod_hybrid_gun_l",
                ["attack_l3"] = "mod_hybrid_gun_l",
                ["attack_l4"] = "mod_hybrid_gun_l",
            },
        };
        SalvoProfileService.ApplyToUnit(unit, hull, modules);
        Assert.That(unit.salvoRoundDmg, Is.EqualTo(12000f));
        Assert.That(unit.fireCycleSec, Is.EqualTo(10f).Within(0.01f));
        Assert.That(unit.damagePerSec, Is.EqualTo(1200f).Within(0.01f));
    }

    [Test]
    public void SalvoProfile_XlGunStub_MatchesFirstPack()
    {
        var modules = ModuleRegistry.LoadDefault();
        var stub = ModuleCatalog.Resolve(modules, "mod_hybrid_gun_xl");
        Assert.That(stub!.damagePerTick, Is.EqualTo(6000f));
        Assert.That(stub.fireCycleSec, Is.EqualTo(15f).Within(0.01f));
    }
}
