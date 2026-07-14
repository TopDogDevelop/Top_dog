using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FieldAuraServiceTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();
    [Test]
    public void ResolveFieldRadius_GreyWolfSmallArmorLink_BonusKm()
    {
        var hull = new HullDef
        {
            hullId = "hull_cruiser_greywolf_guard",
            hullArmorLinkSmallRadiusBonusKm = 7f,
        };
        var mod = new ModuleDef
        {
            moduleId = "mod_armor_link_s",
            moduleKind = "armor_link_field",
            moduleSize = "SMALL",
            fieldRadiusKm = 5f,
        };

        var holder = new BattlefieldUnit { hullId = hull.hullId };

        var radiusM = FieldAuraService.ResolveFieldRadiusM(holder, mod, hull);
        Assert.That(radiusM, Is.EqualTo(12_000f));
    }

    [Test]
    public void DistanceM_Uses3d()
    {
        var a = new BattlefieldUnit { x = 0f, y = 0f, z = 0f };
        var b = new BattlefieldUnit { x = 3f, y = 4f, z = 0f };
        Assert.That(FieldAuraService.DistanceM(a, b), Is.EqualTo(5f).Within(0.01f));
    }

    [Test]
    public void GreyWolfShieldBonus_OnlyWhenArmorFieldActive()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_cruiser_greywolf_guard");
        Assert.That(hull, Is.Not.Null);

        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 5f };
        var greywolf = new BattlefieldUnit
        {
            unitId = "gw",
            hullId = "hull_cruiser_greywolf_guard",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            shieldHp = 500f,
            shieldMax = 500f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
            fieldAuraEnabledAtSec = 1f,
        };
        var protege = new BattlefieldUnit
        {
            unitId = "p1",
            hullId = "hull_frigate_shortlegwolf",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 100f,
            armorFieldHostUnitId = "gw",
        };
        bf.units.Add(greywolf);
        bf.units.Add(protege);

        Assert.That(greywolf.shieldMax, Is.EqualTo(500f));

        var state = new GameState();
        FieldAuraService.Tick(state, bf, modules, ships, 1.1f);
        Assert.That(greywolf.fieldAuraDominant, Is.True);
        Assert.That(greywolf.shieldMax, Is.EqualTo(750f));
        Assert.That(greywolf.shieldHp, Is.EqualTo(750f));
    }

    [Test]
    public void ArmorFieldEnter_HolderArmorEqualsSelfPlusProteges()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var greyHull = ships.FindHull("hull_cruiser_greywolf_guard");
        var wolfHull = ships.FindHull("hull_frigate_shortlegwolf");
        Assert.That(greyHull, Is.Not.Null);
        Assert.That(wolfHull, Is.Not.Null);

        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        var holder = new BattlefieldUnit
        {
            unitId = "gw",
            hullId = greyHull!.hullId,
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            armorHp = greyHull.armorHp,
            armorMax = greyHull.armorHp,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
            fieldAuraEnabledAtSec = 1f,
            fieldAuraArmorDominant = true,
        };
        bf.units.Add(holder);

        var protegeCount = 3;
        for (var i = 0; i < protegeCount; i++)
        {
            var protege = new BattlefieldUnit
            {
                unitId = "p" + i,
                hullId = wolfHull!.hullId,
                side = UnitSide.FRIENDLY,
                alive = true,
                arrivalAtSec = 0f,
                armorHp = wolfHull.armorHp,
                armorMax = wolfHull.armorHp,
                x = 100f * i,
            };
            bf.units.Add(protege);
        }

        var state = new GameState();
        FieldAuraService.Tick(state, bf, modules, ships, 1.1f);

        var expectedArmor = greyHull.armorHp + protegeCount * wolfHull.armorHp;
        Assert.That(holder.armorMax, Is.EqualTo(expectedArmor).Within(0.01f));
        Assert.That(holder.armorHp, Is.EqualTo(expectedArmor).Within(0.01f));
        foreach (var protege in bf.units)
        {
            if (protege.unitId == null || protege.unitId == "gw")
            {
                continue;
            }

            Assert.That(protege.armorFieldHostUnitId, Is.EqualTo("gw"));
            Assert.That(protege.armorHp, Is.EqualTo(0f).Within(0.01f));
        }
    }

    [Test]
    public void Whitewolf_ShieldFusionRadius_IsHalf()
    {
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_cruiser_whitewolf_guard");
        var mod = modules.Resolve("mod_shield_fusion_l");
        Assert.That(hull, Is.Not.Null);
        Assert.That(mod, Is.Not.Null);

        var holder = new BattlefieldUnit { hullId = hull!.hullId };
        var radiusM = FieldAuraService.ResolveFieldRadiusM(holder, mod!, hull);
        Assert.That(radiusM, Is.EqualTo(7500f));
    }

    [Test]
    public void LargeTonnageProtege_BindOnlyPolicy()
    {
        var ships = ShipRegistry.LoadDefault();
        var holder = new BattlefieldUnit
        {
            unitId = "cruiser",
            hullId = "hull_frigate_pineapple",
            tonnageClass = "CRUISER",
            side = UnitSide.FRIENDLY,
            alive = true,
        };
        var dread = new BattlefieldUnit
        {
            unitId = "dread",
            hullId = "hull_dread_ironcoffin",
            tonnageClass = "DREADNOUGHT",
            side = UnitSide.FRIENDLY,
            alive = true,
        };
        var holderHull = ships.FindHull(holder.hullId);
        Assert.That(
            FieldAuraService.EligibleForBinding(dread, holder, holderHull, "shield_fusion_field"),
            Is.True);
        Assert.That(
            FieldAuraService.EligibleForShieldFusion(dread, holder, holderHull),
            Is.False);

        var wolfHull = ships.FindHull("hull_cruiser_whitewolf_guard");
        var wolf = new BattlefieldUnit
        {
            unitId = "wolf",
            hullId = "hull_cruiser_whitewolf_guard",
            tonnageClass = "CRUISER",
            side = UnitSide.FRIENDLY,
            alive = true,
        };
        Assert.That(
            FieldAuraService.EligibleForShieldFusion(dread, wolf, wolfHull),
            Is.False);
    }

    [Test]
    public void ArmorFieldLeave_RestoresProtegeArmor()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 10f };
        var holder = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            armorHp = 5000f,
            armorMax = 5000f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
            fieldAuraEnabledAtSec = 1f,
            fieldAuraArmorDominant = true,
            x = 0f,
        };
        var protege = new BattlefieldUnit
        {
            unitId = "p1",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            armorHp = 800f,
            armorMax = 800f,
            armorFieldHostUnitId = "host",
            x = 100f,
        };
        bf.units.Add(holder);
        bf.units.Add(protege);
        FieldAuraService.ApplyEnter(protege, holder, bf, "armor_link_field");

        protege.x = 50_000f;
        var state = new GameState();
        FieldAuraService.Tick(state, bf, modules, ships, 1.1f);

        Assert.That(protege.armorFieldHostUnitId, Is.Null);
        Assert.That(protege.armorHp, Is.EqualTo(800f).Within(0.01f));
    }
}
