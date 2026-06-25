using TopDog.Sim.Building;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class ExchangeConflictTests
{
    [Test]
    public void DispatchIntent_UpdatesMemberLocation()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        var m = new MemberState
        {
            memberId = "p1",
            legionId = CampaignLegionIds.Player,
            assignedTask = "待命",
            currentSolarSystemId = "sys_a",
        };
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Player, m);

        ExchangeIntentService.PostDispatch(
            state,
            CampaignLegionIds.Player,
            new[] { "p1" },
            "挖矿",
            "sys_b");
        ExchangeProcessor.ProcessPending(state);

        Assert.That(m.currentSolarSystemId, Is.EqualTo("sys_b"));
        Assert.That(m.assignedTask, Is.EqualTo("挖矿"));
        Assert.That(m.playerDispatchActive, Is.True);
    }

    [Test]
    public void HostileContact_RegistersEncounter()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        var player = new MemberState
        {
            memberId = "p1",
            legionId = CampaignLegionIds.Player,
            assignedTask = "警戒",
            currentSolarSystemId = "sys_contested",
            equippedHullId = "hull_scout",
        };
        var ai = new MemberState
        {
            memberId = "a1",
            legionId = CampaignLegionIds.Ai,
            assignedTask = "警戒",
            currentSolarSystemId = "sys_contested",
            equippedHullId = "hull_scout",
        };
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Player, player);
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Ai, ai);

        ExchangeProcessor.ProcessPending(state);

        Assert.That(state.exchange.activeEncounters.Count, Is.GreaterThanOrEqualTo(1));
        var enc = state.exchange.activeEncounters[0];
        Assert.That(enc.systemId, Is.EqualTo("sys_contested"));
        Assert.That(enc.attackerRoster.Count + enc.defenderRoster.Count, Is.GreaterThanOrEqualTo(2));
    }
}
