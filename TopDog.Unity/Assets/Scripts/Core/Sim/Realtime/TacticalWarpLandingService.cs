using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §2.1.2 · §2.1.3
 * 本文件: TacticalWarpLandingService.cs — 跃迁落点几何与落地判定
 * 【机制要点】
 * · ComputeLandingPoint：origin + normalize(entry-origin) × ClampLandingDistM
 * · EvaluateLandingClearance / RecordLandingClearance：落点半径 2 km 阻挡扫描（预留拦截）
 * · warpLandingObstructed · warpLandingObstructUnitId 仅记录，不取消跃迁
 * 【关联】BattlefieldSceneOriginService · BattlefieldSceneProxyService · TacticalWarpService
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>跃迁落点：场景原点（建筑）沿来向射线 1–1000 km；供拦截泡等后续改写。</summary>
public static class TacticalWarpLandingService
{
    public const float MinLandingDistM = 1_000f;
    public const float MaxLandingDistM = 1_000_000f;
    public const float DefaultLandingDistM = 500_000f;

    public static float ClampLandingDistM(float distM) =>
        Math.Clamp(distM, MinLandingDistM, MaxLandingDistM);

    /// <summary>跃迁起跳后落地判定：目标场景落点处是否有单位阻挡（供后续拦截机制读取）。</summary>
    public const float LandingObstructionRadiusM = 2_000f;

    public static bool EvaluateLandingClearance(
        GameState state,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        float landingDistM,
        out float landingX,
        out float landingY,
        out float landingZ,
        out string? obstructingUnitId)
    {
        landingX = landingY = landingZ = 0f;
        obstructingUnitId = null;
        if (toBf.eventRegionId == null || fromBf.eventRegionId == null)
        {
            return false;
        }

        BattlefieldSceneOriginService.Resolve(state, toBf, out var ox, out var oy, out var oz);
        if (!BattlefieldSceneProxyService.TryResolveProxyPosition(
                state, toBf, fromBf.systemId!, fromBf.eventRegionId, out var entryX, out var entryY, out var entryZ))
        {
            entryX = ox - 50_000f;
            entryY = oy;
            entryZ = oz;
        }

        ComputeLandingPoint(ox, oy, oz, entryX, entryY, entryZ, landingDistM, out landingX, out landingY, out landingZ);

        var radiusSq = LandingObstructionRadiusM * LandingObstructionRadiusM;
        foreach (var u in toBf.units)
        {
            if (u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            var dx = u.x - landingX;
            var dy = u.y - landingY;
            var dz = u.z - landingZ;
            if (dx * dx + dy * dy + dz * dz <= radiusSq)
            {
                obstructingUnitId = u.unitId;
                return true;
            }
        }

        return false;
    }

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

    /// <summary>从场景原点沿入场点方向精确 landingDistM 落点。</summary>
    public static void ComputeLandingPoint(
        float originX,
        float originY,
        float originZ,
        float entryX,
        float entryY,
        float entryZ,
        float landingDistM,
        out float x,
        out float y,
        out float z)
    {
        var dx = entryX - originX;
        var dy = entryY - originY;
        var dz = entryZ - originZ;
        var len = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (len < 0.01f)
        {
            dx = 1f;
            dy = dz = 0f;
            len = 1f;
        }

        var scale = ClampLandingDistM(landingDistM) / len;
        x = originX + dx * scale;
        y = originY + dy * scale;
        z = originZ + dz * scale;
    }

    /// <summary>原点为 (0,0,0) 的兼容重载。</summary>
    public static void ComputeLandingPoint(
        float entryX,
        float entryY,
        float entryZ,
        float landingDistM,
        out float x,
        out float y,
        out float z) =>
        ComputeLandingPoint(0f, 0f, 0f, entryX, entryY, entryZ, landingDistM, out x, out y, out z);
}
