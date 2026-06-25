using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class LegionPlayerTradeTests
{
    [Test]
    public void BuyFromLegionListing_UsesListingIdNotItemId()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        MarketRefreshService.EnsureInitial(state, modules, ships);
        state.legions.Add(new LegionState { legionId = "L1", isLocal = true });
        LegionPlayerRegistry.EnsureFromLegions(state);
        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 5000;
        var seller = new MemberState
        {
            memberId = "m1",
            identityCode = "id1",
            legionId = "L1",
            name = "Seller",
        };
        state.members.Add(seller);
        MemberAssetService.PersonalStock(state, seller)["res_inorganic"] = 1;
        var listing = LegionListingService.CreateLegionListing(state, seller, "res_inorganic", 1);
        state.market.legionListings.Add(listing);

        var msg = LegionPlayerTradeService.BuyFromLegionListing(state, "L1", listing.listingId!, 1);

        Assert.That(msg, Does.Contain("军团内购入"));
        Assert.That(msg, Does.Not.Contain("找不到"));
    }

    [Test]
    public void LocalMarketBuy_DoesNotUseExchangeInbox()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        MarketRefreshService.EnsureInitial(state, modules, ships);
        state.flags["exchange.enabled"] = "1";
        state.legions.Add(new LegionState { legionId = "L1", isLocal = true });
        state.market.priceByItemId["res_inorganic"] = 100;
        state.market.npcStock["res_inorganic"] = 2;
        LegionRegistry.MutableLocalStock(state)[CurrencyIds.StarCoin] = 5000;

        var msg = LegionPlayerTradeService.BuyFromMarket(state, "L1", "res_inorganic", 1);

        Assert.That(msg, Does.Contain("购入"));
        Assert.That(state.exchange.pendingMessages, Is.Empty);
    }

    [Test]
    public void RealPersonShopAi_ReservesStockWhenListing()
    {
        var state = new GameState();
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        MarketRefreshService.EnsureInitial(state, modules, ships);
        var m = new MemberState
        {
            memberId = "rp1",
            identityCode = "90000001",
            name = "RP",
        };
        state.members.Add(m);
        state.identities["90000001"] = new IdentityState { identityCode = "90000001" };
        MemberAssetService.PersonalStock(state, m)["mod_hybrid_gun_m"] = 2;

        RealPersonShopAi.Run(state, modules, ships);

        Assert.That(state.market.legionListings, Is.Not.Empty);
        Assert.That(
            MemberAssetService.PersonalStock(state, m).GetValueOrDefault("mod_hybrid_gun_m", 0),
            Is.EqualTo(1));
    }
}
