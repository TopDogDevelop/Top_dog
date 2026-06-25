using TopDog.App;
using TopDog.App.Brick;
using TopDog.Sim.Economy;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class ExchangeTradeTests
{
    private static GameState StateWithLocalLegion(string legionId = "L_local")
    {
        var state = new GameState();
        state.flags["exchange.enabled"] = "1";
        state.legions.Add(new LegionState { legionId = legionId, isLocal = true });
        LegionPlayerRegistry.EnsureFromLegions(state);
        return state;
    }

    [Test]
    public void BuyFromPlayerListing_RoutesThroughExchangeWhenEnabled()
    {
        var state = StateWithLocalLegion();
        state.legions.Add(new LegionState { legionId = "ai_other", isAiControlled = true });
        state.market.playerListings.Add(new TradeListing
        {
            listingId = "test_player_listing",
            sellerKind = "player",
            sellerId = "ai_other",
            itemId = "res_inorganic",
            quantity = 2,
            priceStarCoin = 10,
        });
        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 10_000;

        var msg = ExchangeTradeService.BuyFromPlayerListing(state, "test_player_listing", 1);

        Assert.That(msg, Does.Contain("玩家间购入"));
        Assert.That(state.exchange.pendingMessages, Is.Empty);
        Assert.That(BrickDebugLog.Snapshot().Any(l => l.Contains("exchange.hub")), Is.True);
    }

    [Test]
    public void ListOnPlayerMarket_RoutesThroughExchangeWhenEnabled()
    {
        var state = StateWithLocalLegion();
        var stock = LegionRegistry.MutableLocalStock(state);
        stock["res_inorganic"] = 3;
        state.market.priceByItemId["res_inorganic"] = 50;

        var msg = ExchangeTradeService.ListOnPlayerMarket(state, "res_inorganic", 1);

        Assert.That(msg, Does.Contain("玩家间挂牌"));
        Assert.That(state.market.playerListings, Is.Not.Empty);
        Assert.That(stock.GetValueOrDefault("res_inorganic", 0), Is.EqualTo(2));
    }
}
