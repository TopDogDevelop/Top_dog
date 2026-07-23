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

    [Test]
    public void ArmorField_HostArmorAbsorbsFullHit_BeforeProtegeStructure()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var host = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 8_000f,
            armorMax = 40_000f,
            structureHp = 10_000f,
            x = 0f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        var protege = new BattlefieldUnit
        {
            unitId = "p1",
            side = UnitSide.FRIENDLY,
            alive = true,
            shieldHp = 0f,
            armorHp = 0f,
            structureHp = 20f,
            structureMax = 20f,
            armorFieldHostUnitId = "host",
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

        var ctx = FieldAuraDamageRouter.Route(bf, protege, 36_000f, attacker, modules);
        Assert.That(ctx.armorDamage, Is.EqualTo(36_000f).Within(0.01f));
        Assert.That(ctx.structureDamage, Is.EqualTo(0f).Within(0.01f));

        FieldAuraDamageRouter.ApplyRoutedDamage(bf, ctx, modules);
        Assert.That(host.armorHp, Is.EqualTo(0f).Within(0.01f));
        Assert.That(protege.structureHp, Is.EqualTo(20f - 28_000f).Within(0.01f));
        Assert.That(protege.IsDestroyed(), Is.True);
    }

    [Test]
    public void WhitewolfShield_AtSpawnSeparation_IsOutside_UsesHullRadiusMult()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var host = new BattlefieldUnit
        {
            unitId = "host",
            hullId = "hull_cruiser_whitewolf_guard",
            side = UnitSide.FRIENDLY,
            alive = true,
            shieldHp = 1000f,
            shieldMax = 1000f,
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

        var hull = ships.FindHull(host.hullId);
        var radius = FieldAuraService.ResolveFieldRadiusM(
            host, modules.Resolve("mod_shield_fusion_l")!, hull);
        Assert.That(radius, Is.EqualTo(25_000f).Within(0.1f));

        var ctx = FieldAuraDamageRouter.Route(bf, protege, 200f, attacker, modules, ships);
        Assert.That(ctx.shieldDamage, Is.EqualTo(200f).Within(0.01f),
            "50km spawn must be outside whitewolf 25km shell so shield host absorbs");
        Assert.That(ctx.armorDamage, Is.EqualTo(0f));

        FieldAuraDamageRouter.ApplyRoutedDamage(bf, ctx, modules, ships);
        Assert.That(host.shieldHp, Is.EqualTo(800f).Within(0.01f));
    }

    [Test]
    public void ArmorField_PartialHit_DoesNotTouchProtegeWhileHostHasArmor()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var host = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 8_000f,
            armorMax = 40_000f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        var protege = new BattlefieldUnit
        {
            unitId = "p1",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 0f,
            structureHp = 500f,
            armorFieldHostUnitId = "host",
        };
        bf.units.Add(host);
        bf.units.Add(protege);

        var ctx = FieldAuraDamageRouter.Route(bf, protege, 3_000f, null, modules);
        Assert.That(ctx.armorDamage, Is.EqualTo(3_000f).Within(0.01f));
        Assert.That(ctx.structureDamage, Is.EqualTo(0f));

        FieldAuraDamageRouter.ApplyRoutedDamage(bf, ctx, modules);
        Assert.That(host.armorHp, Is.EqualTo(5_000f).Within(0.01f));
        Assert.That(protege.structureHp, Is.EqualTo(500f).Within(0.01f));
        Assert.That(protege.IsDestroyed(), Is.False);
    }
}
