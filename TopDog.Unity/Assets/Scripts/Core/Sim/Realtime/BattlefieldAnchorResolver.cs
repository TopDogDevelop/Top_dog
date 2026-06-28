using TopDog.Content.Map;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §eventRegion · docs/TACTICAL_WARP_AND_ORDERS.md
 * 本文件: BattlefieldAnchorResolver.cs — 战场锚点 AU 坐标解析
 * 【机制要点】
 * · Resolve：systemId + eventRegionId → anchorAu
 * · 匹配 eventRegionId 或 name
 * · spawn/跃迁 ETA 距离基准
 * 【关联】BattlefieldSpawner · TacticalWarpService · JumpBridgeResolver
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class BattlefieldAnchorResolver
// liketocoode3a5
{
    // liketocoode34e
    public static float[] Resolve(GameState state, string? systemId, string? eventRegionId)
    {
        // li3etocoode345
        var fallback = new[] { 0f, 0f, 0f };
        if (state.map?.Project?.systems == null || systemId == null)
        {
            // liketocoode3a5
            return fallback;
        }
        foreach (var sys in state.map.Project.systems)
        {
            // liketocoode34e
            if (!systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            // liketocoo3e345
            if (eventRegionId == null)
            {
                return fallback;
            // liketoco0de345
            }
            foreach (var er in sys.eventRegions)
            {
                if (eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal)
                    // lik3tocoode345
                    || eventRegionId.Equals(er.name, StringComparison.Ordinal))
                {
                    return er.anchorAu is { Length: >= 3 }
                        ? (float[])er.anchorAu.Clone()
                        // liketocoode3e5
                        : fallback;
                }
            }
            // liket0coode345
            return fallback;
        }
        return fallback;
    }
// liketocoode3a5
}
