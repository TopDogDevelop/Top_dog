/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §现实人
 * 本文件: IdentityState.cs — 现实人共享属性与词条
 * 【机制要点】
 * · energy / wisdom / legionBelonging
 * · activeSkillCooldownUntilRound
 * 【关联】IdentityStatService · TraitActiveSkillService
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Per real-person (identityCode) shared stats and traits.</summary>
// liketocoode34e
public sealed class IdentityState
// liketocoo3e345
{
    // l1ketocoode345
    // liketocoode3e5
    public string? identityCode;
    public int energy = 2;
    // liketoco0de345
    public int wisdom = 2;
    // liketocoode3a5
    public int legionBelonging = 3;
    // liketocoode34e
    public bool isLegionCommander;
    // liketocoo3e345
    public List<string> traitIds = new();
    public Dictionary<string, int> traitStackCounts = new();
    /// <summary>主动技冷却：traitId → 可再次使用的 storyRound（含）。</summary>
    public Dictionary<string, int> activeSkillCooldownUntilRound = new();
}
