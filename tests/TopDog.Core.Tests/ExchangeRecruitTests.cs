using TopDog.Sim.Building;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class ExchangeRecruitTests
{
    [Test]
    public void RecruitComplete_AddsMemberToLegion()
    {
        var state = MakeExchangeState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        var m = new MemberState
        {
            memberId = "9000000101",
            identityCode = "90000001",
            accountSuffix = "01",
            name = "新兵",
            legionId = CampaignLegionIds.Player,
        };
        ExchangeIntentService.PostRecruitComplete(state, CampaignLegionIds.Player, new[] { m });
        ExchangeProcessor.ProcessPending(state);

        Assert.That(state.legionPlayers[CampaignLegionIds.Player].members.Count, Is.EqualTo(1));
        Assert.That(state.members.Count, Is.EqualTo(1));
        Assert.That(m.rosterVisibility, Is.EqualTo(MemberRosterVisibility.Home));
    }

    [Test]
    public void HostileInfiltratorRecruit_HidesFromHomeVisibleRoster()
    {
        var state = MakeExchangeState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        var spy = new MemberState
        {
            memberId = "9000000101",
            identityCode = "90000001",
            accountSuffix = "01",
            name = "潜伏者",
            homeLegionId = CampaignLegionIds.Player,
            traitIds = { InfiltratorRosterService.InfiltratorTraitId },
        };
        ExchangeIntentService.PostRecruitComplete(state, CampaignLegionIds.Ai, new[] { spy });
        ExchangeProcessor.ProcessPending(state);

        Assert.That(spy.legionId, Is.EqualTo(CampaignLegionIds.Ai));
        Assert.That(spy.rosterVisibility, Is.EqualTo(MemberRosterVisibility.Infiltrating));
        Assert.That(LegionPlayerRegistry.VisibleRoster(state, CampaignLegionIds.Player).Count, Is.EqualTo(0));
        Assert.That(state.exchange.infiltrationByIdentity.ContainsKey("90000001"), Is.True);
    }

    [Test]
    public void DoubleHostileRecruit_Rejected()
    {
        var state = MakeExchangeState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        state.legions.Add(new LegionState { legionId = "LEGION_C", isAiControlled = true });
        var spy = new MemberState
        {
            memberId = "9000000101",
            identityCode = "90000001",
            homeLegionId = CampaignLegionIds.Player,
            traitIds = { InfiltratorRosterService.InfiltratorTraitId },
        };
        ExchangeIntentService.PostRecruitComplete(state, CampaignLegionIds.Ai, new[] { spy });
        ExchangeProcessor.ProcessPending(state);

        var spyClone = new MemberState
        {
            memberId = "9000000101",
            identityCode = "90000001",
            homeLegionId = CampaignLegionIds.Player,
            traitIds = { InfiltratorRosterService.InfiltratorTraitId },
        };
        ExchangeIntentService.PostRecruitComplete(state, "LEGION_C", new[] { spyClone });
        ExchangeProcessor.ProcessPending(state);

        Assert.That(LegionPlayerRegistry.Get(state, "LEGION_C")?.members.Count ?? 0, Is.EqualTo(0));
        Assert.That(state.exchange.infiltrationByIdentity["90000001"].hostLegionId, Is.EqualTo(CampaignLegionIds.Ai));
    }

    [Test]
    public void ThreeLegionsSameSystem_OneMultiPartyEncounter()
    {
        var state = MakeExchangeState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        state.legions.Add(new LegionState { legionId = "LEGION_C", isAiControlled = true });
        SeedVisible(state, CampaignLegionIds.Player, "p1", "sys_contested");
        SeedVisible(state, CampaignLegionIds.Ai, "a1", "sys_contested");
        SeedVisible(state, "LEGION_C", "c1", "sys_contested");

        ExchangeProcessor.ProcessPending(state);

        Assert.That(state.exchange.activeEncounters.Count, Is.EqualTo(1));
        Assert.That(state.exchange.activeEncounters[0].participants.Count, Is.EqualTo(3));
    }

    private static GameState MakeExchangeState()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        state.flags["exchange.enabled"] = "1";
        return state;
    }

    private static void SeedVisible(GameState state, string legionId, string memberId, string systemId)
    {
        if (!state.legions.Any(l => legionId.Equals(l.legionId, StringComparison.Ordinal)))
        {
            state.legions.Add(new LegionState { legionId = legionId, isAiControlled = legionId != CampaignLegionIds.Player });
        }
        var m = new MemberState
        {
            memberId = memberId,
            legionId = legionId,
            assignedTask = "警戒",
            currentSolarSystemId = systemId,
            equippedHullId = "hull_frigate_scout",
            rosterVisibility = MemberRosterVisibility.Home,
        };
        LegionPlayerRegistry.AddMemberToLegion(state, legionId, m);
    }
}
