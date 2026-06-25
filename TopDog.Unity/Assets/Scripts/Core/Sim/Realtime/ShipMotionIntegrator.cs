namespace TopDog.Sim.Realtime;

public static class ShipMotionIntegrator
{
    public static void TickUnit(BattlefieldUnit u, float dtSec)
    {
        if (u.isBuilding || u.inTacticalWarp)
        {
            return;
        }

        var (ax, ay, az) = ComputeAcceleration(u);
        u.vx += ax * dtSec;
        u.vy += ay * dtSec;
        u.vz += az * dtSec;

        var speed = u.SpeedMps();
        if (speed > u.maxSpeedMps && speed > 0.0001f)
        {
            var scale = u.maxSpeedMps / speed;
            u.vx *= scale;
            u.vy *= scale;
            u.vz *= scale;
        }

        u.x += u.vx * dtSec;
        u.y += u.vy * dtSec;
        u.z += u.vz * dtSec;
    }

    public static (float ax, float ay, float az) ComputeAcceleration(BattlefieldUnit u)
    {
        var f = HeadingToUnitVector(u.facingRad, u.pitchRad);
        if (u.throttleOn)
        {
            return (f.x * u.accelMps2, f.y * u.accelMps2, f.z * u.accelMps2);
        }

        var speed = u.SpeedMps();
        if (speed < 0.01f)
        {
            return (0f, 0f, 0f);
        }

        var inv = 1f / speed;
        return (-u.vx * inv * u.accelMps2, -u.vy * inv * u.accelMps2, -u.vz * inv * u.accelMps2);
    }

    public static (float axPos, float axNeg, float ayPos, float ayNeg, float azPos, float azNeg) DecomposeSixWay(
        float ax, float ay, float az)
    {
        return (Math.Max(0f, ax), Math.Max(0f, -ax), Math.Max(0f, ay), Math.Max(0f, -ay),
            Math.Max(0f, az), Math.Max(0f, -az));
    }

    public static (float x, float y, float z) HeadingToUnitVector(float yawRad, float pitchRad)
    {
        var cp = (float)Math.Cos(pitchRad);
        return (cp * (float)Math.Cos(yawRad), cp * (float)Math.Sin(yawRad), (float)Math.Sin(pitchRad));
    }

    public static void SteerToward(BattlefieldUnit u, float targetYaw, float targetPitch, float dtSec)
    {
        u.facingRad = ApproachAngle(u.facingRad, targetYaw, u.yawRateRadPerSec * dtSec);
        u.pitchRad = ApproachAngle(u.pitchRad, targetPitch, u.pitchRateRadPerSec * dtSec);
    }

    public static void ApplyManualFacing(BattlefieldUnit u, float yawInput, float pitchInput, float dtSec)
    {
        if (Math.Abs(yawInput) > 0.01f)
        {
            u.facingRad += yawInput * u.yawRateRadPerSec * dtSec;
        }
        if (Math.Abs(pitchInput) > 0.01f)
        {
            u.pitchRad += pitchInput * u.pitchRateRadPerSec * dtSec;
        }
        u.pitchRad = Math.Clamp(u.pitchRad, -1.2f, 1.2f);
    }

    private static float ApproachAngle(float current, float target, float maxStep)
    {
        var delta = NormalizeAngle(target - current);
        if (Math.Abs(delta) <= maxStep)
        {
            return target;
        }
        return current + Math.Sign(delta) * maxStep;
    }

    private static float NormalizeAngle(float rad)
    {
        while (rad > Math.PI)
        {
            rad -= (float)(2 * Math.PI);
        }
        while (rad < -Math.PI)
        {
            rad += (float)(2 * Math.PI);
        }
        return rad;
    }
}
