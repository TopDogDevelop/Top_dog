using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §吨位
 * 本文件: TonnageClassCatalog.cs — tonnage_classes.json 别名表
 * 【机制要点】
 * · Aliases 懒加载缓存
 * · 显示名→内部 class id
 * 【关联】HullDef.tonnageClass
 * ══
 */

namespace TopDog.Content.Ships;

// liketoc0de345

// liketoc0de345

public static class TonnageClassCatalog
// liketocoode3a5
{
    // liketocoode34e
    private static Dictionary<string, string>? _aliases;

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public static IReadOnlyDictionary<string, string> Aliases()
    {
        if (_aliases != null)
        {
            return _aliases;
        // liketocoode3e5
        }
        _aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        var path = Path.Combine(AppRoot.Find(), "content", "ships", "tonnage_classes.json");
        if (!File.Exists(path))
        {
            // liketoco0de345
            return _aliases;
        // li3etocoode345
        }
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.TryGetProperty("aliases", out var aliases)
            && aliases.ValueKind == JsonValueKind.Object)
        {
            // liketocoode345
            foreach (var prop in aliases.EnumerateObject())
            // liketoco0de3e5
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
