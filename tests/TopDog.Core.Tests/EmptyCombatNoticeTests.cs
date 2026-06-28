using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Operations;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class EmptyCombatNoticeTests
{
    [Test]
    public void Confirm_AfterEmptyCombatNotice_StartsNextOperationsRoundWithoutThrow()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS, storyRound = 1 };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        EmptyCombatNoticeService.Begin(state);
        Assert.That(state.emptyCombatPending, Is.True);

        Assert.DoesNotThrow(() => EmptyCombatNoticeService.Confirm(state, ships, modules));
        Assert.That(state.emptyCombatPending, Is.False);
        Assert.That(state.phase, Is.EqualTo(GamePhase.OPERATIONS));
        Assert.That(state.storyRound, Is.EqualTo(2));
        Assert.That(state.operationTimeRemainingSec, Is.GreaterThan(0f));
    }

    [Test]
    public void CompleteOperationsPhase_WithEmptyQueue_EntersNoticeThenConfirmAdvancesRound()
    {
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
            operationTimeRemainingSec = 0f,
            storyRound = 1,
        };
        var ctx = new TopDog.App.Brick.BrickContext(
            state,
            new TopDog.Foundation.Bus.EventBus(),
            new TopDog.Foundation.Clock.SimClock(),
            ships,
            modules,
            TopDog.Content.Traits.TraitCatalog.Empty(),
            new TopDog.Sim.Order.CommandParser());

        OperationsPhaseTransition.CompleteOperationsPhase(ctx);
        Assert.That(state.emptyCombatPending, Is.True);

        state.emptyCombatNoticeSec = 0f;
        Assert.DoesNotThrow(() => EmptyCombatNoticeService.Tick(state, 0.05f, ships, modules));
        Assert.That(state.storyRound, Is.EqualTo(2));
    }
}
