using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FieldAuraDamageRouterTests
{
    [Test]
    public void OutsideField_RoutesShieldLayerToHost_UntilDepleted()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var host = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            shieldHp = 100f,
            shieldMax = 100f,
            x = 0f,
            fittedModules = { ["fn_1"] = "mod_shield_fusion_l" },
        };
        var protege = new BattlefieldUnit
        {
            unitId = "p1",
            side = UnitSide.FRIENDLY,
            alive = true,
            shieldHp = 0f,
            shieldFieldHostUnitId = "host",
            x = 100f,
        };
        var attacker = new BattlefieldUnit
        {
            unitId = "atk",
            side = UnitSide.ENEMY,
            alive = true,
            x = 50_000f,
        };
        bf.units.Add(host);
        bf.units.Add(protege);
        bf.units.Add(attacker);

        var ctx = FieldAuraDamageRouter.Route(bf, protege, 500f, attacker, modules);
        Assert.That(ctx.shieldDamage, Is.EqualTo(500f));
        Assert.That(ctx.armorDamage, Is.EqualTo(0f));

        FieldAuraDamageRouter.ApplyRoutedDamage(bf, ctx, modules);
        Assert.That(host.shieldHp, Is.EqualTo(0f).Within(0.01f));
        Assert.That(protege.shieldHp, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void NullAttacker_TreatedAsOutsideField()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var host = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            shieldHp = 80f,
            shieldMax = 80f,
            fittedModules = { ["fn_1"] = "mod_shield_fusion_l" },
        };
        var protege = new BattlefieldUnit
        {
            unitId = "p1",
            side = UnitSide.FRIENDLY,
            alive = true,
            shieldFieldHostUnitId = "host",
        };
        bf.units.Add(host);
        bf.units.Add(protege);

        var ctx = FieldAuraDamageRouter.Route(bf, protege, 40f, null, modules);
        Assert.That(ctx.shieldDamage, Is.EqualTo(40f));
    }
}
