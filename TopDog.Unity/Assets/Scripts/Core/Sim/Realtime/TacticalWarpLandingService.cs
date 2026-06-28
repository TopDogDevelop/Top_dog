using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>跃迁落点：场景中心沿来向射线 1–1000 km；供拦截泡等后续改写。</summary>
public static class TacticalWarpLandingService
{
    public const float MinLandingDistM = 1_000f;
    public const float MaxLandingDistM = 1_000_000f;
    public const float DefaultLandingDistM = 500_000f;

    public static float ClampLandingDistM(float distM) =>
        Math.Clamp(distM, MinLandingDistM, MaxLandingDistM);

    public static float ResolveLandingDistM(GameState state, BattlefieldUnit? unit = null)
    {
        if (unit != null && unit.warpLandingDistM >= MinLandingDistM)
        {
            return ClampLandingDistM(unit.warpLandingDistM);
        }

        if (state.tacticalWarpLandingDistM >= MinLandingDistM)
        {
            return ClampLandingDistM(state.tacticalWarpLandingDistM);
        }

        return DefaultLandingDistM;
    }

    public static void ComputeLandingPoint(
        float entryDirX,
        float entryDirY,
        float entryDirZ,
        float landingDistM,
        out float x,
        out float y,
        out float z)
    {
        var horiz = MathF.Sqrt(entryDirX * entryDirX + entryDirY * entryDirY);
        if (horiz < 0.01f)
        {
            entryDirX = 1f;
            entryDirY = 0f;
            horiz = 1f;
        }

        var nx = entryDirX / horiz;
        var ny = entryDirY / horiz;
        var dist = ClampLandingDistM(landingDistM);
        x = nx * dist;
        y = ny * dist;
        z = entryDirZ / MathF.Max(horiz, 0.01f) * dist * 0.15f;
    }
}
