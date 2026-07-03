using TopDog.Foundation.Io;
using TopDog.Sim.State;

namespace TopDog.Content.Members;

/// <summary>
/// Scans <c>content/member_portrait_templates/</c> for portrait images used by procedural recruit.
/// Any image file in the folder tree (any subfolder) may be picked at random.
/// </summary>
public static class MemberPortraitCatalog
{
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga" };

    private static readonly object Gate = new();
    private static List<string> _poolRefs = new();
    private static readonly List<string> ExtraScanRoots = new();

    public static void RegisterScanRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        var full = Path.GetFullPath(root.Trim());
        lock (Gate)
        {
            if (!ExtraScanRoots.Contains(full, StringComparer.OrdinalIgnoreCase))
            {
                ExtraScanRoots.Add(full);
            }
        }
    }

    public static void ClearExtraScanRoots()
    {
        lock (Gate)
        {
            ExtraScanRoots.Clear();
        }
    }

    public static void Refresh()
    {
        lock (Gate)
        {
            _poolRefs = ScanPool();
        }
    }

    public static IReadOnlyList<string> PoolRefs()
    {
        lock (Gate)
        {
            if (_poolRefs.Count == 0)
            {
                _poolRefs = ScanPool();
            }

            return _poolRefs;
        }
    }

    /// <summary>Random relative ref under member_portrait_templates, or null when pool empty.</summary>
    public static string? RollRandomRef(Random rng)
    {
        var pool = PoolRefs();
        if (pool.Count == 0)
        {
            return null;
        }

        return pool[rng.Next(pool.Count)];
    }

    public static string? RollSecondGalaxyRef(Random rng) => RollRandomRef(rng);

    public static string? Resolve(MemberState? member)
    {
        if (member == null)
        {
            return null;
        }

        var fromRef = ResolveRef(member.portraitRef);
        if (fromRef != null)
        {
            return fromRef;
        }

        if (string.Equals(member.source, "preset", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(member.identityCode))
        {
            return ResolveRef(member.identityCode);
        }

        return null;
    }

    public static string? ResolveRef(string? portraitRef)
    {
        if (string.IsNullOrWhiteSpace(portraitRef))
        {
            return null;
        }

        var trimmed = portraitRef.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(trimmed) && File.Exists(trimmed))
        {
            return Path.GetFullPath(trimmed);
        }

        var root = AppRoot.Find();
        foreach (var templatesRoot in TemplateRoots())
        {
            var hit = ResolveUnderRoot(templatesRoot, trimmed);
            if (hit != null)
            {
                return hit;
            }
        }

        var legacyCandidates = new List<string>
        {
            Path.Combine(root, trimmed),
            Path.Combine(root, "core", "assets", "raster", "members", trimmed),
            Path.Combine(root, "core", "assets", "raster", "members", trimmed + ".png"),
        };

        if (!trimmed.Contains('/', StringComparison.Ordinal) && !trimmed.Contains('.', StringComparison.Ordinal))
        {
            legacyCandidates.Add(Path.Combine(root, "core", "assets", "raster", "members", trimmed + ".png"));
        }

        foreach (var path in legacyCandidates)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }

    private static string? ResolveUnderRoot(string templatesRoot, string trimmed)
    {
        var candidates = new List<string> { Path.Combine(templatesRoot, trimmed) };

        if (!trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && !trimmed.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !trimmed.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            && !trimmed.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var ext in ImageExtensions)
            {
                candidates.Add(Path.Combine(templatesRoot, trimmed + ext));
            }
        }

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }

    private static List<string> ScanPool()
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var templatesRoot in TemplateRoots())
        {
            if (!Directory.Exists(templatesRoot))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(templatesRoot, "*", SearchOption.AllDirectories))
            {
                if (!IsImage(file))
                {
                    continue;
                }

                refs.Add(ToRef(templatesRoot, file));
            }
        }

        var outList = refs.ToList();
        outList.Sort(StringComparer.OrdinalIgnoreCase);
        return outList;
    }

    private static List<string> TemplateRoots()
    {
        var roots = ScanTemplateRoots();
        lock (Gate)
        {
            foreach (var extra in ExtraScanRoots)
            {
                if (!roots.Contains(extra, StringComparer.OrdinalIgnoreCase))
                {
                    roots.Add(extra);
                }
            }
        }

        return roots;
    }

    private static List<string> ScanTemplateRoots()
    {
        var roots = new List<string> { AppRoot.MemberPortraitTemplatesDir() };

        var env = Environment.GetEnvironmentVariable("TOPDOG_PORTRAIT_POOL");
        if (!string.IsNullOrWhiteSpace(env))
        {
            roots.Add(Path.GetFullPath(env.Trim()));
        }

        var parent = Directory.GetParent(AppRoot.Find())?.FullName;
        if (!string.IsNullOrEmpty(parent))
        {
            var sibling = Path.Combine(parent, "content", "member_portrait_templates");
            roots.Add(sibling);
        }

        return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsImage(string path)
    {
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)
            || name.Equals("README.md", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var ext = Path.GetExtension(name);
        return ImageExtensions.Any(suffix => ext.Equals(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static string ToRef(string templatesRoot, string file)
    {
        var rel = Path.GetRelativePath(templatesRoot, file).Replace('\\', '/');
        return rel;
    }
}
