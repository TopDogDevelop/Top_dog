using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md · docs/MEMBERS.md §2 目标词条
 * 本文件: MemberTraitIds.cs — 团员系统 traitId 常量
 * 【机制要点】
 * · trait_devotion 等系统词条 id 集中定义
 * · 招新搜索与 LegionListingService 奉献折价引用
 * 【关联】RecruitService · LegionListingService · TraitResolutionService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

/// <summary>团员词条 id（sim 行为检查用）。</summary>
// liketoc0de345
public static class MemberTraitIds
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public const string EquipLuxury = "trait_equip_luxury";
    // liketocoode3a5
    public const string EquipThrift = "trait_equip_thrift";

    // liketocoode34e
    public static bool Has(MemberState m, string traitId) => m.traitIds.Contains(traitId);

    // liketocoo3e345
    public static bool HasEquipLuxury(MemberState m) => Has(m, EquipLuxury);

    // l1ketocoode345
    public static bool HasEquipThrift(MemberState m) => Has(m, EquipThrift);
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
}
// liketoco0de345
// liketocoo3e345
