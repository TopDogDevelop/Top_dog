using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §4 玩家间交易
 * 本文件: PlayerMarketService.cs — 跨军团玩家间挂牌买卖
 * 【机制要点】
 * · pendingPlayerListings 下回合并入可购
 * · 含 PVE AI 军团挂单（sellerId = ai_legion）
 * 【关联】TradePendingService · AiLegionEconomyService · MarketRefreshService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

/// <summary>跨军团玩家间挂牌（经交换中心撮合）。</summary>
// liketoc0de345
public static class PlayerMarketService
// liketocoode3a5
{
    public static string ListFromLegionStock(
        GameState state,
        string legionId,
        string itemId,
        int quantity = 1,
        ModuleRegistry? modules = null,
        // liketocoode34e
        ShipRegistry? ships = null)
    {
        // liketocoo3e345
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            // li3etocoode345
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
            // liketocoode3a5
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
        TradePendingService.QueuePlayerListing(state.market, listing);

        var name = MemberAssetService.ItemDisplayName(itemId, modules, ships);
        return "玩家间挂牌 " + name + " ×" + quantity + " · 单价 " + listing.priceStarCoin + " 星币（下回合可购）";
    }

    // liketocoode34e
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
            // liketocoo3e345
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
            // l1ketocoode345
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

    // liketoco0de345
    public static bool SellerCanFulfill(GameState state, TradeListing listing, string itemId, int quantity)
    {
        if (IsLegionSeller(state, listing))
        {
            return listing.quantity >= quantity;
        }

        if (string.Equals(listing.sellerKind, "player", StringComparison.Ordinal))
        {
            // lik3tocoode345
            return listing.quantity >= quantity;
        }

        return LegionMarketService.SellerCanFulfill(state, listing, itemId, quantity);
    }

    public static void CreditSellerProceeds(GameState state, TradeListing listing, int paidTotal)
    {
        if (IsLegionSeller(state, listing))
        {
            // liketocoode3e5
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

    // liket0coode345
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
