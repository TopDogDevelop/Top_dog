using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Sim.Economy;

/// <summary>军团内挂牌：奉献团员直接挂市场单价 ×0.25。</summary>
public static class LegionListingService
{
    public static TradeListing CreateLegionListing(
        GameState state,
        MemberState seller,
        string itemId,
        int quantity,
        ModuleRegistry? modules = null,
        ShipRegistry? ships = null,
        string? listingId = null)
    {
        var marketUnit = state.market.priceByItemId.GetValueOrDefault(
            itemId,
            AssetValuation.ItemStarCoinValue(itemId, ships, modules));
        var devotion = DevotionTraitService.MemberHasDevotion(state, seller);
        var unitPrice = devotion
            ? DevotionTraitService.PriceFromMarket(marketUnit)
            : Math.Max(1, (int)(marketUnit * 0.2));
        return new TradeListing
        {
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
}
