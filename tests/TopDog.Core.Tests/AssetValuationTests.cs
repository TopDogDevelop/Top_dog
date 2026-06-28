using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class AssetValuationTests
{
    [Test]
    public void StarCoin_ValueIsOne()
    {
        Assert.That(AssetValuation.ItemStarCoinValue(CurrencyIds.StarCoin, null, null), Is.EqualTo(1));
    }

    [Test]
    public void Resource_Inorganic_ValueIsOne()
    {
        Assert.That(AssetValuation.ItemStarCoinValue("res_inorganic", null, null), Is.EqualTo(1));
    }

    [Test]
    public void Hull_DefaultByTonnage()
    {
        var carrier = new HullDef { tonnageClass = "CARRIER" };
        var bc = new HullDef { tonnageClass = "BATTLECRUISER" };
        var dread = new HullDef { tonnageClass = "DREADNOUGHT" };
        Assert.That(AssetValuation.HullStarCoinValue(carrier), Is.EqualTo(50_000));
        Assert.That(AssetValuation.HullStarCoinValue(dread), Is.EqualTo(50_000));
        Assert.That(AssetValuation.HullStarCoinValue(bc), Is.EqualTo(5_000));
    }

    [Test]
    public void Module_DefaultBySize()
    {
        Assert.That(AssetValuation.ModuleStarCoinValue(new ModuleDef { moduleSize = ModuleSize.Small }), Is.EqualTo(60));
        Assert.That(AssetValuation.ModuleStarCoinValue(new ModuleDef { moduleSize = ModuleSize.Medium }), Is.EqualTo(600));
        Assert.That(AssetValuation.ModuleStarCoinValue(new ModuleDef { moduleSize = ModuleSize.Large }), Is.EqualTo(6_000));
        Assert.That(AssetValuation.ModuleStarCoinValue(new ModuleDef { moduleSize = ModuleSize.ExtraLarge }), Is.EqualTo(60_000));
        Assert.That(AssetValuation.ModuleStarCoinValue(new ModuleDef { moduleSize = ModuleSize.Youliang }), Is.EqualTo(600_000));
    }

    [Test]
    public void TransferLegionToPersonal_SupportsQuantity()
    {
        var state = new GameState();
        var m = new MemberState { memberId = "m1", name = "A" };
        state.members.Add(m);
        state.legionStock["mod_hybrid_gun_m"] = 5;
        MemberAssetService.TransferLegionToPersonal(state, m, "mod_hybrid_gun_m", 3);
        Assert.That(state.legionStock["mod_hybrid_gun_m"], Is.EqualTo(2));
        Assert.That(MemberAssetService.PersonalQty(state, m, "mod_hybrid_gun_m"), Is.EqualTo(3));
    }

    [Test]
    public void Module_BilingualLabel_UsesChineseFirst()
    {
        var mod = ModuleCatalog.Stub("mod_energy_disrupt_s");
        Assert.That(ModuleCatalog.BilingualLabel(mod), Does.Contain("能量扰断器"));
        Assert.That(ModuleCatalog.BilingualLabel(mod), Does.Contain("Energy Disruptor"));
        Assert.That(ModuleCatalog.BilingualLabel(mod), Does.Not.Contain("mod_energy_disrupt_s /"));
    }
}

[TestFixture]
public sealed class MemberDispatchAutoFitTests
{
    [Test]
    public void Dispatch_AutoFillsFromPersonal_RespectingHullValue()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "m1", name = "Miner", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_m", 2);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_strike_wing_a_l", 1);

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var rng = new Random(42);

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules, rng);
        var fit = MemberFittingService.Fittings(state, m);
        Assert.That(fit.Count, Is.GreaterThan(0));
        foreach (var e in fit)
        {
            var mod = modules.Resolve(e.Value);
            Assert.That(AssetValuation.ModuleStarCoinValue(mod), Is.LessThanOrEqualTo(5_000));
        }
    }

    [Test]
    public void Dispatch_SkipsModulesAboveHullValue()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "m1", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_shield_resist_l", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules, new Random(1));
        Assert.That(MemberFittingService.Fittings(state, m), Is.Empty);
        Assert.That(AssetValuation.ModuleStarCoinValue(modules.Resolve("mod_shield_resist_l")), Is.EqualTo(6_000));
        Assert.That(AssetValuation.HullStarCoinValue(ships.FindHull("hull_bc_spear")), Is.EqualTo(5_000));
    }

    [Test]
    public void Dispatch_ClearsExistingFittingsBeforeFill()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "m1", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_m", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        MemberFittingService.EquipModule(
            state, m, "atk_0", "mod_hybrid_gun_m", null, hull, modules);
        Assert.That(MemberFittingService.Fittings(state, m).Count, Is.EqualTo(1));

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules, new Random(1));
        Assert.That(MemberFittingService.Fittings(state, m).Count, Is.EqualTo(1));
        Assert.That(MemberAssetService.PersonalQty(state, m, "mod_hybrid_gun_m"), Is.EqualTo(0));
    }

    [Test]
    public void Dispatch_LuxuryTrait_PrefersHigherValueModules()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState
        {
            memberId = "m1",
            equippedHullId = "hull_bc_spear",
            traitIds = { MemberTraitIds.EquipLuxury },
        };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_m", 1);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_ore_mining_beam_s", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules);
        var fit = MemberFittingService.Fittings(state, m);
        fit.TryGetValue("atk_0", out var atk0);
        Assert.That(atk0, Is.EqualTo("mod_hybrid_gun_m"));
    }

    [Test]
    public void Dispatch_ThriftTrait_PrefersLowerValueModules()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState
        {
            memberId = "m1",
            equippedHullId = "hull_bc_spear",
            traitIds = { MemberTraitIds.EquipThrift },
        };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_m", 1);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_ore_mining_beam_s", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules);
        var fit = MemberFittingService.Fittings(state, m);
        fit.TryGetValue("atk_0", out var atk0);
        Assert.That(atk0, Is.EqualTo("mod_ore_mining_beam_s"));
    }

    [Test]
    public void Dispatch_LuxuryTrait_IgnoresValuationCap()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState
        {
            memberId = "m1",
            equippedHullId = "hull_bc_spear",
            traitIds = { MemberTraitIds.EquipLuxury },
        };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_l", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules);
        var fit = MemberFittingService.Fittings(state, m);
        fit.TryGetValue("atk_0", out var atk0);
        Assert.That(atk0, Is.EqualTo("mod_hybrid_gun_l"));
        Assert.That(AssetValuation.ModuleStarCoinValue(modules.Resolve("mod_hybrid_gun_l")), Is.EqualTo(6_000));
        Assert.That(AssetValuation.HullStarCoinValue(ships.FindHull("hull_bc_spear")), Is.EqualTo(5_000));
    }
}
