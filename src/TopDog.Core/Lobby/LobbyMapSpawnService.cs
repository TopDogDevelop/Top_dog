using TopDog.Content.Map;

namespace TopDog.Lobby;

/// <summary>Ensure lobby spawn picks exist on the selected map (incl. procedural).</summary>
public static class LobbyMapSpawnService
{
    public static void EnsureValidSpawns(CustomLobbyState lobby, LoadedMap map)
    {
        if (map.Project.systems.Count == 0)
        {
            return;
        }
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in lobby.players)
        {
            if (!string.IsNullOrWhiteSpace(p.spawnSolarSystemId)
                && FindSystem(map, p.spawnSolarSystemId) != null)
            {
                used.Add(p.spawnSolarSystemId);
                continue;
            }
            p.spawnSolarSystemId = PickUnusedSpawn(map, used);
            if (p.spawnSolarSystemId != null)
            {
                used.Add(p.spawnSolarSystemId);
            }
        }
    }

    public static void SyncProceduralFlag(CustomLobbyState lobby)
    {
        lobby.proceduralMap = MapCatalogEntry.ProceduralMapId.Equals(lobby.mapPath, StringComparison.Ordinal);
    }

    private static string? PickUnusedSpawn(LoadedMap map, HashSet<string> used)
    {
        foreach (var s in map.Project.systems)
        {
            if (s.solarSystemId == null || used.Contains(s.solarSystemId))
            {
                continue;
            }
            return s.solarSystemId;
        }
        return map.Project.systems[0].solarSystemId;
    }

    private static SolarSystemDef? FindSystem(LoadedMap map, string id)
    {
        foreach (var s in map.Project.systems)
        {
            if (id.Equals(s.solarSystemId, StringComparison.Ordinal))
            {
                return s;
            }
        }
        return null;
    }
}
