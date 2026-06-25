using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class LegionPlayerStateTests
{
    [Test]
    public void PartitionMembers_BucketsByLegionId()
    {
        var state = new GameState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        state.members.Add(new MemberState { memberId = "p1", legionId = CampaignLegionIds.Player });
        state.members.Add(new MemberState { memberId = "a1", legionId = CampaignLegionIds.Ai });

        LegionPlayerRegistry.PartitionMembers(state);

        Assert.That(state.legionPlayers.ContainsKey(CampaignLegionIds.Player), Is.True);
        Assert.That(state.legionPlayers[CampaignLegionIds.Player].members.Count, Is.EqualTo(1));
        Assert.That(state.legionPlayers[CampaignLegionIds.Ai].members.Count, Is.EqualTo(1));
        Assert.That(state.members.Count, Is.EqualTo(2));
    }

    [Test]
    public void PartitionMembers_MapsLegacyPlayerLegionIdToLocalLobbyLegion()
    {
        var state = new GameState();
        var localId = "lobby-host-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.members.Add(new MemberState
        {
            memberId = "p1",
            isPlayer = true,
            rosterVisibility = MemberRosterVisibility.Home,
        });

        LegionPlayerRegistry.PartitionMembers(state);

        Assert.That(MemberRosterSort.RosterForLegion(state, localId).Count, Is.EqualTo(1));
    }

    [Test]
    public void EnsureRosterForLegion_ReindexesFlatMembersWhenBucketEmpty()
    {
        var state = new GameState();
        var localId = "lobby-host-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.members.Add(new MemberState
        {
            memberId = "p1",
            isPlayer = true,
            rosterVisibility = MemberRosterVisibility.Home,
        });

        Assert.That(LegionPlayerRegistry.VisibleRoster(state, localId).Count, Is.EqualTo(0));

        LegionPlayerRegistry.EnsureRosterForLegion(state, localId);

        Assert.That(MemberRosterSort.RosterForLegion(state, localId).Count, Is.EqualTo(1));
        Assert.That(state.legionPlayers[localId].members.Count, Is.EqualTo(1));
    }

    [Test]
    public void EnsureAggregateFromBuckets_RefillsEmptyAggregate()
    {
        var state = new GameState();
        var localId = "lobby-host-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        LegionPlayerRegistry.EnsureFromLegions(state);
        state.legionPlayers[localId].members.Add(new MemberState
        {
            memberId = "p1",
            legionId = localId,
            rosterVisibility = MemberRosterVisibility.Home,
        });
        Assert.That(state.members.Count, Is.EqualTo(0));

        LegionPlayerRegistry.EnsureAggregateFromBuckets(state);

        Assert.That(state.members.Count, Is.EqualTo(1));
        Assert.That(MemberRosterSort.RosterForLegion(state, localId).Count, Is.EqualTo(1));
    }

    [Test]
    public void VisibleRoster_HidesInfiltratingMembers()
    {
        var state = new GameState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        var home = new MemberState
        {
            memberId = "h1",
            legionId = CampaignLegionIds.Player,
            rosterVisibility = MemberRosterVisibility.Home,
        };
        var spy = new MemberState
        {
            memberId = "s1",
            legionId = CampaignLegionIds.Player,
            rosterVisibility = MemberRosterVisibility.Infiltrating,
        };
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Player, home);
        LegionPlayerRegistry.AddMemberToLegion(state, CampaignLegionIds.Player, spy);

        var visible = LegionPlayerRegistry.VisibleRoster(state, CampaignLegionIds.Player);

        Assert.That(visible.Count, Is.EqualTo(1));
        Assert.That(visible[0].memberId, Is.EqualTo("h1"));
    }
}
