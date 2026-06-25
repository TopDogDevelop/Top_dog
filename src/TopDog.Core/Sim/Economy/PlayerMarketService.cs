using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

/// <summary>跨军团玩家间挂牌（经交换中心撮合）。</summary>
public static class PlayerMarketService
{
    public static string ListFromLegionStock(
        GameState state,
        string legionId,
        string itemId,
        int quantity = 1,
        ModuleRegistry? modules = null,
        ShipRegistry? ships = null)
    {
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            return "数量无效";
        }

        if (CurrencyIds.IsCurrency(itemId))
        {
            return "不可挂牌星币";
        }

        var local = LegionRegistry.Local(state);
        if (local == null || !legionId.Equals(local.legionId, StringComparison.Ordinal))
        {
            return "仅本地军团可挂牌至玩家间市场";
        }

        var stock = LegionRegistry.MutableLocalStock(state);
        var have = stock.GetValueOrDefault(itemId, 0);
        if (have < quantity)
        {
            return "军团库存不足: " + itemId;
        }

        stock[itemId] = have - quantity;
        if (stock[itemId] <= 0)
        {
            stock.Remove(itemId);
        }
        LegionRegistry.SyncLocalStockToLegacy(state);

        var unitPrice = state.market.priceByItemId.GetValueOrDefault(
            itemId,
            AssetValuation.ItemStarCoinValue(itemId, ships, modules));
        var listing = new TradeListing
        {
            listingId = TradeListingCatalog.NewPlayerMarketListingId(state, legionId, itemId),
            sellerKind = "player",
            sellerId = legionId,
            itemId = itemId,
            quantity = quantity,
            priceStarCoin = Math.Max(1, unitPrice),
        };
        state.market.playerListings.Add(listing);

        var name = MemberAssetService.ItemDisplayName(itemId, modules, ships);
        return "玩家间挂牌 " + name + " ×" + quantity + " · 单价 " + listing.priceStarCoin + " 星币";
    }

    public static string BuyFromPlayerListing(GameState state, string listingId, int quantity = 1)
    {
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            return "数量无效";
        }

        var listing = TradeListingCatalog.FindPlayerListing(state, listingId);
        if (listing == null || string.IsNullOrWhiteSpace(listing.itemId))
        {
            return "找不到玩家挂牌";
        }

        if (listing.quantity < quantity)
        {
            return "挂牌数量不足";
        }

        var itemId = listing.itemId!;
        if (CurrencyIds.IsCurrency(itemId))
        {
            return "不可购买星币";
        }

        if (!SellerCanFulfill(state, listing, itemId, quantity))
        {
            return "卖家库存不足";
        }

        var cost = listing.priceStarCoin * quantity;
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
            state.market.playerListings.Remove(listing);
        }

        var name = MemberAssetService.ItemDisplayName(itemId, null, null);
        return "玩家间购入 " + name + " ×" + quantity + " · 花费 " + cost + " 星币";
    }

    public static bool SellerCanFulfill(GameState state, TradeListing listing, string itemId, int quantity)
    {
        if (IsLegionSeller(state, listing))
        {
            return listing.quantity >= quantity;
        }

        if (string.Equals(listing.sellerKind, "player", StringComparison.Ordinal))
        {
            return listing.quantity >= quantity;
        }

        return LegionMarketService.SellerCanFulfill(state, listing, itemId, quantity);
    }

    public static void CreditSellerProceeds(GameState state, TradeListing listing, int paidTotal)
    {
        if (IsLegionSeller(state, listing))
        {
            var seller = LegionRegistry.Find(state, listing.sellerId);
            if (seller == null)
            {
                return;
            }

            seller.legionStock[CurrencyIds.StarCoin] =
                seller.legionStock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + paidTotal;
            if (seller.isLocal)
            {
                LegionRegistry.SyncLocalStockToLegacy(state);
            }

            return;
        }

        LegionMarketService.CreditSellerProceeds(state, listing, paidTotal);
    }

    public static void DebitSellerItem(
        GameState state,
        TradeListing listing,
        string itemId,
        int quantity)
    {
        if (IsLegionSeller(state, listing))
        {
            return;
        }

        LegionMarketService.DebitSellerItem(state, listing, itemId, quantity);
    }

    private static bool IsLegionSeller(GameState state, TradeListing listing) =>
        string.Equals(listing.sellerKind, "player", StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(listing.sellerId)
        && LegionRegistry.Find(state, listing.sellerId) != null;
}
