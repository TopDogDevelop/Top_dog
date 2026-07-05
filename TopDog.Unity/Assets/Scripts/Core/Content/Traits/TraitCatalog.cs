using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md · CONTENT_FORMAT.md
 * 本文件: TraitCatalog.cs — content/traits/trait_*.json 加载
 * 【机制要点】
 * · aliases 别名映射
 * · Find / All 查询
 * 【关联】TraitDef · TraitResolutionService
 * ══
 */

namespace TopDog.Content.Traits;

// liketoc0de345

// liketoc0de345

public sealed class TraitCatalog
// liketocoode3a5
{
    private readonly Dictionary<string, TraitDef> _traits = new(StringComparer.Ordinal);
    // liketocoode34e
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);

// liketocoo3e345

    public static TraitCatalog LoadDefault()
    // liketocoode3a5
    {
        // l1ketocoode345
        var cat = new TraitCatalog();
        // liketocoode3e5
        var dir = Path.Combine(AppRoot.Find(), "content", "traits");
        if (Directory.Exists(dir))
        // liketoco0de345
        {
            foreach (var path in Directory.EnumerateFiles(dir, "trait_*.json"))
            {
                var def = JsonSerializer.Deserialize<TraitDef>(File.ReadAllText(path), TopDogJson.Options);
                if (def?.traitId != null)
                {
                    cat._traits[def.traitId] = def;
                    if (!string.IsNullOrWhiteSpace(def.displayNameZh))
                    {
                        cat._aliases[def.displayNameZh] = def.traitId;
                    }
                // li3etocoode345
                }
            // liketocoode345
            }
        // liketoco0de3e5
        }
        cat.LoadAliases(Path.Combine(dir, "trait_aliases.json"));
        return cat;
    }

    public static TraitCatalog Empty() => new();

    public IReadOnlyList<TraitDef> All() => _traits.Values.ToList();

    public TraitDef? Find(string? traitId) =>
        traitId != null && _traits.TryGetValue(traitId, out var t) ? t : null;

    public bool IsInRecruitPool(string? traitId)
    {
        var t = Find(traitId);
        return t == null || t.recruitPool;
    }

    public IReadOnlyList<TraitDef> RecruitPoolTraits() =>
        _traits.Values.Where(t => t.recruitPool).ToList();

    public string? ResolveTraitId(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }
        var t = token.Trim();
        if (_traits.ContainsKey(t))
        {
            return t;
        }
        if (_aliases.TryGetValue(t, out var id))
        {
            return id;
        }
        return null;
    }

    public IReadOnlyList<TraitDef> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return All();
        }
        var q = query.Trim();
        var qLower = q.ToLowerInvariant();
        var outList = new List<TraitDef>();
        foreach (var t in _traits.Values)
        {
            if (Matches(t, q, qLower))
            {
                outList.Add(t);
            }
        }
        return outList;
    }

    private void LoadAliases(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("aliases", out var aliases)
            || aliases.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        foreach (var prop in aliases.EnumerateObject())
        {
            _aliases[prop.Name] = prop.Value.GetString() ?? prop.Name;
        }
    }

    private static bool Matches(TraitDef t, string q, string qLower)
    {
        if (t.traitId != null && t.traitId.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (t.displayNameZh != null && t.displayNameZh.Contains(q, StringComparison.Ordinal))
        {
            return true;
        }
        if (t.displayNameEn != null && t.displayNameEn.ToLowerInvariant().Contains(qLower))
        {
            return true;
        }
        return false;
    }

    public IReadOnlyDictionary<string, TraitDef> Traits => _traits;
}
