using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>同星系战术跃迁发起门控（艏向对准 · 艏向速度 · 扰断抗性）。</summary>
public static class TacticalWarpInitiateRules
{
    public const float MaxHeadingErrorDeg = 20f;
    public const float MinForwardSpeedFraction = 0.5f;
    public const float ImmobileMaxSpeedThresholdMps = 1f;

    public enum FailReason
    {
        None,
        Heading,
        ForwardSpeed,
        WarpScram,
    }

    public static float EffectiveMaxSpeedMps(BattlefieldUnit unit) =>
        unit.maxSpeedMps > 0f ? unit.maxSpeedMps : 0f;

    public static float ForwardSpeedMps(BattlefieldUnit unit)
    {
        var (hx, hy, hz) = ShipMotionIntegrator.HeadingToUnitVector(unit.facingRad, unit.pitchRad);
        return unit.vx * hx + unit.vy * hy + unit.vz * hz;
    }

    public static float HeadingErrorDegToward(BattlefieldUnit unit, float tx, float ty, float tz)
    {
        var dx = tx - unit.x;
        var dy = ty - unit.y;
        var dz = tz - unit.z;
        var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (dist < 0.01f)
        {
            return 0f;
        }

        var desiredYaw = MathF.Atan2(dy, dx);
        var horiz = MathF.Sqrt(dx * dx + dy * dy);
        var desiredPitch = horiz > 0.01f ? MathF.Atan2(dz, horiz) : 0f;
        var (hx, hy, hz) = ShipMotionIntegrator.HeadingToUnitVector(unit.facingRad, unit.pitchRad);
        var (dxu, dyu, dzu) = ShipMotionIntegrator.HeadingToUnitVector(desiredYaw, desiredPitch);
        var dot = Math.Clamp(hx * dxu + hy * dyu + hz * dzu, -1f, 1f);
        return MathF.Acos(dot) * (180f / MathF.PI);
    }

    public static bool PassesHeadingCheck(BattlefieldUnit unit, float proxyX, float proxyY, float proxyZ) =>
        HeadingErrorDegToward(unit, proxyX, proxyY, proxyZ) <= MaxHeadingErrorDeg;

    public static bool PassesForwardSpeedCheck(BattlefieldUnit unit)
    {
        var effectiveMax = EffectiveMaxSpeedMps(unit);
        if (effectiveMax <= ImmobileMaxSpeedThresholdMps)
        {
            return true;
        }

        return ForwardSpeedMps(unit) >= MinForwardSpeedFraction * effectiveMax;
    }

    public static float ResolveWarpScramResist(BattlefieldUnit unit, HullDef? hull) =>
        hull?.warpScramResist ?? 0f;

    public static bool PassesWarpScramCheck(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit unit,
        HullDef? hull,
        ModuleRegistry? modules = null)
    {
        var resist = ResolveWarpScramResist(unit, hull);
        var strength = TacticalWarpDisruptionService.EffectiveWarpScramStrength(bf, unit, modules);
        return strength <= 0f || resist > strength;
    }

    public static FailReason Evaluate(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit unit,
        HullDef? hull,
        float proxyX,
        float proxyY,
        float proxyZ,
        ModuleRegistry? modules = null)
    {
        if (!PassesHeadingCheck(unit, proxyX, proxyY, proxyZ))
        {
            return FailReason.Heading;
        }

        if (!PassesForwardSpeedCheck(unit))
        {
            return FailReason.ForwardSpeed;
        }

        if (!PassesWarpScramCheck(state, bf, unit, hull, modules))
        {
            return FailReason.WarpScram;
        }

        return FailReason.None;
    }

    public static string MessageFor(FailReason reason) => reason switch
    {
        FailReason.Heading => "须将舰艏对准目标场景（误差≤20°）",
        FailReason.ForwardSpeed => "须在舰艏方向达到有效最大航速的50%以上",
        FailReason.WarpScram => "跃迁干扰强度超过舰船抗性",
        _ => "当前无法跃迁",
    };
}
