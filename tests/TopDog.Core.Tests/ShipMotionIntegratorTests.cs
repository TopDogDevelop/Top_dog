using TopDog.Sim.Realtime;

namespace TopDog.Tests;

public sealed class ShipMotionIntegratorTests
{
    [Test]
    public void ThrottleAcceleratesAlongHeading()
    {
        var u = new BattlefieldUnit
        {
            facingRad = 0f,
            pitchRad = 0f,
            throttleOn = true,
            accelMps2 = 100f,
            maxSpeedMps = 500f,
        };
        for (var i = 0; i < 20; i++)
        {
            ShipMotionIntegrator.TickUnit(u, 0.1f);
        }
        Assert.That(u.vx, Is.GreaterThan(0f));
        Assert.That(u.SpeedMps(), Is.LessThanOrEqualTo(u.maxSpeedMps + 0.01f));
    }

    [Test]
    public void BrakeSlowsToStop()
    {
        var u = new BattlefieldUnit
        {
            vx = 200f,
            throttleOn = false,
            accelMps2 = 50f,
            maxSpeedMps = 500f,
        };
        for (var i = 0; i < 100; i++)
        {
            ShipMotionIntegrator.TickUnit(u, 0.1f);
        }
        Assert.That(u.SpeedMps(), Is.LessThan(1f));
    }
}
