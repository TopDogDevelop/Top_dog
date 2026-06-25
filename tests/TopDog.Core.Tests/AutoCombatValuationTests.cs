using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class AutoCombatValuationTests
{
    [Test]
    public void MemberValue_IncludesHullAndFittedModules()
    {
        var state = new GameState();
        var m = new MemberState { memberId = "m1", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_hybrid_gun_m", 1);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        MemberFittingService.EquipModule(state, m, "atk_0", "mod_hybrid_gun_m", null, hull, modules);

        var value = AutoCombatValuation.MemberValue(state, m, ships, modules);

        Assert.That(value, Is.EqualTo(5_000 + 600));
    }

    [Test]
    public void ChooseAutoResolve_RefreshesStarCoinRosterPower()
    {
        var state = new GameState
        {
            phase = GamePhase.COMBAT_PREP,
            combatPrepStep = CombatPrepStep.CHOOSE_MODE,
            combatQueue =
            {
                new CombatQueueEntry
                {
                    entryId = "e1",
                    label = "巡逻",
                    combatSubtype = CombatSubtype.HARVEST,
                    friendlyMemberIds = { "m1" },
                    enemyRoster =
                    {
                        new CombatRosterLine
                        {
                            displayName = "敌方",
                            hullId = "hull_bc_spear",
                            tonnageClass = "BATTLECRUISER",
                            combatPower = 999f,
                        },
                    },
                },
            },
        };
        var m = new MemberState { memberId = "m1", name = "Alpha", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var ctx = new TopDog.App.Brick.BrickContext(
            state,
            new TopDog.Foundation.Bus.EventBus(),
            new TopDog.Foundation.Clock.SimClock(),
            ships,
            modules,
            TopDog.Content.Traits.TraitCatalog.Empty(),
            new TopDog.Sim.Order.CommandParser());

        CombatPhaseService.ChooseAutoResolve(ctx);

        Assert.That(state.combatPrepStep, Is.EqualTo(CombatPrepStep.CHOOSE_STANCE));
        Assert.That(state.combatQueue[0].friendlyRosterLines, Has.Count.EqualTo(1));
        Assert.That(state.combatQueue[0].friendlyRosterLines[0].combatPower, Is.EqualTo(5_000f));
    }
}
