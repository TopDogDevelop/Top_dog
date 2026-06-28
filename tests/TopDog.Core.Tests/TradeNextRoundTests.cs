using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TradeNextRoundTests
{
    [Test]
    public void SellToNpc_NotBuyableUntilRefresh()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        MarketRefreshService.EnsureInitial(state, modules, ships);
        state.market.npcStock.Clear();
        state.legions.Add(new LegionState { legionId = "L1", isLocal = true });
        LegionPlayerRegistry.EnsureFromLegions(state);
        state.market.priceByItemId["mod_hybrid_gun_m"] = 1000;
        LegionRegistry.MutableLocalStock(state)["mod_hybrid_gun_m"] = 2;
        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 0;

        var sell = LegionPlayerTradeService.SellToMarket(state, "L1", "mod_hybrid_gun_m", 1);
        Assert.That(sell, Does.Contain("下回合"));
        Assert.That(state.market.npcStock.GetValueOrDefault("mod_hybrid_gun_m"), Is.EqualTo(0));
        Assert.That(state.market.pendingNpcStock["mod_hybrid_gun_m"], Is.EqualTo(1));

        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 5000;
        var buyFail = LegionPlayerTradeService.BuyFromMarket(state, "L1", "mod_hybrid_gun_m", 1);
        Assert.That(buyFail, Does.Contain("库存不足"));

        MarketRefreshService.Refresh(state, modules, ships);
        Assert.That(state.market.pendingNpcStock, Is.Empty);
        Assert.That(state.market.npcStock.GetValueOrDefault("mod_hybrid_gun_m"), Is.GreaterThanOrEqualTo(1));

        var buyOk = LegionPlayerTradeService.BuyFromMarket(state, "L1", "mod_hybrid_gun_m", 1);
        Assert.That(buyOk, Does.Contain("购入"));
    }

    [Test]
    public void LegionListing_NotBuyableUntilRefresh()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        MarketRefreshService.EnsureInitial(state, modules, ships);
        state.legions.Add(new LegionState { legionId = "L1", isLocal = true });
        LegionPlayerRegistry.EnsureFromLegions(state);
        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 5000;
        LegionRegistry.MutableLocalStock(state)["res_inorganic"] = 5;
        state.members.Add(new MemberState
        {
            memberId = "m1",
            identityCode = "id1",
            legionId = "L1",
            name = "Seller",
        });

        var list = LegionPlayerTradeService.ListOnLegionMarket(state, "L1", "res_inorganic", 1);
        Assert.That(list, Does.Contain("下回合"));
        Assert.That(state.market.legionListings, Is.Empty);
        Assert.That(state.market.pendingLegionListings, Has.Count.EqualTo(1));

        var listingId = state.market.pendingLegionListings[0].listingId!;
        var buyFail = LegionPlayerTradeService.BuyFromLegionListing(state, "L1", listingId, 1);
        Assert.That(buyFail, Does.Contain("找不到"));

        MarketRefreshService.Refresh(state, modules, ships);
        Assert.That(state.market.legionListings, Has.Count.EqualTo(1));
        var buyOk = LegionPlayerTradeService.BuyFromLegionListing(state, "L1", listingId, 1);
        Assert.That(buyOk, Does.Contain("军团内购入"));
    }

    [Test]
    public void PlayerListing_NotBuyableUntilRefresh()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        MarketRefreshService.EnsureInitial(state, modules, ships);
        state.legions.Add(new LegionState { legionId = "L1", isLocal = true });
        LegionPlayerRegistry.EnsureFromLegions(state);
        LegionRegistry.MutableLocalStock(state)["res_inorganic"] = 3;
        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 5000;
        state.market.priceByItemId["res_inorganic"] = 50;

        var list = PlayerMarketService.ListFromLegionStock(state, "L1", "res_inorganic", 1);
        Assert.That(list, Does.Contain("下回合"));
        Assert.That(state.market.playerListings, Is.Empty);
        Assert.That(state.market.pendingPlayerListings, Has.Count.EqualTo(1));

        var listingId = state.market.pendingPlayerListings[0].listingId!;
        var buyFail = PlayerMarketService.BuyFromPlayerListing(state, listingId, 1);
        Assert.That(buyFail, Does.Contain("找不到"));

        MarketRefreshService.Refresh(state, modules, ships);
        var buyOk = PlayerMarketService.BuyFromPlayerListing(state, listingId, 1);
        Assert.That(buyOk, Does.Contain("玩家间购入"));
    }
}
