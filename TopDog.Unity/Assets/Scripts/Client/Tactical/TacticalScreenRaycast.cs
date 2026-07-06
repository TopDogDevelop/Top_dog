using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;

namespace TopDog.Client.Tactical;

/// <summary>屏幕坐标 → 过注视点平面的世界坐标（TACTICAL_NAVIGATION.md）。</summary>
public static class TacticalScreenRaycast
{
    public static bool TryRaycastFocusPlane(
        TacticalViewportCamera camera,
        GameState state,
        BattlefieldState bf,
        Vector2 screenLocal,
        float viewportWidth,
        float viewportHeight,
        out float worldX,
        out float worldY,
        out float worldZ)
    {
        worldX = worldY = worldZ = 0f;
        if (camera == null || state == null || bf == null)
        {
            return false;
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(state, bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;

        if (!TacticalRaycastMath.TryScreenToFocusPlaneOffset(
                screenLocal.x,
                screenLocal.y,
                viewportWidth,
                viewportHeight,
                camera.VerticalFovDeg,
                camera.OrbitYawRad,
                camera.OrbitPitchRad,
                camera.ViewDistance,
                out var dx,
                out var dy,
                out var dz))
        {
            return false;
        }

        worldX = fx + dx;
        worldY = fy + dy;
        worldZ = fz + dz;
        return true;
    }
}
