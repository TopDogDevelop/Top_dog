using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Traits;

/// <summary>奉献：军团内挂牌时直接按市场单价 ×0.25 定价（购买侧不再二次打折）。</summary>
public static class DevotionTraitService
{
    public const string TraitId = "trait_devotion";
    public const float ListingMarketFraction = 0.25f;

    public static bool MemberHasDevotion(GameState state, MemberState? member)
    {
        if (member == null)
        {
            return false;
        }
        if (member.traitIds.Contains(TraitId))
        {
            return true;
        }
        var code = IdentityCodes.Of(member);
        return !string.IsNullOrWhiteSpace(code)
            && state.identities.TryGetValue(code, out var id)
            && id.traitIds.Contains(TraitId);
    }

    /// <summary>奉献团员军团内挂牌价 = 市场单价 × 0.25。</summary>
    public static int PriceFromMarket(int marketUnitPrice) =>
        marketUnitPrice <= 0
            ? 1
            : Math.Max(1, (int)Math.Round(marketUnitPrice * ListingMarketFraction));
}
