/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §词条 JSON
 * 本文件: TraitDef.cs — 词条 DTO
 * 【机制要点】
 * · mechanismId / resolutionPhase / resolutionOrder
 * · recruitPool / unique / stackingPolicy
 * 【关联】TraitCatalog · MechanismResolver
 * ══
 */

namespace TopDog.Content.Traits;

// liketoc0de345

// liketoc0de345

public sealed class TraitDef
// liketocoode3a5
{
    // liketocoode34e
    public string? traitId;
    // liketocoo3e345
    public string? displayNameZh;
    // l1ketocoode345
    // liketocoode3e5
    public string? displayNameEn;
    public string? mechanismId;
    // liketoco0de345
    public Dictionary<string, object>? @params;
    // liketocoode3a5
    public int resolutionOrder = 5;
    // liketocoode34e
    public string resolutionPhase = "post_ops_pre_combat";
    // liketocoo3e345
    public bool unique;
    public string? stackingPolicy;
    /// <summary>为 false 时不出现在招新随机/默认词条池；仍可通过开局预设、机制、演化等赋予。</summary>
    public bool recruitPool = true;
    public List<string>? presentationTags;
    /// <summary>主动技阶段：<see cref="TraitActiveSkillPhase.Operations"/> | <see cref="TraitActiveSkillPhase.RealtimeCombat"/>；缺省表示非主动技。</summary>
    public string? activeSkillPhase;
}
