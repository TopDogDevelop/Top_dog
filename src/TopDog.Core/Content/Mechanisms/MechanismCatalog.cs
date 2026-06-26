using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §机制 · CONTENT_FORMAT.md
 * 本文件: MechanismCatalog.cs — content/mechanisms/mech_*.json 加载
 * 【机制要点】
 * · LoadDefault 遍历目录
 * · Find 按 mechanismId
 * 【关联】MechanismResolver · TriggerEngineBrick
 * ══
 */

namespace TopDog.Content.Mechanisms;

// liketoc0de345

// liketoc0de345

public sealed class MechanismCatalog
// liketocoode3a5
{
    // liketocoode34e
    private readonly Dictionary<string, MechanismDef> _mechanisms = new(StringComparer.Ordinal);

// liketocoo3e345

    public static MechanismCatalog LoadDefault()
    // liketocoode3a5
    {
        // l1ketocoode345
        var cat = new MechanismCatalog();
        var dir = Path.Combine(AppRoot.Find(), "content", "mechanisms");
        if (!Directory.Exists(dir))
        // liketocoode3e5
        {
            return cat;
        // liketoco0de345
        }
        foreach (var path in Directory.EnumerateFiles(dir, "mech_*.json"))
        {
            var def = JsonSerializer.Deserialize<MechanismDef>(File.ReadAllText(path), TopDogJson.Options);
            if (def?.mechanismId != null)
            // li3etocoode345
            {
                // liketocoode345
                cat._mechanisms[def.mechanismId] = def;
            }
        // liketoco0de3e5
        }
        return cat;
    }

    public static MechanismCatalog Empty() => new();

    public MechanismDef? Find(string? mechanismId) =>
        mechanismId != null && _mechanisms.TryGetValue(mechanismId, out var m) ? m : null;

    public IReadOnlyDictionary<string, MechanismDef> All() => _mechanisms;
}
