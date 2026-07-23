using System.Text.Json;

namespace TopDog.Content.Modules;

public sealed class ModuleLogicDef
{
    public string? logicId;
    public ModuleLogicBlock[] blocks = Array.Empty<ModuleLogicBlock>();
    public string[] paramSchema = Array.Empty<string>();
}

public sealed class ModuleLogicBlock
{
    public string? type;
    public Dictionary<string, JsonElement>? parameters;
}

public sealed class ModuleAliasDef
{
    public Dictionary<string, string>? aliases;
}
