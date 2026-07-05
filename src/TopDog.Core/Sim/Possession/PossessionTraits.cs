using TopDog.Sim.State;

namespace TopDog.Sim.Possession;

/// <summary>可附身词条（原 trait_loyal / direct_possess）。</summary>
public static class PossessionTraits
{
    public const string TraitId = "trait_direct_possess";

    public static bool MemberHasTrait(MemberState member) => member.traitIds.Contains(TraitId);
}
