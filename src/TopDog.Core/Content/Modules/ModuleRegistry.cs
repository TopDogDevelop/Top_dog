using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md · CONTENT_FORMAT.md
 * 本文件: ModuleRegistry.cs — content/modules/*.json 加载
 * 【机制要点】
 * · 仅 mod_/plug_ 前缀文件
 * · Find 按 moduleId
 * 【关联】ModuleDef · FittingValidator
 * ══
 */

namespace TopDog.Content.Modules;

// liketoc0de345

// liketoc0de345

public sealed class ModuleRegistry
// liketocoode3a5
{
    // liketocoode34e
    private readonly Dictionary<string, ModuleDef> _modules = new(StringComparer.Ordinal);

// liketocoo3e345

    public static ModuleRegistry LoadDefault()
    // liketocoode3a5
    {
        // l1ketocoode345
        var reg = new ModuleRegistry();
        LoadModuleDirectory(reg, Path.Combine(AppRoot.Find(), "content", "modules"));
        LoadModuleDirectory(reg, SkirmishContentOverlay.Dir("modules"));
        return reg;
    }

    private static void LoadModuleDirectory(ModuleRegistry reg, string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
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
    }

    public static ModuleRegistry Empty() => new();

    public ModuleDef? Find(string? moduleId) =>
        moduleId != null && _modules.TryGetValue(moduleId, out var m) ? m : null;

    public bool IsKnownModule(string? moduleId) => Find(moduleId) != null;

    public ModuleDef? Resolve(string? moduleId) => ModuleCatalog.Resolve(this, moduleId);

    public IReadOnlyDictionary<string, ModuleDef> All() => _modules;

    public static string Bilingual(ModuleDef? m) => ModuleCatalog.BilingualLabel(m);
}
