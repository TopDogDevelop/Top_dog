using TopDog.Sim.Combat;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class HpBarVisualDistortionTests
{
    [Test]
    public void DistortPercent_EndpointsAndMiddle()
    {
        Assert.That(HpBarVisualDistortion.DistortPercent(0f), Is.EqualTo(0f));
        Assert.That(HpBarVisualDistortion.DistortPercent(10f), Is.EqualTo(20f));
        Assert.That(HpBarVisualDistortion.DistortPercent(50f), Is.EqualTo(50f));
        Assert.That(HpBarVisualDistortion.DistortPercent(90f), Is.EqualTo(80f));
        Assert.That(HpBarVisualDistortion.DistortPercent(100f), Is.EqualTo(100f));
    }
}
