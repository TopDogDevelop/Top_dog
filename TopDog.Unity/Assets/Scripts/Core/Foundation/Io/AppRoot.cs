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

    private static readonly List<string> ExtraMapsRoots = new();

    /// <summary>Additional folders that contain *.topdog-map packages (e.g. StreamingAssets/maps).</summary>
    public static void RegisterMapsRoot(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        var full = Path.GetFullPath(root);
        if (!Directory.Exists(full))
        {
            return;
        }

        for (var i = 0; i < ExtraMapsRoots.Count; i++)
        {
            if (string.Equals(ExtraMapsRoots[i], full, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        ExtraMapsRoots.Add(full);
    }

    public static void ClearExtraMapsRoots() => ExtraMapsRoots.Clear();

    /// <summary>Primary MapsDir plus any registered extras (deduped).</summary>
    public static IReadOnlyList<string> MapsDirs()
    {
        var list = new List<string>();
        void Add(string? p)
        {
            if (string.IsNullOrWhiteSpace(p) || !Directory.Exists(p))
            {
                return;
            }

            var full = Path.GetFullPath(p);
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], full, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            list.Add(full);
        }

        Add(MapsDir());
        foreach (var e in ExtraMapsRoots)
        {
            Add(e);
        }

        return list;
    }

    private static bool HasContentMap(string root) =>
        Directory.Exists(Path.Combine(root, "content", "map", "systems"));
}
