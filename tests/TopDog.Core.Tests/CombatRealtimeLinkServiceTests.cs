using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatRealtimeLinkServiceTests
{
    [Test]
    public void Begin_FreezesSimulationForTwoSeconds_ThenBroadcastsSuccess()
    {
        var state = new GameState { combatRealtimeActive = true };
        CombatRealtimeLinkService.Begin(state);

        Assert.That(state.alertLog[^1], Is.EqualTo("正在建立战场连接，请等候"));
        Assert.That(CombatRealtimeLinkService.IsHandshakeFrozen(state), Is.True);
        Assert.That(CombatRealtimeLinkService.TickHandshake(state, 1f), Is.False);
        Assert.That(CombatRealtimeLinkService.IsHandshakeFrozen(state), Is.True);

        Assert.That(CombatRealtimeLinkService.TickHandshake(state, 1.01f), Is.True);
        Assert.That(state.alertLog[^1], Is.EqualTo("实时战场连接成功！"));
        Assert.That(CombatRealtimeLinkService.IsHandshakeFrozen(state), Is.False);
        Assert.That(state.combatRealtimeLinkHandshakeActive, Is.False);
    }

    [Test]
    public void WithoutBegin_SimulationRunsImmediately()
    {
        var state = new GameState { combatRealtimeActive = true };

        Assert.That(CombatRealtimeLinkService.TickHandshake(state, 0.1f), Is.True);
        Assert.That(CombatRealtimeLinkService.IsHandshakeFrozen(state), Is.False);
    }

    [Test]
    public void EnsureRealtimeCombat_BeginsHandshakeOnce()
    {
        var state = new GameState();
        SkirmishPhaseRules.EnsureRealtimeCombat(state);

        Assert.That(state.combatRealtimeActive, Is.True);
        Assert.That(CombatRealtimeLinkService.IsHandshakeFrozen(state), Is.True);

        SkirmishPhaseRules.EnsureRealtimeCombat(state);
        Assert.That(CombatRealtimeLinkService.IsHandshakeFrozen(state), Is.True);
        Assert.That(state.combatRealtimeLinkDelaySec, Is.EqualTo(CombatRealtimeLinkService.HandshakeDelaySec).Within(0.001f));
    }

    [Test]
    public void Reset_ClearsHandshake()
    {
        var state = new GameState { combatRealtimeActive = true };
        CombatRealtimeLinkService.Begin(state);
        CombatRealtimeLinkService.Reset(state);

        Assert.That(state.combatRealtimeLinkHandshakeActive, Is.False);
        Assert.That(state.combatRealtimeLinkDelaySec, Is.EqualTo(-1f));
        Assert.That(CombatRealtimeLinkService.TickHandshake(state, 1f), Is.True);
    }
}
