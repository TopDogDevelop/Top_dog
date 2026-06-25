using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MarketRefreshTests
{
    [Test]
    public void EnsureInitial_SeedsPricesBeforeFirstCombat()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        Assert.That(state.market.priceByItemId, Is.Empty);
        MarketRefreshService.EnsureInitial(state, modules, ships);
        Assert.That(state.market.priceByItemId, Is.Not.Empty);
        Assert.That(state.market.npcStock, Is.Not.Empty);
        Assert.That(state.market.priceByItemId["mod_hybrid_gun_m"], Is.GreaterThan(0));
    }

    [Test]
    public void CampaignBootstrap_HasMarketPricesOnCreate()
    {
        var core = CampaignBootstrap.Create(CampaignBootstrap.Profile.SHIPS_AND_MAP, WorldlineType.STORY);
        Assert.That(core.State.market.priceByItemId, Is.Not.Empty);
        Assert.That(core.State.market.npcStock, Is.Not.Empty);
    }

    [Test]
    public void Refresh_IncludesHullPricesWhenShipsLoaded()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        if (ships.AllHulls().Count == 0)
        {
            Assert.Ignore("No hull content in test environment");
        }
        MarketRefreshService.Refresh(state, modules, ships);
        var firstHull = ships.AllHulls()[0].hullId!;
        Assert.That(state.market.priceByItemId.ContainsKey(firstHull), Is.True);
    }
}
