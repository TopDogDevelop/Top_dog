using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Exchange;

/// <summary>跨玩家交易经交换中心 inbox；本地 NPC/军团内交易见 <see cref="LegionPlayerTradeService"/>。</summary>
public static class ExchangeTradeService
{
    public static bool UsesExchange(GameState state) =>
        state.flags.TryGetValue("exchange.enabled", out var v)
        && string.Equals(v, "1", StringComparison.Ordinal);

    public static string BuyFromPlayerListing(GameState state, string listingId, int quantity = 1) =>
        Route(state, ExchangeMessageKind.TradePlayerBuy, legionId: LegionRegistry.Local(state)?.legionId,
            listingId: listingId, quantity: quantity);

    public static string ListOnPlayerMarket(
        GameState state,
        string itemId,
        int quantity = 1,
        ModuleRegistry? modules = null,
        ShipRegistry? ships = null)
    {
        var legionId = LegionRegistry.Local(state)?.legionId;
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return "找不到本地军团";
        }

        if (!UsesExchange(state))
        {
            return PlayerMarketService.ListFromLegionStock(
                state, legionId, itemId, quantity, modules, ships);
        }

        var msg = new ExchangeMessage
        {
            kind = ExchangeMessageKind.TradePlayerList,
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
