using TopDog.Net.Protocol;

namespace TopDog.Tests;

public sealed class NetWireCodecTests
{
    [Test]
    public void RoundTripEnvelope()
    {
        var env = new NetEnvelope
        {
            type = NetMessageType.TACTICAL_INPUT,
            sequence = 7,
            payloadJson = "{\"yawInput\":1}",
        };
        var line = NetWireCodec.ToWire(env);
        Assert.That(NetWireCodec.TryParseLine(line, out var parsed), Is.True);
        Assert.That(parsed.type, Is.EqualTo(NetMessageType.TACTICAL_INPUT));
        Assert.That(parsed.sequence, Is.EqualTo(7));
        Assert.That(parsed.payloadJson, Does.Contain("yawInput"));
    }
}
