using TopDog.Net;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class NetSnapshotPartitionTests
{
    [Test]
    public void ForGuest_KeepsOnlyLocalLegionMembers()
    {
        var full = new GameState();
        full.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        full.legions.Add(new LegionState { legionId = CampaignLegionIds.Ai, isAiControlled = true });
        LegionPlayerRegistry.AddMemberToLegion(
            full,
            CampaignLegionIds.Player,
            new MemberState { memberId = "p1", legionId = CampaignLegionIds.Player });
        LegionPlayerRegistry.AddMemberToLegion(
            full,
            CampaignLegionIds.Ai,
            new MemberState { memberId = "a1", legionId = CampaignLegionIds.Ai });

        var guest = NetSnapshotPartition.ForGuest(full, CampaignLegionIds.Player);

        Assert.That(guest.legionPlayers.Count, Is.EqualTo(1));
        Assert.That(guest.legionPlayers.ContainsKey(CampaignLegionIds.Player), Is.True);
        Assert.That(guest.members.Count, Is.EqualTo(1));
        Assert.That(guest.members[0].memberId, Is.EqualTo("p1"));
        Assert.That(guest.exchange, Is.Not.Null);
    }
}
