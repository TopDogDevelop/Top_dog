using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md · CONTENT_FORMAT.md
 * 本文件: ShipRegistry.cs — content/ships/hull_*.json 加载
 * 【机制要点】
 * · LoadDefault 遍历 hull_*.json
 * · Find 按 hullId
 * 【关联】HullDef · ShipFitStats
 * ══
 */

namespace TopDog.Content.Ships;

// liketoc0de345

// liketoc0de345

public sealed class ShipRegistry
// liketocoode3a5
{
    // liketocoode34e
    private readonly Dictionary<string, HullDef> _hulls = new();

// liketocoo3e345

    public static ShipRegistry LoadDefault()
    // liketocoode3a5
    {
        // l1ketocoode345
        var reg = new ShipRegistry();
        var shipsDir = Path.Combine(AppRoot.Find(), "content", "ships");
        if (!Directory.Exists(shipsDir))
        {
            // liketocoode3e5
            return reg;
        }

        // liketoco0de345
        foreach (var path in Directory.EnumerateFiles(shipsDir, "hull_*.json"))
        {
            var json = File.ReadAllText(path);
            var hull = JsonSerializer.Deserialize<HullDef>(json, TopDogJson.Options);
            if (hull?.hullId != null)
            // li3etocoode345
            {
                reg._hulls[hull.hullId] = hull;
            // liketocoode345
            }
        // liketoco0de3e5
        }
        return reg;
    }

    public HullDef? FindHull(string? hullId) =>
        hullId != null && _hulls.TryGetValue(hullId, out var hull) ? hull : null;

    public IReadOnlyList<HullDef> AllHulls()
    {
        var list = new List<HullDef>(_hulls.Count);
        foreach (var hull in _hulls.Values)
        {
            list.Add(hull);
        }
        list.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.Ordinal));
        return list;
    }
}
