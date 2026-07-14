using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MemberEquipHullClearsFittingsTests
{
    [Test]
    public void EquipHull_ClearsPreviousShipFittings_ToPersonal()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "m1", name = "测试员" };
        state.members.Add(m);
        var stock = MemberAssetService.PersonalStock(state, m);
        stock["hull_bc_spear"] = 1;
        stock["hull_cruiser_greywolf_guard"] = 1;
        stock["mod_hybrid_gun_m"] = 1;
        stock["mod_armor_link_s"] = 1;

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var spear = ships.FindHull("hull_bc_spear")!;
        var greywolf = ships.FindHull("hull_cruiser_greywolf_guard")!;

        Assert.That(MemberAssetService.EquipHull(state, m, "hull_bc_spear", MemberAssetService.SourcePersonal, ships), Does.Contain("装备"));
        Assert.That(
            MemberFittingService.EquipModule(state, m, "atk_0", "mod_hybrid_gun_m", null, spear, modules),
            Does.Contain("装配"));
        Assert.That(MemberFittingService.Fittings(state, m).Count, Is.EqualTo(1));

        Assert.That(
            MemberAssetService.EquipHull(state, m, "hull_cruiser_greywolf_guard", MemberAssetService.SourcePersonal, ships),
            Does.Contain("装备"));
        Assert.That(m.equippedHullId, Is.EqualTo("hull_cruiser_greywolf_guard"));
        Assert.That(MemberFittingService.Fittings(state, m), Is.Empty);
        Assert.That(stock.GetValueOrDefault("mod_hybrid_gun_m", 0), Is.EqualTo(1));

        Assert.That(
            MemberFittingService.EquipModule(state, m, "fn_0", "mod_armor_link_s", null, greywolf, modules),
            Does.Contain("装配"));
        Assert.That(MemberFittingService.Fittings(state, m)["fn_0"], Is.EqualTo("mod_armor_link_s"));
    }
}
