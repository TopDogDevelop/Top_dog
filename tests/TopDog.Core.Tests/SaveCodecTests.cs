using TopDog.Sim.Persist;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class SaveCodecTests
{
    [Test]
    public void RoundTripPreservesPhase()
    {
        var state = new GameState
        {
            campaignName = "Test",
            phase = GamePhase.OPERATIONS,
        };
        var json = SaveCodec.ToJson(state);
        var loaded = SaveCodec.FromJson(json);
        Assert.That(loaded.campaignName, Is.EqualTo("Test"));
        Assert.That(loaded.phase, Is.EqualTo(GamePhase.OPERATIONS));
    }
}
