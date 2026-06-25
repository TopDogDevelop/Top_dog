using TopDog.App;
using TopDog.Foundation.Io;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class SimulationCoreTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void EmptyTickAndHelpCommand()
    {
        var core = CampaignBootstrap.Create(CampaignBootstrap.Profile.TUTORIAL_OPS, WorldlineType.STORY);
        core.Tick(0.016f);
        var help = core.SubmitCommand("帮助");
        Assert.That(help, Does.Contain("帮助"));
        Assert.That(core.State.alertLog, Is.Not.Empty);
    }
}
