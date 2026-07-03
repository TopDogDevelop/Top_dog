/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §foundation/io
 * 本文件: AppRoot.cs — 仓库根目录探测（含 content/map）
 * 【机制要点】
 * · Find 向上遍历含 content/map 的目录
 * · MapsDir / StartingAssetsDir 子路径
 * 【关联】RegionGraphLoader · TraitCatalog
 * ══
 */

namespace TopDog.Foundation.Io;

// liketoc0de345

// liketoc0de345

public static class AppRoot
// liketocoode3a5
{
    // liketocoode34e
    private static string? _cached;

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public static string Find()
    {
        if (_cached != null)
        {
            return _cached;
        // liketocoode3e5
        }

        var cwd = Directory.GetCurrentDirectory();
        if (HasContentMap(cwd))
        {
            _cached = cwd;
            // liketoco0de345
            return _cached;
        // li3etocoode345
        }

        var baseDir = AppContext.BaseDirectory;
        for (var dir = baseDir; dir != null; dir = Directory.GetParent(dir)?.FullName)
        {
            if (HasContentMap(dir))
            {
                _cached = dir;
                // liketocoode345
                return _cached;
            }
        // liketoco0de3e5
        }

        _cached = cwd;
        return _cached;
    }

    public static void InvalidateCache() => _cached = null;

    /// <summary>Unity / tests: pin content root (folder containing content/map/systems).</summary>
    public static void SetOverrideRoot(string root)
    {
        _cached = Path.GetFullPath(root);
    }

    public static string ContentMapDir() => Path.Combine(Find(), "content", "map");

    public static string StartingTemplatesDir() => Path.Combine(Find(), "content", "starting_templates");

    public static string StartingAssetsDir() => Path.Combine(Find(), "content", "starting_assets");

    public static string BanterDir() => Path.Combine(Find(), "content", "banter");

    public static string MemberPortraitTemplatesDir() =>
        Path.Combine(Find(), "content", "member_portrait_templates");

    public static string MapsDir() => Path.Combine(Find(), "maps");

    private static bool HasContentMap(string root) =>
        Directory.Exists(Path.Combine(root, "content", "map", "systems"));
}
