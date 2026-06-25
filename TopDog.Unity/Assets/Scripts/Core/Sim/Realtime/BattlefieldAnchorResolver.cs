using TopDog.Content.Map;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class BattlefieldAnchorResolver
{
    public static float[] Resolve(GameState state, string? systemId, string? eventRegionId)
    {
        var fallback = new[] { 0f, 0f, 0f };
        if (state.map?.Project?.systems == null || systemId == null)
        {
            return fallback;
        }
        foreach (var sys in state.map.Project.systems)
        {
            if (!systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            if (eventRegionId == null)
            {
                return fallback;
            }
            foreach (var er in sys.eventRegions)
            {
                if (eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal)
                    || eventRegionId.Equals(er.name, StringComparison.Ordinal))
                {
                    return er.anchorAu is { Length: >= 3 }
                        ? (float[])er.anchorAu.Clone()
                        : fallback;
                }
            }
            return fallback;
        }
        return fallback;
    }
}
