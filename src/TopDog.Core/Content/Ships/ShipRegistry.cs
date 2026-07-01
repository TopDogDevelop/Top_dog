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
        LoadHullDirectory(reg, Path.Combine(AppRoot.Find(), "content", "ships"));
        LoadHullDirectory(reg, SkirmishContentOverlay.Dir("ships"));
        return reg;
    }

    private static void LoadHullDirectory(ShipRegistry reg, string shipsDir)
    {
        if (!Directory.Exists(shipsDir))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(shipsDir, "hull_*.json"))
        {
            var json = File.ReadAllText(path);
            var hull = JsonSerializer.Deserialize<HullDef>(json, TopDogJson.Options);
            if (hull?.hullId != null)
            {
                reg._hulls[hull.hullId] = hull;
            }
        }
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
