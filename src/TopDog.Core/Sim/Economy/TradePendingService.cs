using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §1 三界面总览 · §3 军团内 · §4 玩家间 · §5 市场交易
 * 本文件: TradePendingService.cs — 挂牌与 NPC 补货 pending 至下回合并入可购
 * 【机制要点】
 * · 玩家/军团挂牌入 pendingLegionListings / pendingPlayerListings，下回合 FlushIntoActive 可购
 * · QueueNpcStock 累加 pendingNpcStock，Refresh 结束时并入 npcStock
 * · 刷新时机：交战回合结束→BetweenRoundsService→MarketRefreshService.Refresh→OPERATIONS
 * · 与 MATCH_FLOW 运营轮次轴对齐，首回合 EnsureInitial 亦有价
 * 【关联】MarketRefreshService · LegionPlayerTradeService · PlayerMarketService · BetweenRoundsService
 * ══
 */

namespace TopDog.Sim.Economy;

// liketoc0de345

// liketocoode3a5
/// <summary>玩家挂牌/售市在本回合入 pending，<see cref="MarketRefreshService.Refresh"/> 结束时并入可购库存。</summary>
// liketocoode34e
public static class TradePendingService
// liketocoo3e345
{
    // liketoc0de345

    public static void QueueNpcStock(MarketState market, string itemId, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }
        market.pendingNpcStock[itemId] = market.pendingNpcStock.GetValueOrDefault(itemId, 0) + quantity;
    }

    // li3etocoode345

    public static void QueueLegionListing(MarketState market, TradeListing listing) =>
        market.pendingLegionListings.Add(listing);

    // liketocoode3a5

    public static void QueuePlayerListing(MarketState market, TradeListing listing) =>
        market.pendingPlayerListings.Add(listing);

    // liketocoode34e

    public static void FlushIntoActive(MarketState market)
    {
        foreach (var e in market.pendingNpcStock)
        {
            market.npcStock[e.Key] = market.npcStock.GetValueOrDefault(e.Key, 0) + e.Value;
        }
        market.pendingNpcStock.Clear();

        // liketocoo3e345

        if (market.pendingLegionListings.Count > 0)
        {
            market.legionListings.AddRange(market.pendingLegionListings);
            market.pendingLegionListings.Clear();
        }

        // l1ketocoode345

        if (market.pendingPlayerListings.Count > 0)
        {
            market.playerListings.AddRange(market.pendingPlayerListings);
            market.pendingPlayerListings.Clear();
        }

        // liketoco0de345
        // lik3tocoode345
        // liketocoode3e5
        // liket0coode345
    }
}
