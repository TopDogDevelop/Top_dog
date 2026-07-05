using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TacticalWarpLandingServiceTests
{
    [Test]
    public void ComputeLandingPoint_ExactRadialDistanceFromOrigin()
    {
        TacticalWarpLandingService.ComputeLandingPoint(
            0f,
            0f,
            0f,
            100_000f,
            0f,
            0f,
            200_000f,
            out var x,
            out var y,
            out var z);

        var dist = MathF.Sqrt(x * x + y * y + z * z);
        Assert.That(dist, Is.EqualTo(200_000f).Within(1f));
        Assert.That(x, Is.EqualTo(200_000f).Within(1f));
        Assert.That(y, Is.EqualTo(0f).Within(0.01f));
        Assert.That(z, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void ComputeLandingPoint_UsesBuildingOriginOffset()
    {
        TacticalWarpLandingService.ComputeLandingPoint(
            10_000f,
            20_000f,
            0f,
            110_000f,
            20_000f,
            0f,
            200_000f,
            out var x,
            out var y,
            out var z);

        var dx = x - 10_000f;
        var dy = y - 20_000f;
        var dist = MathF.Sqrt(dx * dx + dy * dy);
        Assert.That(dist, Is.EqualTo(200_000f).Within(1f));
    }
}
