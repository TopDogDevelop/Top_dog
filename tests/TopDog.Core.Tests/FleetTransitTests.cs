using TopDog.App;
using TopDog.Foundation.Io;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class FleetTransitTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void GoCommandStartsTransitWhenHullEquipped()
    {
        var core = CampaignBootstrap.Create(CampaignBootstrap.Profile.SHIPS_AND_MAP, WorldlineType.STORY);
        Assert.That(core.State.members, Is.Not.Empty);
        var member = core.State.members[0];
        member.equippedHullId = "hull_bc_spear";

        var echo = core.SubmitCommand("go Mine Field");
        Assert.That(echo, Does.Contain("跃迁"));
        Assert.That(core.State.fleets.Count, Is.EqualTo(1));
        Assert.That(core.State.fleets[0].inTransit, Is.True);
    }
}
