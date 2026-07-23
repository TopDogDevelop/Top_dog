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
    private readonly Dictionary<string, ModuleLogicDef> _logic = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _moduleSources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _logicSources = new(StringComparer.Ordinal);

// liketocoo3e345

    public static ModuleRegistry LoadDefault()
    // liketocoode3a5
    {
        // l1ketocoode345
        var reg = new ModuleRegistry();
        reg.LoadContentRoot(Path.Combine(AppRoot.Find(), "content", "modules"), "base");
        reg.ResolveAndValidate();
        return reg;
    }

    /// <summary>显式模式入口加载 overlay；overlay 不得覆盖基础通用 ID。</summary>
    public static ModuleRegistry LoadWithSkirmishOverlay()
    {
        var reg = new ModuleRegistry();
        reg.LoadContentRoot(Path.Combine(AppRoot.Find(), "content", "modules"), "base");
        reg.LoadContentRoot(SkirmishContentOverlay.Dir("modules"), "skirmish_overlay");
        reg.ResolveAndValidate();
        return reg;
    }

    private void LoadContentRoot(string dir, string layer)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        var paths = Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        foreach (var path in paths)
        {
            var name = Path.GetFileName(path);
            if (name.Equals("module_aliases.json", StringComparison.Ordinal))
            {
                LoadAliases(path);
                continue;
            }

            if (name.StartsWith("logic_", StringComparison.Ordinal))
            {
                var logic = Deserialize<ModuleLogicDef>(path);
                if (string.IsNullOrWhiteSpace(logic.logicId))
                {
                    throw new InvalidDataException($"Module logic missing logicId: {path}");
                }
                AddUnique(_logic, _logicSources, logic.logicId, logic, path, layer);
                continue;
            }

            if (!name.StartsWith("mod_", StringComparison.Ordinal)
                && !name.StartsWith("plug_", StringComparison.Ordinal))
            {
                continue;
            }

            var mod = Deserialize<ModuleDef>(path);
            if (string.IsNullOrWhiteSpace(mod.moduleId))
            {
                throw new InvalidDataException($"Module item missing moduleId: {path}");
            }
            AddUnique(_modules, _moduleSources, mod.moduleId, mod, path, layer);
        }
    }

    private static T Deserialize<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), TopDogJson.Options)
                   ?? throw new InvalidDataException($"Empty JSON object: {path}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON in {path}: {ex.Message}", ex);
        }
    }

    private static void AddUnique<T>(
        Dictionary<string, T> values,
        Dictionary<string, string> sources,
        string id,
        T value,
        string path,
        string layer)
    {
        if (sources.TryGetValue(id, out var previous))
        {
            throw new InvalidDataException(
                $"Duplicate common content id '{id}' in {previous} and {path} (layer {layer}).");
        }

        values.Add(id, value);
        sources.Add(id, path);
    }

    private void LoadAliases(string path)
    {
        var aliases = Deserialize<ModuleAliasDef>(path).aliases;
        if (aliases == null)
        {
            return;
        }

        foreach (var pair in aliases.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                throw new InvalidDataException($"Invalid module alias in {path}.");
            }
            if (!_aliases.TryAdd(pair.Key, pair.Value))
            {
                throw new InvalidDataException($"Duplicate module alias '{pair.Key}' in {path}.");
            }
        }
    }

    private void ResolveAndValidate()
    {
        foreach (var pair in _aliases)
        {
            if (!_modules.ContainsKey(pair.Value))
            {
                throw new InvalidDataException(
                    $"Module alias '{pair.Key}' references missing module '{pair.Value}'.");
            }
        }

        foreach (var module in _modules.Values.OrderBy(module => module.moduleId, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(module.logicId))
            {
                continue;
            }
            if (!_logic.TryGetValue(module.logicId, out var logic))
            {
                throw new InvalidDataException(
                    $"Module '{module.moduleId}' references missing logic '{module.logicId}'.");
            }

            module.resolvedLogic = logic;
            var supplied = module.@params ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            var schema = new HashSet<string>(logic.paramSchema ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (var key in supplied.Keys)
            {
                if (!schema.Contains(key))
                {
                    throw new InvalidDataException(
                        $"Module '{module.moduleId}' parameter '{key}' is not declared by '{logic.logicId}'.");
                }
            }
            foreach (var required in schema)
            {
                if (!supplied.ContainsKey(required))
                {
                    throw new InvalidDataException(
                        $"Module '{module.moduleId}' is missing required parameter '{required}' for '{logic.logicId}'.");
                }
            }
        }
    }

    public static ModuleRegistry Empty() => new();

    public ModuleDef? Find(string? moduleId) =>
        moduleId != null && _modules.TryGetValue(ResolveAlias(moduleId), out var m) ? m : null;

    public string ResolveAlias(string moduleId)
    {
        var current = moduleId;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (_aliases.TryGetValue(current, out var next))
        {
            if (!seen.Add(current))
            {
                throw new InvalidDataException($"Module alias cycle detected at '{current}'.");
            }
            current = next;
        }
        return current;
    }

    public bool IsKnownModule(string? moduleId) => Find(moduleId) != null;

    public ModuleDef? Resolve(string? moduleId) => ModuleCatalog.Resolve(this, moduleId);

    public IReadOnlyDictionary<string, ModuleDef> All() => _modules;
    public IReadOnlyDictionary<string, ModuleLogicDef> AllLogic() => _logic;
    public string? SourceOf(string moduleId) =>
        _moduleSources.TryGetValue(moduleId, out var source) ? source : null;

    public static string Bilingual(ModuleDef? m) => ModuleCatalog.BilingualLabel(m);
}
