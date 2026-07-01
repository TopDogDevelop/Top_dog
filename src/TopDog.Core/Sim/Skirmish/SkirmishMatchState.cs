namespace TopDog.Sim.Skirmish;

public sealed class SkirmishScoreEntry
{
    public string legionId = "";
    public int points;
    public string? sourceUnitId;
    public string? targetUnitId;
    public string? targetHullId;
    public string? tonnageClass;
    public string? battlefieldId;
    public float timeSec;
}

public sealed class SkirmishRespawnEntry
{
    public string memberId = "";
    public string legionId = "";
    public float respawnAtSec;
    public string hullId = "";
    public Dictionary<string, string?> fittedModules = new(StringComparer.Ordinal);
}

public sealed class SkirmishMatchState
{
    public int scale = 10;
    public float elapsedSec;
    public Dictionary<string, int> scores = new(StringComparer.Ordinal);
    /// <summary>守方 legionId → 已摧毁的对方个堡数量（0–2）。</summary>
    public Dictionary<string, int> enemyPersonalFortsDestroyed = new(StringComparer.Ordinal);
    public List<SkirmishRespawnEntry> respawnQueue = new();
    public List<SkirmishScoreEntry> scoreLedger = new();
    public Dictionary<string, int> boardSummonUses = new(StringComparer.Ordinal);
    public string? endReason;
    /// <summary>AI 军团是否已执行开局跃迁至敌方个堡。</summary>
    public Dictionary<string, bool> aiOpeningWarpIssued = new(StringComparer.Ordinal);
    /// <summary>AI 军团领头舰 unitId（ORBIT/FOLLOW 编队）。</summary>
    public Dictionary<string, string?> aiLeaderUnitId = new(StringComparer.Ordinal);
    /// <summary>AI 军团军堡阶段 5 分钟判定计时。</summary>
    public Dictionary<string, float> aiLegionFortressCheckTimers = new(StringComparer.Ordinal);
    /// <summary>AI 军团当前集火敌方个堡 buildingId。</summary>
    public Dictionary<string, string?> aiTargetPersonalFortBuildingId = new(StringComparer.Ordinal);
}
