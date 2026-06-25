using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class LegionMarketServiceTests
{
    [Test]
    public void BuyFromLegionListing_PaysListedDevotionPrice()
    {
        var state = new GameState();
        state.legions.Add(new LegionState
        {
            legionId = "VIL",
            isLocal = true,
            legionStock = { [CurrencyIds.StarCoin] = 500 },
        });
        state.legionStock[CurrencyIds.StarCoin] = 500;
        state.market.priceByItemId["res_inorganic"] = 100;
        var seller = new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIL",
            traitIds = { DevotionTraitService.TraitId },
        };
        state.members.Add(seller);
        state.personalStockByGroup["mb_10001001"] = new Dictionary<string, int> { ["res_inorganic"] = 1 };
        seller.multiboxGroupId = "mb_10001001";
        var listing = LegionListingService.CreateLegionListing(state, seller, "res_inorganic", 1);
        state.market.legionListings.Add(listing);
        IdentityMigrationService.EnsureFromMembers(state);

        Assert.That(listing.priceStarCoin, Is.EqualTo(25));
        Assert.That(listing.devotionListing, Is.True);
        Assert.That(LegionMarketService.BuyerPrice(state, listing), Is.EqualTo(25));

        var msg = LegionMarketService.BuyFromLegionListing(state, listing.listingId!, 1);
        Assert.That(msg, Does.Contain("奉献"));
        Assert.That(state.legionStock.GetValueOrDefault(CurrencyIds.StarCoin), Is.EqualTo(475));
        Assert.That(state.legionStock.GetValueOrDefault("res_inorganic"), Is.EqualTo(1));
    }

    [Test]
    public void BuyFromLegionListing_NoDevotionDiscountWhenListingFullPrice()
    {
        var state = new GameState();
        state.legions.Add(new LegionState { legionId = "VIL", isLocal = true, legionStock = { [CurrencyIds.StarCoin] = 500 } });
        state.legionStock[CurrencyIds.StarCoin] = 500;
        var seller = new MemberState
        {
            memberId = "1000100201",
            identityCode = "10001002",
            legionId = "VIL",
            multiboxGroupId = "g2",
        };
        state.members.Add(seller);
        state.personalStockByGroup["g2"] = new Dictionary<string, int> { ["res_inorganic"] = 1 };
        state.market.legionListings.Add(new TradeListing
        {
            listingId = "full_price",
            sellerId = seller.memberId,
            itemId = "res_inorganic",
            quantity = 1,
            priceStarCoin = 100,
            devotionListing = false,
        });

        Assert.That(LegionMarketService.BuyerPrice(state, state.market.legionListings[0]), Is.EqualTo(100));
        var msg = LegionMarketService.BuyFromLegionListing(state, "full_price", 1);
        Assert.That(msg, Does.Not.Contain("奉献"));
        Assert.That(state.legionStock[CurrencyIds.StarCoin], Is.EqualTo(400));
    }
}
