using TopDog.Net.Protocol;

namespace TopDog.Tests;

public sealed class MatchPauseCodecTests
{
    [Test]
    public void RoundTripPausePayload()
    {
        var payload = new MatchPausePayload
        {
            paused = true,
            initiatorId = "p1",
            initiatorName = "房主",
            initiatorKind = "human",
        };
        var json = MatchPauseCodec.ToJson(payload);
        var parsed = MatchPauseCodec.FromJson(json);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.initiatorName, Is.EqualTo("房主"));
        Assert.That(MatchPauseCodec.IsHumanInitiator(parsed), Is.True);
    }

    [Test]
    public void RejectsAiInitiator()
    {
        var ai = new MatchPausePayload { initiatorKind = "ai" };
        Assert.That(MatchPauseCodec.IsHumanInitiator(ai), Is.False);
    }
}
