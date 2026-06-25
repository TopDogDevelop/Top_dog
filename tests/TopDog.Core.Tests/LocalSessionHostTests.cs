using TopDog.App;
using TopDog.Foundation.Io;
using TopDog.Net.Local;
using TopDog.Net.Ports;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class LocalSessionHostTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void CommandSubmitRoutesToCore()
    {
        var core = CampaignBootstrap.Create(CampaignBootstrap.Profile.TUTORIAL_OPS, WorldlineType.STORY);
        var host = new LocalSessionHost();
        host.Bind(core);

        var msg = host.SubmitCommand("帮助");

        Assert.That(msg, Does.Contain("帮助"));
    }

    [Test]
    public void TacticalInputRoutesToPossessionQueue()
    {
        var core = CampaignBootstrap.Create(CampaignBootstrap.Profile.TUTORIAL_OPS, WorldlineType.STORY);
        core.State.combatRealtimeActive = true;
        core.State.possessingMemberId = core.State.members.Count > 0
            ? core.State.members[0].memberId
            : null;
        var host = new LocalSessionHost();
        host.Bind(core);

        host.SubmitTacticalInput(new PossessionInputSample
        {
            yawInput = 1f,
            pitchInput = 0.5f,
            toggleThrottle = true,
            sequence = 42,
        });

        Assert.That(core.State.possessionYawInput, Is.EqualTo(1f).Within(0.001f));
        Assert.That(core.State.possessionPitchInput, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(core.State.possessionToggleThrottle, Is.True);
    }

    [Test]
    public void IsOnlineIsFalse()
    {
        var host = new LocalSessionHost();
        Assert.That(host.IsOnline, Is.False);
    }
}
