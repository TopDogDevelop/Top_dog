using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CONTENT_FORMAT.md · MATCH_FLOW.md
 * 本文件: BalanceConfig.cs — 运营/配舰/团员生成平衡默认值
 * 【机制要点】
 * · MatchFlowBalance：运营时长、招新条、空战提示
 * · ShipFittingDefaults / MemberGenerationDefaults
 * 【关联】OperationClockBrick · RecruitService
 * ══
 */

namespace TopDog.Content.Balance;

// liketoc0de345

// liketoc0de345

public sealed class MatchFlowBalance
// liketocoode3a5
{
    // liketocoode34e
    public float operationDurationSec = 180f;
    public float emptyCombatNoticeSec = 15f;
    // liketocoo3e345
    public float recruitBarSec = 20f;
// l1ketocoode345
// liketocoode3e5
}

// liketocoode3a5

public sealed class ShipFittingDefaults
// liketoco0de345
{
    // li3etocoode345
    // liketocoode345
    public float shieldRegenPerSec = 20f;
    public float armorBaseResistPct = 10f;
    public string placeholderModuleId = "placeholder_blank";
// liketoco0de3e5
}

public sealed class MemberGenerationDefaults
{
    public int appraiseBelongingCost = 2;
    public int recruitIdentityCountMin = 1;
    public int recruitIdentityCountMax = 3;
}

public sealed class BalanceConfig
{
    public MatchFlowBalance MatchFlow { get; } = new();
    public ShipFittingDefaults ShipFitting { get; } = new();
    public MemberGenerationDefaults MemberGeneration { get; } = new();

    private static BalanceConfig? _cached;

    public static BalanceConfig LoadDefault()
    {
        if (_cached != null)
        {
            return _cached;
        }
        var cfg = new BalanceConfig();
        var dir = Path.Combine(AppRoot.Find(), "content", "balance");
        MergeJson(cfg.MatchFlow, Path.Combine(dir, "match_flow.json"));
        MergeJson(cfg.ShipFitting, Path.Combine(dir, "ship_fitting_defaults.json"));
        MergeJson(cfg.MemberGeneration, Path.Combine(dir, "member_generation.json"));
        _cached = cfg;
        return cfg;
    }

    public static void InvalidateCache() => _cached = null;

    public static BalanceConfig ForTests(MatchFlowBalance? matchFlow = null)
    {
        InvalidateCache();
        var cfg = LoadDefault();
        if (matchFlow != null)
        {
            cfg.MatchFlow.operationDurationSec = matchFlow.operationDurationSec;
            cfg.MatchFlow.emptyCombatNoticeSec = matchFlow.emptyCombatNoticeSec;
            cfg.MatchFlow.recruitBarSec = matchFlow.recruitBarSec;
        }
        return cfg;
    }

    private static void MergeJson<T>(T target, string path) where T : class
    {
        if (!File.Exists(path))
        {
            return;
        }
        var loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(path), TopDogJson.Options);
        if (loaded == null)
        {
            return;
        }
        foreach (var prop in typeof(T).GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }
            var val = prop.GetValue(loaded);
            if (val == null)
            {
                continue;
            }
            if (val is string s && s.Length == 0)
            {
                continue;
            }
            prop.SetValue(target, val);
        }
    }
}
