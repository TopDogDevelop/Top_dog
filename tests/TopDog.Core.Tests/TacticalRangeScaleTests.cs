using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TacticalRangeScaleTests
{
    [Test]
    public void KmFromDialT_Endpoints()
    {
        Assert.That(TacticalRangeScale.KmFromDialT(0f), Is.EqualTo(0f).Within(0.01f));
        Assert.That(TacticalRangeScale.KmFromDialT(0.5f), Is.EqualTo(200f).Within(0.01f));
        Assert.That(TacticalRangeScale.KmFromDialT(1f), Is.EqualTo(1000f).Within(0.01f));
    }

    [Test]
    public void DialTFromKm_RoundTrip()
    {
        foreach (var km in new[] { 0f, 1f, 50f, 200f, 500f, 1000f })
        {
            var t = TacticalRangeScale.DialTFromKm(km);
            Assert.That(TacticalRangeScale.KmFromDialT(t), Is.EqualTo(km).Within(0.5f));
        }
    }
}
