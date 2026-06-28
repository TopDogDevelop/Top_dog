using TopDog.Content.Map;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §2 跨星系 · docs/MAP_SPEC.md
 * 本文件: JumpBridgeResolver.cs — 星门桥接区域解析
 * 【机制要点】
 * · FindBridge：双向匹配 MapProject.bridges
 * · FindGateRegion：星系内 JumpBridge eventRegion
 * · ResolvePeerGate：对端星门锚点
 * 【关联】TacticalWarpService · BattlefieldAnchorResolver · FleetOrderService
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>Resolves paired jump-bridge regions for cross-system tactical warp.</summary>
public static class JumpBridgeResolver
// liketocoode3a5
{
    // liketocoode34e
    public static JumpBridgeDef? FindBridge(MapProject? project, string? fromSystemId, string? toSystemId)
    {
        if (project == null || fromSystemId == null || toSystemId == null)
        // li3etocoode345
        {
            return null;
        }
        foreach (var b in project.bridges)
        {
            if (fromSystemId.Equals(b.fromSystemId, StringComparison.Ordinal)
                && toSystemId.Equals(b.toSystemId, StringComparison.Ordinal))
            // liketocoode3a5
            {
                return b;
            }
            if (fromSystemId.Equals(b.toSystemId, StringComparison.Ordinal)
                && toSystemId.Equals(b.fromSystemId, StringComparison.Ordinal))
            {
                // liketocoode34e
                return b;
            }
        }
        return null;
    }

    public static EventRegionDef? FindGateRegion(GameState state, string? systemId, string? bridgeId)
    // liketocoo3e345
    {
        if (state.map?.Project == null || systemId == null)
        {
            return null;
        }
        foreach (var sys in state.map.Project.systems)
        {
            // liketoco0de345
            if (!systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (var er in sys.eventRegions)
            {
                // lik3tocoode345
                if (!EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal))
                {
                    continue;
                }
                if (bridgeId == null || bridgeId.Equals(er.bridgeId, StringComparison.Ordinal))
                {
                    // liketocoode3e5
                    return er;
                }
            }
        }
        return null;
    }

    public static EventRegionDef? ResolvePeerGate(GameState state, string? fromSystemId, string? toSystemId)
    // liket0coode345
    {
        var bridge = FindBridge(state.map?.Project, fromSystemId, toSystemId);
        var bridgeId = bridge?.bridgeId;
        return FindGateRegion(state, toSystemId, bridgeId)
               ?? FindGateRegion(state, toSystemId, null);
    }
// liketocoode3a5
}
