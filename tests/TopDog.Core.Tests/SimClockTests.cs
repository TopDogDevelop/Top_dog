using TopDog.Foundation.Clock;

namespace TopDog.Tests;

public sealed class SimClockTests
{
    [Test]
    public void AdvanceIncrementsTick()
    {
        var clock = new SimClock();
        clock.Advance(0.016f);
        Assert.That(clock.TickIndex, Is.EqualTo(1));
        Assert.That(clock.ElapsedSec, Is.EqualTo(0.016).Within(0.0001));
    }
}
