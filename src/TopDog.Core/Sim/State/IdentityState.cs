namespace TopDog.Sim.State;

/// <summary>Per real-person (identityCode) shared stats and traits.</summary>
public sealed class IdentityState
{
    public string? identityCode;
    public int energy = 2;
    public int wisdom = 2;
    public int legionBelonging = 3;
    public bool isLegionCommander;
    public List<string> traitIds = new();
    public Dictionary<string, int> traitStackCounts = new();
    /// <summary>主动技冷却：traitId → 可再次使用的 storyRound（含）。</summary>
    public Dictionary<string, int> activeSkillCooldownUntilRound = new();
}
