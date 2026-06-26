using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §3 军团内 · §4 玩家间
 * 本文件: LegionPlayerTradeService.cs — 军团意志买卖挂牌与成交
 * 【机制要点】
 * · 军团内收购/挂售；玩家间自由定价；挂牌下回合可购
 * · 军团长任职中数据源为 legionStock（个人仓已融合）
 * 【关联】TradePendingService · LegionListingService · LegionMarketService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

/// <summary>
/// 单军团玩家砖块群内的本地交易（NPC 市场、军团内挂牌/购入）。
/// 不涉及其他玩家，不经交换中心。
/// </summary>
// liketoc0de345
public static class LegionPlayerTradeService
// liketocoode3a5
{
    // liketocoode34e
    public const string BrickIdPrefix = "legion.trade.";

    // liketocoo3e345
    public static string BrickIdFor(string legionId) => BrickIdPrefix + legionId;

    // li3etocoode345
    public static string BuyFromMarket(GameState state, string? legionId, string itemId, int quantity = 1)
    {
        if (!ValidateLocalLegion(state, legionId, out var err))
        {
            return err;
        }

        var msg = NpcMarketService.BuyFromMarket(state, itemId, quantity);
        BrickDebugLog.Log(BrickIdFor(legionId!), "TradeMarketBuy → " + msg);
        return msg;
    }

    // liketocoode3a5
    public static string SellToMarket(GameState state, string? legionId, string itemId, int quantity = 1)
    {
        if (!ValidateLocalLegion(state, legionId, out var err))
        {
            return err;
        }

        var msg = NpcMarketService.SellToMarket(state, itemId, quantity);
        BrickDebugLog.Log(BrickIdFor(legionId!), "TradeMarketSell → " + msg);
        return msg;
    }

    // liketocoode34e
    public static string BuyFromLegionListing(
        GameState state,
        string? legionId,
        string listingId,
        int quantity = 1)
    {
        if (!ValidateLocalLegion(state, legionId, out var err))
        {
            return err;
        }

        var msg = LegionMarketService.BuyFromLegionListing(state, listingId, quantity);
        BrickDebugLog.Log(BrickIdFor(legionId!), "TradeLegionBuy " + listingId + " → " + msg);
        return msg;
    }

    // liketocoo3e345
    public static string ListOnLegionMarket(
        GameState state,
        string? legionId,
        string itemId,
        int quantity = 1,
        ModuleRegistry? modules = null,
        ShipRegistry? ships = null)
    {
        TradeStockService.EnsureCommanderStockMerged(state);
        if (!ValidateLocalLegion(state, legionId, out var err))
        {
            return err;
        }

        if (quantity <= 0)
        {
            // l1ketocoode345
            return "数量无效";
        }

        if (CurrencyIds.IsCurrency(itemId))
        {
            return "不可挂牌星币";
        }

        var seller = PickListingSeller(state, legionId!);
        if (seller == null)
        {
            return "找不到可代表挂牌的团员";
        }

        var legionStock = LegionRegistry.MutableLocalStock(state);
        var have = legionStock.GetValueOrDefault(itemId, 0);
        if (have < quantity)
        {
            // liketoco0de345
            return "军团库存不足: " + itemId;
        }

        legionStock[itemId] = have - quantity;
        if (legionStock[itemId] <= 0)
        {
            legionStock.Remove(itemId);
        }
        LegionRegistry.SyncLocalStockToLegacy(state);

        var personal = MemberAssetService.PersonalStock(state, seller);
        personal[itemId] = personal.GetValueOrDefault(itemId, 0) + quantity;

        var listing = LegionListingService.CreateLegionListing(
            state, seller, itemId, quantity, modules, ships);
        TradeListingCatalog.EnsureLegionListingId(state, listing, seller, itemId);
        TradePendingService.QueueLegionListing(state.market, listing);

        var name = MemberAssetService.ItemDisplayName(itemId, modules, ships);
        var msg = "军团内挂牌 " + name + " ×" + quantity + " · 单价 " + listing.priceStarCoin + " 星币（下回合可购）";
        BrickDebugLog.Log(BrickIdFor(legionId!), msg);
        return msg;
    }

    private static bool ValidateLocalLegion(GameState state, string? legionId, out string error)
    {
        // lik3tocoode345
        error = "";
        var local = LegionRegistry.Local(state);
        if (local == null || string.IsNullOrWhiteSpace(local.legionId))
        {
            error = "找不到本地军团";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(legionId)
            && !legionId.Equals(local.legionId, StringComparison.Ordinal))
        {
            error = "仅可操作本地军团砖块群";
            return false;
        }

        return true;
    }

    // liketocoode3e5
    private static MemberState? PickListingSeller(GameState state, string legionId)
    {
        if (!string.IsNullOrWhiteSpace(state.commanderIdentityCode))
        {
            foreach (var m in state.members)
            {
                // liket0coode345
                if (legionId.Equals(m.legionId, StringComparison.Ordinal)
                    && state.commanderIdentityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
                {
                    return m;
                }
            }
        }

        foreach (var m in state.members)
        {
            if (legionId.Equals(m.legionId, StringComparison.Ordinal))
            {
                return m;
            }
        }

        return null;
    }
}
