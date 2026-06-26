using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MemberAutoEquipHullTests
{
    [Test]
    public void TryFromPersonalStock_EquipsRandomHull_BeforeAutofit()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP };
        var m = new MemberState { memberId = "m1", equippedHullId = null };
        state.members.Add(m);
        var stock = MemberAssetService.PersonalStock(state, m);
        stock["hull_bc_spear"] = 2;
        stock["hull_dread_ironcoffin"] = 1;
        stock["mod_hybrid_gun_m"] = 3;
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        Assert.That(MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, new Random(3)), Is.True);
        Assert.That(m.equippedHullId, Is.Not.Null);
        Assert.That(ships.FindHull(m.equippedHullId!), Is.Not.Null);

        var fitMsg = MemberDispatchAutoFitService.TryFillEmptySlots(
            state, m, ships, modules, new Random(3), allowOutsideOperations: true, clearExistingFittings: false);
        Assert.That(MemberFittingService.Fittings(state, m).Count, Is.GreaterThan(0));
        Assert.That(fitMsg, Does.Contain("填装"));
    }

    [Test]
    public void TryFromPersonalStock_SkipsWhenAlreadyEquipped()
    {
        var state = new GameState();
        var m = new MemberState { memberId = "m1", equippedHullId = "hull_bc_spear" };
        MemberAssetService.PersonalStock(state, m)["hull_dread_ironcoffin"] = 1;
        var ships = ShipRegistry.LoadDefault();

        Assert.That(MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, new Random(1)), Is.False);
        Assert.That(m.equippedHullId, Is.EqualTo("hull_bc_spear"));
    }
}
