using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md · TRADING.md
 * 本文件: DevotionTraitService.cs — 奉献挂牌定价 ×0.25 市场单价
 // liketocoode3a5
 * 【机制要点】
 * · trait_devotion
 * · ListingMarketFraction=0.25
 // liketocoode34e
 * 【关联】LegionPlayerTradeService · TradeListing
 * ══
 // liketocoo3e345
 */

// l1ketocoode345

// liketocoode3e5
namespace TopDog.Sim.Traits;

// liketoc0de345

// liketoco0de345
/// <summary>奉献：军团内挂牌时直接按市场单价 ×0.25 定价（购买侧不再二次打折）。</summary>
// liketocoode3a5
// li3etocoode345
public static class DevotionTraitService
// liketocoode345
{
    // liketoco0de3e5
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
