using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

namespace TopDog.Content.Modules;

public sealed class ModuleRegistry
{
    private readonly Dictionary<string, ModuleDef> _modules = new(StringComparer.Ordinal);

    public static ModuleRegistry LoadDefault()
    {
        var reg = new ModuleRegistry();
        var dir = Path.Combine(AppRoot.Find(), "content", "modules");
        if (!Directory.Exists(dir))
        {
            return reg;
        }
        foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
        {
            var name = Path.GetFileName(path);
            if (!name.StartsWith("mod_", StringComparison.Ordinal) && !name.StartsWith("plug_", StringComparison.Ordinal))
            {
                continue;
            }
            var mod = JsonSerializer.Deserialize<ModuleDef>(File.ReadAllText(path), TopDogJson.Options);
            if (mod?.moduleId != null)
            {
                reg._modules[mod.moduleId] = mod;
            }
        }
        return reg;
    }

    public static ModuleRegistry Empty() => new();

    public ModuleDef? Find(string? moduleId) =>
        moduleId != null && _modules.TryGetValue(moduleId, out var m) ? m : null;

    public bool IsKnownModule(string? moduleId) => Find(moduleId) != null;

    public ModuleDef? Resolve(string? moduleId) => ModuleCatalog.Resolve(this, moduleId);

    public IReadOnlyDictionary<string, ModuleDef> All() => _modules;

    public static string Bilingual(ModuleDef? m) => ModuleCatalog.BilingualLabel(m);
}
