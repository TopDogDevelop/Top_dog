using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class LegionCommanderTests
{
    [Test]
    public void Appoint_MergesPersonalToLegion()
    {
        var state = new GameState
        {
            identities =
            {
                ["10000001"] = new IdentityState { identityCode = "10000001" },
            },
            members =
            {
                new MemberState
                {
                    memberId = "1000000101",
                    identityCode = "10000001",
                    name = "Leader",
                    multiboxGroupId = "mb_10000001",
                },
            },
        };
        state.personalStockByGroup["mb_10000001"] = new Dictionary<string, int> { ["res_inorganic"] = 5 };
        var msg = LegionCommanderService.Appoint(state, "1000000101");
        Assert.That(msg, Does.Contain("任命"));
        Assert.That(state.commanderIdentityCode, Is.EqualTo("10000001"));
        Assert.That(state.legionStock.GetValueOrDefault("res_inorganic"), Is.EqualTo(5));
        Assert.That(state.personalStockByGroup["mb_10000001"], Is.Empty);
    }

    [Test]
    public void Dismiss_PenalizesOthersNotCommander()
    {
        var state = new GameState
        {
            storyRound = 5,
            commanderLastDismissStoryRound = 0,
            commanderIdentityCode = "10000001",
            identities =
            {
                ["10000001"] = new IdentityState { identityCode = "10000001", legionBelonging = 80, isLegionCommander = true },
                ["10000002"] = new IdentityState { identityCode = "10000002", legionBelonging = 60 },
            },
            members =
            {
                new MemberState { memberId = "1000000101", identityCode = "10000001", name = "C" },
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "O" },
            },
        };
        var msg = LegionCommanderService.Dismiss(state);
        Assert.That(msg, Does.Contain("卸任"));
        Assert.That(state.commanderIdentityCode, Is.Null);
        Assert.That(state.identities["10000001"].legionBelonging, Is.EqualTo(80));
        Assert.That(state.identities["10000002"].legionBelonging, Is.EqualTo(10));
    }

    [Test]
    public void Commander_CannotDepart()
    {
        var state = new GameState
        {
            commanderIdentityCode = "10000001",
            identities = { ["10000001"] = new IdentityState { identityCode = "10000001", legionBelonging = -5 } },
            members = { new MemberState { memberId = "1000000101", identityCode = "10000001" } },
        };
        LegionDepartureService.Depart(state, "10000001");
        Assert.That(state.members, Has.Count.EqualTo(1));
    }

    [Test]
    public void Dismiss_RespectsCooldown()
    {
        var state = new GameState
        {
            storyRound = 2,
            commanderLastDismissStoryRound = 1,
            commanderIdentityCode = "10000001",
            identities = { ["10000001"] = new IdentityState { identityCode = "10000001" } },
            members = { new MemberState { memberId = "1000000101", identityCode = "10000001" } },
        };
        var msg = LegionCommanderService.Dismiss(state);
        Assert.That(msg, Does.Contain("冷却"));
        Assert.That(state.commanderIdentityCode, Is.EqualTo("10000001"));
    }
}
