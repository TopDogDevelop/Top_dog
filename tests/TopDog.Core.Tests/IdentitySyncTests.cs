using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class IdentitySyncTests
{
    [Test]
    public void EnsureFromMembers_UnionsTraitsPerIdentity()
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
                    name = "奥法凯",
                    traitIds = { "trait_loyal", "trait_multibox" },
                },
                new MemberState
                {
                    memberId = "1000000102",
                    identityCode = "10000001",
                    name = "新手教官",
                },
            },
        };
        IdentityMigrationService.EnsureFromMembers(state);
        foreach (var m in state.members)
        {
            Assert.That(m.traitIds, Does.Contain("trait_loyal"));
            Assert.That(m.traitIds, Does.Contain("trait_multibox"));
        }
        Assert.That(state.identities["10000001"].traitIds, Does.Contain("trait_loyal"));
    }

    [Test]
    public void EnsureFromMembers_SyncsMultiboxStats()
    {
        var state = new GameState
        {
            identities =
            {
                ["10000001"] = new IdentityState
                {
                    identityCode = "10000001",
                    energy = 30,
                    wisdom = 50,
                    legionBelonging = 100,
                },
            },
            members =
            {
                new MemberState { memberId = "1000000101", identityCode = "10000001", name = "A", energy = 5, wisdom = 10, legionBelonging = 50 },
                new MemberState { memberId = "1000000102", identityCode = "10000001", name = "B", energy = 99, wisdom = 99, legionBelonging = 99 },
            },
        };
        IdentityMigrationService.EnsureFromMembers(state);
        foreach (var m in state.members)
        {
            Assert.That(IdentityStatFacade.HasMirrorMismatch(state, m), Is.False);
            Assert.That(m.energy, Is.EqualTo(99));
            Assert.That(m.wisdom, Is.EqualTo(99));
            Assert.That(m.legionBelonging, Is.EqualTo(100));
        }
    }

    [Test]
    public void RegenEnergy_OncePerIdentity()
    {
        var state = new GameState
        {
            identities =
            {
                ["10000001"] = new IdentityState { identityCode = "10000001", energy = 5 },
            },
            members =
            {
                new MemberState { memberId = "1000000101", identityCode = "10000001", energy = 5 },
                new MemberState { memberId = "1000000102", identityCode = "10000001", energy = 5 },
            },
        };
        IdentityStatService.RegenEnergyAllMembers(state);
        Assert.That(state.identities["10000001"].energy, Is.EqualTo(6));
        Assert.That(state.members[0].energy, Is.EqualTo(6));
        Assert.That(state.members[1].energy, Is.EqualTo(6));
    }
}
