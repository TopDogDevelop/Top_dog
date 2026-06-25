namespace TopDog.Foundation.Io;

public static class AppRoot
{
    private static string? _cached;

    public static string Find()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var cwd = Directory.GetCurrentDirectory();
        if (HasContentMap(cwd))
        {
            _cached = cwd;
            return _cached;
        }

        var baseDir = AppContext.BaseDirectory;
        for (var dir = baseDir; dir != null; dir = Directory.GetParent(dir)?.FullName)
        {
            if (HasContentMap(dir))
            {
                _cached = dir;
                return _cached;
            }
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

    public static string MapsDir() => Path.Combine(Find(), "maps");

    private static bool HasContentMap(string root) =>
        Directory.Exists(Path.Combine(root, "content", "map", "systems"));
}
