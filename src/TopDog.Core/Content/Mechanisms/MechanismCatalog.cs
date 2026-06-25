using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

namespace TopDog.Content.Mechanisms;

public sealed class MechanismCatalog
{
    private readonly Dictionary<string, MechanismDef> _mechanisms = new(StringComparer.Ordinal);

    public static MechanismCatalog LoadDefault()
    {
        var cat = new MechanismCatalog();
        var dir = Path.Combine(AppRoot.Find(), "content", "mechanisms");
        if (!Directory.Exists(dir))
        {
            return cat;
        }
        foreach (var path in Directory.EnumerateFiles(dir, "mech_*.json"))
        {
            var def = JsonSerializer.Deserialize<MechanismDef>(File.ReadAllText(path), TopDogJson.Options);
            if (def?.mechanismId != null)
            {
                cat._mechanisms[def.mechanismId] = def;
            }
        }
        return cat;
    }

    public static MechanismCatalog Empty() => new();

    public MechanismDef? Find(string? mechanismId) =>
        mechanismId != null && _mechanisms.TryGetValue(mechanismId, out var m) ? m : null;

    public IReadOnlyDictionary<string, MechanismDef> All() => _mechanisms;
}
