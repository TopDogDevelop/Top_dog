using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

namespace TopDog.Content.Ships;

public static class TonnageClassCatalog
{
    private static Dictionary<string, string>? _aliases;

    public static IReadOnlyDictionary<string, string> Aliases()
    {
        if (_aliases != null)
        {
            return _aliases;
        }
        _aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        var path = Path.Combine(AppRoot.Find(), "content", "ships", "tonnage_classes.json");
        if (!File.Exists(path))
        {
            return _aliases;
        }
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.TryGetProperty("aliases", out var aliases)
            && aliases.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in aliases.EnumerateObject())
            {
                _aliases[prop.Name] = prop.Value.GetString() ?? prop.Name;
            }
        }
        return _aliases;
    }

    public static void InvalidateCache() => _aliases = null;

    public static bool TryResolve(string namePart, out string tonnageClass) =>
        Aliases().TryGetValue(namePart, out tonnageClass!);
}
