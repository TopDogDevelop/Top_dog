using System.Text.Json;
using System.Text.Json.Serialization;
using TopDog.Foundation.Io;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_INDEX.md · docs/MECHANISM_TEST_SCENARIOS.md
 * 本文件: MechanismTestScenarioDef.cs — 机制详测场景 JSON DTO
 * 【机制要点】
 * · scenarioOrder：战役列表 #01–#10 排序（StoryLevelCatalog）
 * · mapMode：nav_rally / intra_scene_warp；缺省单矿带对阵
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public sealed class MechanismTestScenarioDef
{
    public string scenarioId = "";
    public string displayName = "";
    /// <summary>关卡列表排序序号（1-based）。</summary>
    public int scenarioOrder;
    public int seed;
    public float spawnSeparationM = 20_000f;
    /// <summary>nav_rally / intra_scene_warp；默认单矿带对阵。</summary>
    public string? mapMode;
    public List<MechanismTestLegionDef> legions = new();
}

public sealed class MechanismTestLegionDef
{
    public string legionId = "";
    public string displayName = "";
    public bool isPlayer;
    public bool isAiControlled = true;
    public List<MechanismTestMemberDef> members = new();
    public MechanismTestExpandDef? expandTemplate;
}

public sealed class MechanismTestMemberDef
{
    public string memberTemplateId = "";
    public string memberTemplateRowId = "";
    public string memberId = "";
    public string displayName = "";
    public string hullId = "";
    public Dictionary<string, string> fitted = new(StringComparer.Ordinal);
}

public sealed class MechanismTestExpandDef
{
    public string templateId = "";
    public string identityCode = "";
    public string defaultHullId = "";
    public Dictionary<string, string> defaultFitted = new(StringComparer.Ordinal);
    public List<MechanismTestMemberDef> overrides = new();
    public List<string> excludeDisplayNames = new();
    public List<string> randomRepairModules = new();
    /// <summary>randomRepairModules 写入槽位；默认 fn_1（远程维修关），场域详测用 atk_1。</summary>
    public string randomRepairSlotKey = "fn_1";
}

public static class MechanismTestCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string ScenariosDir =>
        Path.Combine(AppRoot.Find(), "content", "mechanism_tests", "scenarios");

    public static IReadOnlyList<MechanismTestScenarioDef> ListAll()
    {
        var dir = ScenariosDir;
        if (!Directory.Exists(dir))
        {
            return Array.Empty<MechanismTestScenarioDef>();
        }

        var list = new List<MechanismTestScenarioDef>();
        foreach (var file in Directory.GetFiles(dir, "mt_*.json"))
        {
            if (TryLoadFile(file, out var scenario))
            {
                list.Add(scenario!);
            }
        }

        list.Sort((a, b) =>
        {
            var order = a.scenarioOrder.CompareTo(b.scenarioOrder);
            return order != 0
                ? order
                : string.Compare(a.scenarioId, b.scenarioId, StringComparison.Ordinal);
        });
        return list;
    }

    public static bool TryGet(string scenarioId, out MechanismTestScenarioDef scenario)
    {
        scenario = null!;
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            return false;
        }

        var path = Path.Combine(ScenariosDir, scenarioId + ".json");
        if (!File.Exists(path))
        {
            return false;
        }

        return TryLoadFile(path, out scenario);
    }

    private static bool TryLoadFile(string path, out MechanismTestScenarioDef? scenario)
    {
        scenario = null;
        try
        {
            var json = File.ReadAllText(path);
            scenario = JsonSerializer.Deserialize<MechanismTestScenarioDef>(json, JsonOptions);
            return scenario?.scenarioId != null;
        }
        catch
        {
            return false;
        }
    }
}
