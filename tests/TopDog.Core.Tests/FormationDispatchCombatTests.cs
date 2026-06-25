using TopDog.App;
using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Clock;
using TopDog.Sim.Combat;
using TopDog.Sim.Formation;
using TopDog.Sim.Member;
using TopDog.Sim.Order;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FormationDispatchCombatTests
{
    private static GameState OpsState()
    {
        return new GameState
        {
            phase = GamePhase.OPERATIONS,
            currentSolarSystemId = "sys_a",
            members =
            {
                new MemberState { memberId = "m1", name = "Alpha" },
                new MemberState { memberId = "m2", name = "Beta" },
            },
        };
    }

    [Test]
    public void CreateFormation_AssignsMembers()
    {
        var state = OpsState();
        var echo = FormationService.Create(state, new[] { "m1", "m2" });
        Assert.That(echo, Does.Contain("已组建"));
        Assert.That(state.formations, Has.Count.EqualTo(1));
        Assert.That(state.members[0].formationId, Is.Not.Null);
        Assert.That(state.members[1].formationId, Is.EqualTo(state.members[0].formationId));
    }

    [Test]
    public void DissolveFormation_ClearsMembers()
    {
        var state = OpsState();
        FormationService.Create(state, new[] { "m1", "m2" });
        var echo = FormationService.DissolveForMember(state, "m1");
        Assert.That(echo, Does.Contain("解散").Or.Contain("移出"));
        Assert.That(state.members[0].formationId, Is.Null);
        Assert.That(state.members[1].formationId, Is.Null);
    }

    [Test]
    public void DispatchToSystem_SetsTaskAndLocation()
    {
        var state = OpsState();
        var echo = MemberDispatchService.DispatchToSystem(
            state, "m1", MemberDispatchService.TaskMining, "sys_b");
        Assert.That(echo, Does.Contain("采矿"));
        Assert.That(state.members[0].assignedTask, Is.EqualTo(MemberDispatchService.TaskMining));
        Assert.That(state.members[0].currentSolarSystemId, Is.EqualTo("sys_b"));
    }

    [Test]
    public void ChooseAutoResolve_EntersStanceStep()
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
                },
            },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var ctx = new BrickContext(
            state,
            new EventBus(),
            new SimClock(),
            ships,
            modules,
            TraitCatalog.Empty(),
            new CommandParser());
        var echo = CombatPhaseService.ChooseAutoResolve(ctx);
        Assert.That(echo, Does.Contain("接战"));
        Assert.That(state.combatPrepStep, Is.EqualTo(CombatPrepStep.CHOOSE_STANCE));
        Assert.That(state.aiAgreedAutoResolve, Is.True);
    }
}
