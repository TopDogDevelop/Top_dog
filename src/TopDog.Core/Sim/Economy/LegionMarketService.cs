using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

/// <summary>军团内交易：现实人挂单 ↔ 军团收购（奉献折扣见 <see cref="DevotionTraitService"/>）。</summary>
public static class LegionMarketService
{
    public static int BuyerPrice(GameState state, TradeListing listing, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return 0;
        }
        return listing.priceStarCoin * quantity;
    }

    public static bool DevotionApplies(GameState state, TradeListing listing) =>
        listing.devotionListing;

    public static string BuyFromLegionListing(GameState state, string listingId, int quantity = 1)
    {
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            return "数量无效";
        }
        TradeListing? listing = TradeListingCatalog.FindLegionListing(state, listingId);
        if (listing == null || string.IsNullOrWhiteSpace(listing.itemId))
        {
            return "找不到军团内挂牌";
        }
        if (listing.quantity < quantity)
        {
            return "挂牌数量不足";
        }
        var itemId = listing.itemId!;
        if (CurrencyIds.IsCurrency(itemId))
        {
            return "不可通过挂牌购买星币";
        }
        if (!SellerCanFulfill(state, listing, itemId, quantity))
        {
            return "卖家库存不足";
        }

        var cost = BuyerPrice(state, listing, quantity);
        if (!MemberAssetService.TryDebitLegion(state, CurrencyIds.StarCoin, cost))
        {
            return "军团星币不足（需 " + cost + "）";
        }

        var legionStock = LegionRegistry.MutableLocalStock(state);
        legionStock[itemId] = legionStock.GetValueOrDefault(itemId, 0) + quantity;
        LegionRegistry.SyncLocalStockToLegacy(state);

        CreditSellerProceeds(state, listing, cost);
        DebitSellerItem(state, listing, itemId, quantity);

        listing.quantity -= quantity;
        if (listing.quantity <= 0)
        {
            state.market.legionListings.Remove(listing);
        }

        var name = MemberAssetService.ItemDisplayName(itemId, null, null);
        var devotionNote = DevotionApplies(state, listing)
            ? "（奉献挂牌）"
            : "";
        return "军团内购入 " + name + " ×" + quantity + " · 花费 " + cost + " 星币" + devotionNote;
    }

    public static bool SellerCanFulfill(GameState state, TradeListing listing, string itemId, int quantity)
    {
        var seller = FindSellerMember(state, listing.sellerId);
        if (seller == null)
        {
            return false;
        }
        var personal = MemberAssetService.PersonalStock(state, seller);
        return personal.GetValueOrDefault(itemId, 0) >= quantity;
    }

    public static void CreditSellerProceeds(GameState state, TradeListing listing, int paidTotal)
    {
        var seller = FindSellerMember(state, listing.sellerId);
        if (seller == null)
        {
            return;
        }
        var personal = MemberAssetService.PersonalStock(state, seller);
        personal[CurrencyIds.StarCoin] = personal.GetValueOrDefault(CurrencyIds.StarCoin, 0) + paidTotal;
    }

    public static void DebitSellerItem(
        GameState state,
        TradeListing listing,
        string itemId,
        int quantity)
    {
        var seller = FindSellerMember(state, listing.sellerId);
        if (seller == null)
        {
            return;
        }
        var personal = MemberAssetService.PersonalStock(state, seller);
        var have = personal.GetValueOrDefault(itemId, 0);
        if (have < quantity)
        {
            return;
        }
        var left = have - quantity;
        if (left <= 0)
        {
            personal.Remove(itemId);
        }
        else
        {
            personal[itemId] = left;
        }
    }

    public static MemberState? FindSellerMember(GameState state, string? sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return null;
        }
        foreach (var m in state.members)
        {
            if (sellerId.Equals(m.memberId, StringComparison.Ordinal)
                || sellerId.Equals(m.name, StringComparison.Ordinal)
                || sellerId.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }
}
