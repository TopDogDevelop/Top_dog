using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §2.1.2 · docs/TACTICAL_VIEW.md §4.6
 * 本文件: BattlefieldSceneOriginService.cs — 战术场景原点（建筑锚点）
 * 【机制要点】
 * · Resolve：targetBuildingId → 首个 isBuilding → 否则 (0,0,0)
 * · 落点距原点欧氏距离 = landingDistM；距离环圆心与此一致
 * 【关联】TacticalWarpLandingService · TacticalPlaneOverlay
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>战术场景原点：建筑锚点（落点距离、距离环中心）。</summary>
public static class BattlefieldSceneOriginService
{
    public static void Resolve(GameState state, BattlefieldState bf, out float x, out float y, out float z)
    {
        x = y = z = 0f;
        if (bf.targetBuildingId != null)
        {
            foreach (var u in bf.units)
            {
                if (bf.targetBuildingId.Equals(u.buildingId, StringComparison.Ordinal)
                    && !u.IsDestroyed())
                {
                    x = u.x;
                    y = u.y;
                    z = u.z;
                    return;
                }
            }
        }

        foreach (var u in bf.units)
        {
            if (u.isBuilding && !u.IsDestroyed())
            {
                x = u.x;
                y = u.y;
                z = u.z;
                return;
            }
        }
    }
}
