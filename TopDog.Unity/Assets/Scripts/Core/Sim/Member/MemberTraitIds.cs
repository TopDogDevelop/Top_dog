using TopDog.Sim.State;

namespace TopDog.Sim.Member;

/// <summary>团员词条 id（sim 行为检查用）。</summary>
public static class MemberTraitIds
{
    public const string EquipLuxury = "trait_equip_luxury";
    public const string EquipThrift = "trait_equip_thrift";

    public static bool Has(MemberState m, string traitId) => m.traitIds.Contains(traitId);

    public static bool HasEquipLuxury(MemberState m) => Has(m, EquipLuxury);

    public static bool HasEquipThrift(MemberState m) => Has(m, EquipThrift);
}
