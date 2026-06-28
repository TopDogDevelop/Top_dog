using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Clock;
using TopDog.Sim.Combat;
using TopDog.Sim.Order;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatRoundContinueTests
{
    [Test]
    public void ContinueAfterResult_AdvancesQueue_AndClearsBattlefields()
    {
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var state = new GameState
        {
            phase = GamePhase.COMBAT,
            combatAwaitingContinue = true,
            combatPrepStep = CombatPrepStep.SHOW_RESULT,
            combatQueueIndex = 0,
            combatRealtimeActive = true,
            activeBattlefieldId = "bf-1",
        };
        state.combatQueue.Add(new CombatQueueEntry { label = "第一场", queueOrdinal = 1, queueTotal = 2 });
        state.combatQueue.Add(new CombatQueueEntry { label = "第二场", queueOrdinal = 2, queueTotal = 2 });
        state.battlefields.Add(new BattlefieldState { battlefieldId = "bf-1", finished = true });

        var ctx = new BrickContext(
            state,
            new EventBus(),
            new SimClock(),
            ships,
            modules,
            TraitCatalog.Empty(),
            new CommandParser());
        var msg = CombatPhaseService.ContinueAfterResult(ctx);

        Assert.That(state.combatQueueIndex, Is.EqualTo(1));
        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT_PREP));
        Assert.That(state.combatPrepStep, Is.EqualTo(CombatPrepStep.CHOOSE_MODE));
        Assert.That(state.combatAwaitingContinue, Is.False);
        Assert.That(state.battlefields, Is.Empty);
        Assert.That(state.activeBattlefieldId, Is.Null);
        Assert.That(msg, Does.Contain("下一项"));
    }
}
