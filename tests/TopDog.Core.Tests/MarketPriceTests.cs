using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Member;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MarketPriceTests
{
    [Test]
    public void RollMarketPrice_IronCoffinAtMaxMultiplier_IsAbout275k()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var rng = new Random(42);
        int? maxSeen = null;
        for (var i = 0; i < 5000; i++)
        {
            var price = MarketPriceService.RollMarketPrice("hull_dread_ironcoffin", modules, ships, rng);
            if (price >= 270_000)
            {
                maxSeen = price;
                break;
            }
        }
        Assert.That(maxSeen, Is.Not.Null.And.GreaterThanOrEqualTo(270_000),
            "500× branch on 50k×1.1 base should reach ~275k");
    }

    [Test]
    public void RollMarketPrice_At120PercentPeak_IsAbout66k()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var valuation = AssetValuation.ItemStarCoinValue("hull_dread_ironcoffin", ships, modules);
        Assert.That(valuation, Is.EqualTo(50_000));
        var basePrice = (int)Math.Round(valuation * MarketPriceService.ValuationBaseRatio);
        var mult = 1.20;
        var expected = (int)Math.Round(basePrice * mult);
        Assert.That(expected, Is.EqualTo(66_000));
    }

    [Test]
    public void SampleMultiplierPercent_Tail500_AboutHalfPercent()
    {
        var rng = new Random(12345);
        var count500 = 0;
        const int trials = 20_000;
        for (var i = 0; i < trials; i++)
        {
            if (MarketPriceService.SampleMultiplierPercent(rng) == 500)
            {
                count500++;
            }
        }
        var rate = (double)count500 / trials;
        Assert.That(rate, Is.InRange(0.002, 0.008), "P(500%) should be ~0.5%");
    }

    [Test]
    public void RollMarketPrice_Inorganic_HasPositivePrice()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var rng = new Random(1);
        var price = MarketPriceService.RollMarketPrice("res_inorganic", modules, ships, rng);
        Assert.That(price, Is.GreaterThanOrEqualTo(1));
        var val = AssetValuation.ItemStarCoinValue("res_inorganic", ships, modules);
        Assert.That(val, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void BuyFromMarket_UsesPriceByItemIdWithoutExtraMarkup()
    {
        var core = TopDog.App.CampaignBootstrap.Create(
            TopDog.App.CampaignBootstrap.Profile.SHIPS_AND_MAP,
            TopDog.Sim.State.WorldlineType.STORY);
        var state = core.State;
        const string itemId = "res_inorganic";
        state.market.npcStock[itemId] = 5;
        state.market.priceByItemId[itemId] = 100;
        var stock = TopDog.Sim.Legion.LegionRegistry.MutableLocalStock(state);
        stock[CurrencyIds.StarCoin] = 10_000;
        var msg = NpcMarketService.BuyFromMarket(state, itemId, 1);
        Assert.That(msg, Does.Contain("花费 100"));
        Assert.That(stock[CurrencyIds.StarCoin], Is.EqualTo(9900));
    }
}
