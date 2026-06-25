using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

namespace TopDog.Content.Ships;

public sealed class ShipRegistry
{
    private readonly Dictionary<string, HullDef> _hulls = new();

    public static ShipRegistry LoadDefault()
    {
        var reg = new ShipRegistry();
        var shipsDir = Path.Combine(AppRoot.Find(), "content", "ships");
        if (!Directory.Exists(shipsDir))
        {
            return reg;
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
