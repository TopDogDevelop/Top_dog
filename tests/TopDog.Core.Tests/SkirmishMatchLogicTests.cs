using TopDog.Net.Lan;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class SkirmishMatchLogicTests
{
    [Test]
    public void PickHostIp_IsDeterministicForPair()
    {
        var a = "192.168.1.10";
        var b = "192.168.1.20";
        Assert.That(SkirmishMatchLogic.PickHostIp(a, b), Is.EqualTo(SkirmishMatchLogic.PickHostIp(b, a)));
    }

    [Test]
    public void Evaluate_ScaleMismatchBlocksReady()
    {
        var peers = new List<SkirmishMatchPacket>
        {
            new() { localIp = "192.168.1.20", scale = 30, state = "seeking" },
        };
        var snap = SkirmishMatchLogic.Evaluate("192.168.1.10", 50, peers, 0f);
        Assert.That(snap.Phase, Is.EqualTo(SkirmishMatchPhase.ScaleMismatch));
        Assert.That(snap.OpponentScale, Is.EqualTo(30));
    }

    [Test]
    public void Evaluate_MatchingScaleBecomesReady()
    {
        var peers = new List<SkirmishMatchPacket>
        {
            new() { localIp = "192.168.1.20", scale = 50, state = "seeking" },
        };
        var snap = SkirmishMatchLogic.Evaluate("192.168.1.10", 50, peers, 0f);
        Assert.That(snap.Phase, Is.EqualTo(SkirmishMatchPhase.Ready));
        Assert.That(snap.HostIp, Is.Not.Null);
    }

    [Test]
    public void ParseSkirmishMatch_RoundTrip()
    {
        var payload = LanProtocol.BuildSkirmishMatchPayload("10.0.0.2", 100, "seeking", "abc");
        var parsed = LanProtocol.ParseSkirmishMatch(payload);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.localIp, Is.EqualTo("10.0.0.2"));
        Assert.That(parsed.scale, Is.EqualTo(100));
    }
}
