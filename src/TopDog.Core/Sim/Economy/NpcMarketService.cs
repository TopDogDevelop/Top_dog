using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

public static class NpcMarketService
{
    public const double SellToNpcRatio = 0.7;
    public const double BuyFromNpcRatio = 1.1;

    public static string SellToMarket(GameState state, string itemId, int quantity = 1)
    {
        TradeStockService.EnsureCommanderStockMerged(state);
        if (quantity <= 0)
        {
            return "数量无效";
        }
        if (CurrencyIds.IsCurrency(itemId))
        {
            return "不可向市场出售星币";
        }
        var stock = LegionRegistry.MutableLocalStock(state);
        var have = stock.GetValueOrDefault(itemId, 0);
        if (have < quantity)
        {
            return "军团库存不足: " + itemId;
        }
        if (!state.market.priceByItemId.TryGetValue(itemId, out var marketPrice) || marketPrice <= 0)
        {
            return "暂无市场价: " + itemId;
        }
        var payout = (int)Math.Max(1, Math.Round(marketPrice * SellToNpcRatio)) * quantity;
        stock[itemId] = have - quantity;
        if (stock[itemId] <= 0)
        {
            stock.Remove(itemId);
        }
        stock[CurrencyIds.StarCoin] = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + payout;
        LegionRegistry.SyncLocalStockToLegacy(state);
        state.market.npcStock[itemId] = state.market.npcStock.GetValueOrDefault(itemId, 0) + quantity;
        return "售出 " + itemId + " ×" + quantity + " · 收入 " + payout + " 星币";
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
