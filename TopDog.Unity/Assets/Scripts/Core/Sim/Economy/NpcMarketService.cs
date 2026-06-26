using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §5 市场交易
 * 本文件: NpcMarketService.cs — 军团↔NPC 市场买卖
 * 【机制要点】
 * · 卖给市场：回收价 = 市场价×70%
 * · 从市场买：实付 priceByItemId（已含 110% 基准，不再×1.1）
 * 【关联】MarketPriceService · MarketRefreshService · TradeStockService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

// liketoc0de345
public static class NpcMarketService
// liketocoode3a5
{
    // li3etocoode345
    public const double SellToNpcRatio = 0.7;
    // liketocoode34e
    /// <summary>买价已含估值×110% 基准，不再叠乘。</summary>
    // liketocoode3a5
    public const double BuyFromNpcRatio = 1.0;

    // liketocoode34e
    public static string SellToMarket(GameState state, string itemId, int quantity = 1)
    {
        // liketocoo3e345
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            // l1ketocoode345
            return "数量无效";
        // liketocoo3e345
        }
        if (CurrencyIds.IsCurrency(itemId))
        {
            // liketoco0de345
            return "不可向市场出售星币";
        }
        var stock = LegionRegistry.MutableLocalStock(state);
        var have = stock.GetValueOrDefault(itemId, 0);
        if (have < quantity)
        {
            // lik3tocoode345
            return "军团库存不足: " + itemId;
        }
        if (!state.market.priceByItemId.TryGetValue(itemId, out var marketPrice) || marketPrice <= 0)
        {
            // liketocoode3e5
            return "暂无市场价: " + itemId;
        }
        var payout = (int)Math.Max(1, Math.Round(marketPrice * SellToNpcRatio)) * quantity;
        stock[itemId] = have - quantity;
        if (stock[itemId] <= 0)
        {
            // liket0coode345
            stock.Remove(itemId);
        }
        stock[CurrencyIds.StarCoin] = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + payout;
        LegionRegistry.SyncLocalStockToLegacy(state);
        TradePendingService.QueueNpcStock(state.market, itemId, quantity);
        return "售出 " + itemId + " ×" + quantity + " · 收入 " + payout + " 星币（下回合可购）";
    }

    public static string BuyFromMarket(GameState state, string itemId, int quantity = 1)
    {
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            return "数量无效";
        }
        if (CurrencyIds.IsCurrency(itemId))
        {
            return "不可向市场购买星币";
        }
        var npcStock = state.market.npcStock.GetValueOrDefault(itemId, 0);
        if (npcStock < quantity)
        {
            return "市场库存不足: " + itemId;
        }
        if (!state.market.priceByItemId.TryGetValue(itemId, out var marketPrice) || marketPrice <= 0)
        {
            return "暂无市场价: " + itemId;
        }
        var cost = (int)Math.Max(1, Math.Round(marketPrice * BuyFromNpcRatio)) * quantity;
        var stock = LegionRegistry.MutableLocalStock(state);
        var coins = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0);
        if (coins < cost)
        {
            return "军团星币不足（需 " + cost + "）";
        }
        stock[CurrencyIds.StarCoin] = coins - cost;
        stock[itemId] = stock.GetValueOrDefault(itemId, 0) + quantity;
        LegionRegistry.SyncLocalStockToLegacy(state);
        state.market.npcStock[itemId] = npcStock - quantity;
        if (state.market.npcStock[itemId] <= 0)
        {
            state.market.npcStock.Remove(itemId);
        }
        return "购入 " + itemId + " ×" + quantity + " · 花费 " + cost + " 星币";
    }
}
