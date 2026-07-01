using System.Text.Json;
using TopDog.Foundation.Io;
using TopDog.Foundation.Json;

namespace TopDog.Content.Balance;

public sealed class SkirmishScaleConfig
{
    public int rosterCap;
    public float legionFortressHpMultiplier = 1f;
    public float personalFortressStructureHp = 100_000f;
}

public sealed class SkirmishBalanceConfig
{
    public float matchDurationSec = 3600f;
    public float respawnCooldownSec = 300f;
    public float spawnRadiusM = 100_000f;
    public float legionFortressBaseStructureHp = 10_000_000f;
    public float eventRegionSpacingAu = 5f;
    public float planetDistanceAu = 5f;
    public Dictionary<string, SkirmishScaleConfig> scales = new(StringComparer.Ordinal);
    public Dictionary<string, int> tonnageScore = new(StringComparer.Ordinal);

    private static SkirmishBalanceConfig? _cached;

    public static SkirmishBalanceConfig LoadDefault()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var path = Path.Combine(AppRoot.Find(), "content", "balance", "skirmish.json");
        if (!File.Exists(path))
        {
            _cached = new SkirmishBalanceConfig();
            return _cached;
        }

        var json = File.ReadAllText(path);
        _cached = JsonSerializer.Deserialize<SkirmishBalanceConfig>(json, TopDogJson.Options) ?? new SkirmishBalanceConfig();
        return _cached;
    }

    public static void InvalidateCache() => _cached = null;

    public SkirmishScaleConfig ResolveScale(int scale)
    {
        var key = scale.ToString();
        if (scales.TryGetValue(key, out var cfg))
        {
            return cfg;
        }

        return new SkirmishScaleConfig
        {
            rosterCap = scale,
            legionFortressHpMultiplier = scale / 100f,
            personalFortressStructureHp = scale * 1000f,
        };
    }

    public int ScoreForTonnage(string? tonnageClass)
    {
        if (tonnageClass != null && tonnageScore.TryGetValue(tonnageClass, out var score))
        {
            return score;
        }

        return 0;
    }
}
