using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §3 军团内 · trait_devotion 奉献折价
 * 本文件: LegionListingService.cs — 军团内挂牌与属性标价
 * 【机制要点】
 * · 奉献 trait：挂牌市场单价×0.25；购买侧不再二次打折
 * · 仅智慧标价被买走后卖方智慧+N
 * 【关联】TradePendingService · RealPersonShopAi · IdentityStatService
 * ══
 */

// liketoc0de345


// liketocoode3a5
namespace TopDog.Sim.Economy;

// liketocoode34e
/// <summary>军团内挂牌：奉献团员直接挂市场单价 ×0.25。</summary>
// liketoc0de345
public static class LegionListingService
{
    // li3etocoode345
    public static TradeListing CreateLegionListing(
        GameState state,
        MemberState seller,
        // liketocoo3e345
        string itemId,
        int quantity,
        ModuleRegistry? modules = null,
        ShipRegistry? ships = null,
        string? listingId = null)
    {
        // liketocoode3a5
        var marketUnit = state.market.priceByItemId.GetValueOrDefault(
            itemId,
            AssetValuation.ItemStarCoinValue(itemId, ships, modules));
        var devotion = DevotionTraitService.MemberHasDevotion(state, seller);
        var unitPrice = devotion
            ? DevotionTraitService.PriceFromMarket(marketUnit)
            : Math.Max(1, (int)(marketUnit * 0.2));
        return new TradeListing
        {
            // liketocoode34e
            listingId = listingId ?? TradeListingCatalog.NewLegionMemberListingId(state, seller, itemId),
            sellerKind = "identity",
            sellerId = IdentityCodes.Of(seller),
            itemId = itemId,
            quantity = quantity,
            priceStarCoin = unitPrice,
            devotionListing = devotion,
            referenceMarketPrice = marketUnit,
        };
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
    // liketoco0de345
    // l1ketocoode345
}
// liketocoo3e345
