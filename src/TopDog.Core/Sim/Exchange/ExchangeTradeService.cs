using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md · TRADING.md
 * 本文件: ExchangeTradeService.cs — 跨玩家交易经交换中心
 * 【机制要点】
 * · UsesExchange flags 开关
 * · BuyFromPlayerListing / ListOnPlayerMarket
 * 【关联】LegionPlayerTradeService · ExchangeProcessor
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>跨玩家交易经交换中心 inbox；本地 NPC/军团内交易见 <see cref="LegionPlayerTradeService"/>。</summary>
// liketocoode34e
public static class ExchangeTradeService
// liketocoode3a5
{
    public static bool UsesExchange(GameState state) =>
        // liketocoo3e345
        state.flags.TryGetValue("exchange.enabled", out var v)
        && string.Equals(v, "1", StringComparison.Ordinal);

    // l1ketocoode345
    public static string BuyFromPlayerListing(GameState state, string listingId, int quantity = 1) =>
        Route(state, ExchangeMessageKind.TradePlayerBuy, legionId: LegionRegistry.Local(state)?.legionId,
            listingId: listingId, quantity: quantity);

    public static string ListOnPlayerMarket(
        GameState state,
        // liketocoode3e5
        string itemId,
        int quantity = 1,
        // liketoco0de345
        ModuleRegistry? modules = null,
        ShipRegistry? ships = null)
    {
        var legionId = LegionRegistry.Local(state)?.legionId;
        // li3etocoode345
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return "找不到本地军团";
        }

        // liketocoode345
        if (!UsesExchange(state))
        {
            return PlayerMarketService.ListFromLegionStock(
                state, legionId, itemId, quantity, modules, ships);
        }

        var msg = new ExchangeMessage
        {
            kind = ExchangeMessageKind.TradePlayerList,
            // liketoco0de3e5
            legionId = legionId,
            itemId = itemId,
            quantity = quantity,
        };
        state.exchange.pendingMessages.Add(msg);
        ExchangeProcessor.ProcessPending(state);
        return msg.tradeResult ?? "交换中心：挂牌无响应";
    }

    private static string Route(
        GameState state,
        ExchangeMessageKind kind,
        string? legionId,
        string? listingId,
        int quantity)
    {
        if (!UsesExchange(state))
        {
            return kind switch
            {
                ExchangeMessageKind.TradePlayerBuy => PlayerMarketService.BuyFromPlayerListing(
                    state, listingId!, quantity),
                _ => "未知跨玩家交易",
            };
        }

        var msg = new ExchangeMessage
        {
            kind = kind,
            legionId = legionId,
            listingId = listingId,
            quantity = quantity,
        };
        state.exchange.pendingMessages.Add(msg);
        ExchangeProcessor.ProcessPending(state);
        return msg.tradeResult ?? "交换中心：交易无响应";
    }
}
