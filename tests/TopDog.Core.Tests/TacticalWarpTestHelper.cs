using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

internal static class TacticalWarpTestHelper
{
    public static void PrepareInitiate(
        GameState state,
        BattlefieldState from,
        BattlefieldState to,
        BattlefieldUnit unit,
        float maxSpeedMps = 100f)
    {
        unit.maxSpeedMps = maxSpeedMps;
        if (to.systemId == null || to.eventRegionId == null)
        {
            return;
        }

        if (!BattlefieldSceneProxyService.TryResolveProxyPosition(
                state, from, to.systemId, to.eventRegionId, out var px, out var py, out var pz))
        {
            return;
        }

        ShipMotionIntegrator.SnapHeadingToward(unit, px, py, pz);
        var forward = TacticalWarpInitiateRules.MinForwardSpeedFraction * maxSpeedMps;
        var (hx, hy, hz) = ShipMotionIntegrator.HeadingToUnitVector(unit.facingRad, unit.pitchRad);
        unit.vx = hx * forward;
        unit.vy = hy * forward;
        unit.vz = hz * forward;
    }
}
