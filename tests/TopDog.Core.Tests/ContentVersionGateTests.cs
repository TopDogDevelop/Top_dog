using NUnit.Framework;
using TopDog.Net.Lan;

namespace TopDog.Tests;

[TestFixture]
public sealed class ContentVersionGateTests
{
    [Test]
    public void Compare_OrdersByDateThenRevision()
    {
        Assert.That(ContentVersionGate.Compare("202607.14.2", "202607.14.1"), Is.GreaterThan(0));
        Assert.That(ContentVersionGate.Compare("202607.13.9", "202607.14.1"), Is.LessThan(0));
        Assert.That(ContentVersionGate.Compare("202607.14.10", "202607.14.3"), Is.GreaterThan(0));
        Assert.That(ContentVersionGate.Compare("202608.1.1", "202607.31.9"), Is.GreaterThan(0));
    }

    [Test]
    public void Compare_TreatsPaddedRevisionAsSameNumber()
    {
        Assert.That(ContentVersionGate.Compare("202607.14.003", "202607.14.3"), Is.EqualTo(0));
    }

    [Test]
    public void TryParse_AllowsUpToThreeDigitRevision_RejectsLettersAndOldForm()
    {
        Assert.That(ContentVersionGate.TryParse("202607.14.1", out var y, out var m, out var d, out var n), Is.True);
        Assert.That(y, Is.EqualTo(2026));
        Assert.That(m, Is.EqualTo(7));
        Assert.That(d, Is.EqualTo(14));
        Assert.That(n, Is.EqualTo(1));

        Assert.That(ContentVersionGate.TryParse("202607.14.999", out _, out _, out _, out var n999), Is.True);
        Assert.That(n999, Is.EqualTo(999));
        Assert.That(ContentVersionGate.TryParse("202607.14.1000", out _, out _, out _, out _), Is.False);
        Assert.That(ContentVersionGate.TryParse("2026.7.14.v1", out _, out _, out _, out _), Is.False);
        Assert.That(ContentVersionGate.TryParse("202607.14.v1", out _, out _, out _, out _), Is.False);
    }

    [Test]
    public void Matches_StillRequiresExactString()
    {
        ContentVersionGate.Set("202607.14.3");
        Assert.That(ContentVersionGate.Matches("202607.14.3"), Is.True);
        Assert.That(ContentVersionGate.Matches("202607.14.003"), Is.False);
    }
}
