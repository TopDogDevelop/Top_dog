using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Operations;
using TopDog.Sim.Ship;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FittingValidatorTests
{
    [Test]
    public void ModuleFitsSlot_MediumInMediumAttack()
    {
        var hull = new HullDef { attackSlots = 1, defaultSlotSize = ModuleSize.Medium };
        var mod = new ModuleDef
        {
            moduleId = "mod_hybrid_gun_m",
            slotCategory = "ATTACK",
            moduleSize = ModuleSize.Medium,
        };
        Assert.That(FittingValidator.ModuleFitsSlot("atk_0", mod, hull), Is.True);
    }

    [Test]
    public void EquipModule_FromPersonalStock()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "1000000101", name = "Test", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_m", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        var echo = MemberFittingService.EquipModule(
            state, m, "atk_0", "mod_hybrid_gun_m", null, hull, modules);
        Assert.That(echo, Does.Contain("装配"));
        Assert.That(MemberFittingService.Fittings(state, m)["atk_0"], Is.EqualTo("mod_hybrid_gun_m"));
    }

    [Test]
    public void ListEquippableModules_ExcludesStarCoin()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "1000000101", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty(CurrencyIds.StarCoin, 100);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        var list = MemberFittingService.ListEquippableModules(state, m, "atk_0", hull, modules);
        Assert.That(list.Exists(x => x.moduleId == CurrencyIds.StarCoin), Is.False);
    }

    [Test]
    public void PassiveSlot_RejectsAttackModule()
    {
        var hull = new HullDef { passiveSlots = 1, defaultSlotSize = ModuleSize.Medium };
        var mod = new ModuleDef
        {
            moduleId = "mod_hybrid_gun_m",
            slotCategory = "ATTACK",
            moduleSize = ModuleSize.Medium,
        };
        Assert.That(FittingValidator.ModuleFitsSlot("pas_0", mod, hull), Is.False);
    }

    [Test]
    public void PassiveSlot_AcceptsGainPlugin()
    {
        var hull = new HullDef { passiveSlots = 1, defaultSlotSize = ModuleSize.Medium };
        var mod = new ModuleDef
        {
            moduleId = "plug_speed_m",
            slotCategory = "PASSIVE",
            moduleKind = "stat_plugin",
            moduleSize = ModuleSize.Medium,
        };
        Assert.That(FittingValidator.ModuleFitsSlot("pas_0", mod, hull), Is.True);
    }

    [Test]
    public void PassiveSlot_GainPluginIgnoresSize()
    {
        var hull = new HullDef { passiveSlots = 1, defaultSlotSize = ModuleSize.Small };
        var mod = new ModuleDef
        {
            moduleId = "plug_speed_10",
            slotCategory = "PASSIVE",
            moduleKind = "stat_plugin",
            moduleSize = ModuleSize.Large,
        };
        Assert.That(FittingValidator.ModuleFitsSlot("pas_0", mod, hull), Is.True);
    }

    [Test]
    public void LaunchTube_AcceptsStrikeWingFromStubInventory()
    {
        var hull = new HullDef { launchTubeSlots = 1, defaultSlotSize = ModuleSize.Large };
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "1000000101", equippedHullId = "hull_carrier_crispy" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_strike_wing_a_l", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        hull = ships.FindHull("hull_carrier_crispy") ?? hull;
        var list = MemberFittingService.ListEquippableModules(state, m, "tube_0", hull, modules);
        Assert.That(list.Exists(x => x.moduleId == "mod_strike_wing_a_l"), Is.True);
    }

    [Test]
    public void LaunchTube_AcceptsLegacyStrikeWingId()
    {
        var hull = new HullDef { launchTubeSlots = 1, defaultSlotSize = ModuleSize.Large };
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "1000000101", equippedHullId = "hull_carrier_crispy" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("strike_wing_a", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        hull = ships.FindHull("hull_carrier_crispy") ?? hull;
        var list = MemberFittingService.ListEquippableModules(state, m, "tube_0", hull, modules);
        Assert.That(list.Count, Is.GreaterThan(0));
    }

    [Test]
    public void CollectorHull_UsesPerCategorySlotSizes()
    {
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_carrier_stellar_collector");
        Assert.That(hull, Is.Not.Null);
        Assert.That(FittingValidator.SlotSize(hull, "atk_0"), Is.EqualTo(ModuleSize.Small));
        Assert.That(FittingValidator.SlotSize(hull, "fn_0"), Is.EqualTo(ModuleSize.Medium));
        Assert.That(FittingValidator.SlotSize(hull, "def_0"), Is.EqualTo(ModuleSize.Large));
    }
}
