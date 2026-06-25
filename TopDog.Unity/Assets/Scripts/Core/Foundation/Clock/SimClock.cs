namespace TopDog.Foundation.Clock;

public sealed class SimClock
{
    private long _tickIndex;
    private double _elapsedSec;

    public long TickIndex => _tickIndex;
    public double ElapsedSec => _elapsedSec;

    public void Advance(float dtSec)
    {
        if (dtSec <= 0f)
        {
            return;
        }
        _tickIndex++;
        _elapsedSec += dtSec;
    }
}
