using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class MemberRosterSortTests
{
    [Test]
    public void Order_PrefersLabelsThenTraitsThenAccountName()
    {
        var members = new List<MemberState>
        {
            new() { memberId = "0000000003", accountName = "赵六", traitIds = { "t1", "t2" } },
            new() { memberId = "0000000001", accountName = "奥法凯", labels = { "团长人选" }, traitIds = { "t1" } },
            new() { memberId = "0000000002", accountName = "奥法凯", labels = { "教官" }, traitIds = { "t1", "t2", "t3" } },
            new() { memberId = "0000000004", accountName = "Beta", traitIds = { "t1", "t2" } },
        };
        var sorted = MemberRosterSort.Order(members);
        Assert.That(sorted[0].memberId, Is.EqualTo("0000000002"));
        Assert.That(sorted[1].memberId, Is.EqualTo("0000000001"));
        Assert.That(sorted[^1].memberId, Is.EqualTo("0000000003"));
    }
}
