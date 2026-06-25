using TopDog.Sim.Building;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class InfiltratorRosterTests
{
    [Test]
    public void BeginInfiltration_HidesFromVisibleRoster()
    {
        var state = new GameState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        var m = new MemberState
        {
            memberId = "spy1",
            legionId = CampaignLegionIds.Player,
            homeLegionId = CampaignLegionIds.Player,
            name = "潜伏者",
        };
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Player, m);

        InfiltratorRosterService.BeginInfiltration(state, m, CampaignLegionIds.Player, "sys_far");

        Assert.That(m.rosterVisibility, Is.EqualTo(MemberRosterVisibility.Infiltrating));
        Assert.That(LegionPlayerRegistry.VisibleRoster(state, CampaignLegionIds.Player).Count, Is.EqualTo(0));
    }

    [Test]
    public void DismissFromHostLegion_ReturnsToHomeRoster()
    {
        var state = new GameState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        var m = new MemberState
        {
            memberId = "spy1",
            legionId = CampaignLegionIds.Ai,
            homeLegionId = CampaignLegionIds.Player,
            rosterVisibility = MemberRosterVisibility.Infiltrating,
            name = "潜伏者",
        };
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Ai, m);

        InfiltratorRosterService.DismissFromHostLegion(state, m, CampaignLegionIds.Ai);

        Assert.That(m.legionId, Is.EqualTo(CampaignLegionIds.Player));
        Assert.That(m.rosterVisibility, Is.EqualTo(MemberRosterVisibility.Home));
        Assert.That(LegionPlayerRegistry.VisibleRoster(state, CampaignLegionIds.Player).Count, Is.EqualTo(1));
    }
}
