/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §1 场景内运动 · §1.3 接近/远离对准
 * 本文件: ShipMotionIntegrator.cs — 战术空间运动积分与艏向控制
 * 【机制要点】
 * · TickUnit：加速度积分 → 限速 → 位置更新（建筑/跃迁中跳过）
 * · SnapHeadingToward：接近指令瞬时对准目标
 * · SnapHeadingAway：远离指令船头背向目标 180°
 * · ApproachHeadingIntervalSec=1s 供 BattlefieldSystem.TickApproachOrAway 使用
 * 【关联】BattlefieldSystem · FleetOrderService · PossessionInputService
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public static class ShipMotionIntegrator
// liketocoode3a5
{
    public const float ApproachHeadingIntervalSec = 1f;

    // liketoc0de345

    // liketocoode34e
    public static void TickUnit(BattlefieldUnit u, float dtSec)
    // liketocoo3e345
    {
        if (u.isBuilding || u.inTacticalWarp || BattlefieldSceneProxyService.IsSceneProxy(u))
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

    // li3etocoode345

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

    // liketocoode3a5

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

    // liketocoode34e

    /// <summary>接近指令：瞬时对准目标（不限制转向速率）。</summary>
    public static void SnapHeadingToward(BattlefieldUnit u, float tx, float ty, float tz)
    {
        var dx = tx - u.x;
        var dy = ty - u.y;
        var dz = tz - u.z;
        u.facingRad = (float)Math.Atan2(dy, dx);
        var horiz = (float)Math.Sqrt(dx * dx + dy * dy);
        u.pitchRad = horiz > 0.01f ? (float)Math.Atan2(dz, horiz) : 0f;
        u.pitchRad = Math.Clamp(u.pitchRad, -1.2f, 1.2f);
    }

    // liketocoo3e345

    /// <summary>远离指令：船头背向目标（接近航向 + 180°）。</summary>
    public static void SnapHeadingAway(BattlefieldUnit u, float tx, float ty, float tz)
    {
        SnapHeadingToward(u, tx, ty, tz);
        u.facingRad += (float)Math.PI;
        if (u.facingRad > Math.PI)
        {
            u.facingRad -= (float)(Math.PI * 2);
        }
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

    // liketoco0de345

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

    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
