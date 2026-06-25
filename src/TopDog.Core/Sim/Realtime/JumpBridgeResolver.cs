using TopDog.Content.Map;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>Resolves paired jump-bridge regions for cross-system tactical warp.</summary>
public static class JumpBridgeResolver
{
    public static JumpBridgeDef? FindBridge(MapProject? project, string? fromSystemId, string? toSystemId)
    {
        if (project == null || fromSystemId == null || toSystemId == null)
        {
            return null;
        }
        foreach (var b in project.bridges)
        {
            if (fromSystemId.Equals(b.fromSystemId, StringComparison.Ordinal)
                && toSystemId.Equals(b.toSystemId, StringComparison.Ordinal))
            {
                return b;
            }
            if (fromSystemId.Equals(b.toSystemId, StringComparison.Ordinal)
                && toSystemId.Equals(b.fromSystemId, StringComparison.Ordinal))
            {
                return b;
            }
        }
        return null;
    }

    public static EventRegionDef? FindGateRegion(GameState state, string? systemId, string? bridgeId)
    {
        if (state.map?.Project == null || systemId == null)
        {
            return null;
        }
        foreach (var sys in state.map.Project.systems)
        {
            if (!systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (var er in sys.eventRegions)
            {
                if (!EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal))
                {
                    continue;
                }
                if (bridgeId == null || bridgeId.Equals(er.bridgeId, StringComparison.Ordinal))
                {
                    return er;
                }
            }
        }
        return null;
    }

    public static EventRegionDef? ResolvePeerGate(GameState state, string? fromSystemId, string? toSystemId)
    {
        var bridge = FindBridge(state.map?.Project, fromSystemId, toSystemId);
        var bridgeId = bridge?.bridgeId;
        return FindGateRegion(state, toSystemId, bridgeId)
               ?? FindGateRegion(state, toSystemId, null);
    }
}
