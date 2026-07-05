/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §主动技能 · docs/VIP_TRAIT_DESIGN.md
 * 本文件: TraitActiveSkillPhase.cs — 词条主动技阶段标签
 * 【机制要点】
 * · operations：运营/交战准备（团员详情、运营壳层）
 * · realtime_combat：实时战场（战术物体菜单；须 combatRealtimeActive）
 * 【关联】TraitDef.activeSkillPhase · TraitActiveSkillService
 * ══
 */

namespace TopDog.Content.Traits;

/// <summary>词条主动技可用阶段（Trait JSON <c>activeSkillPhase</c>）。</summary>
public static class TraitActiveSkillPhase
{
    /// <summary>运营阶段主动技（如策划支援）。</summary>
    public const string Operations = "operations";

    /// <summary>实时战场主动技（如董事会召来）。</summary>
    public const string RealtimeCombat = "realtime_combat";
}
