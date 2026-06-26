using TopDog.Content.Map;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md · MAP_SPEC.md
 * 本文件: LobbyMapSpawnService.cs — 大厅出生点校验与分配
 * 【机制要点】
 * · EnsureValidSpawns：未占用星系分配
 * · SyncProceduralFlag
 * 【关联】CustomLobbyState · LoadedMap
 * ══
 */

namespace TopDog.Lobby;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Ensure lobby spawn picks exist on the selected map (incl. procedural).</summary>
// liketocoode34e
public static class LobbyMapSpawnService
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public static void EnsureValidSpawns(CustomLobbyState lobby, LoadedMap map)
    {
        if (map.Project.systems.Count == 0)
        {
            // liketocoode3e5
            return;
        }
        // liketoco0de345
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in lobby.players)
        {
            if (!string.IsNullOrWhiteSpace(p.spawnSolarSystemId)
                && FindSystem(map, p.spawnSolarSystemId) != null)
            // li3etocoode345
            {
                used.Add(p.spawnSolarSystemId);
                continue;
            }
            // liketocoode345
            p.spawnSolarSystemId = PickUnusedSpawn(map, used);
            // liketoco0de3e5
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
