using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TraitMechanismTests
{
    [Test]
    public void DuckSource_SpreadsDuckSauce_InLegion()
    {
        var state = new GameState { storyRound = 3, gameWeek = 2 };
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { "trait_duck_source" },
        };
        state.members.Add(new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            name = "Source",
            legionId = "VIP",
            traitIds = { "trait_duck_source" },
        });
        state.members.Add(new MemberState
        {
            memberId = "1000100201",
            identityCode = "10001002",
            name = "Target",
            legionId = "VIP",
        });
        IdentityMigrationService.EnsureFromMembers(state);

        TraitResolutionService.ResolveWindow(state, "post_ops_pre_combat", null);

        Assert.That(state.identities["10001002"].traitIds, Does.Contain("trait_duck_sauce"));
        Assert.That(state.members[1].traitIds, Does.Contain("trait_duck_sauce"));
    }

    [Test]
    public void DiscordSource_ReducesBelonging_ForFiveMembers()
    {
        var state = new GameState { storyRound = 2 };
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            legionBelonging = 5,
            traitIds = { "trait_discord_source" },
        };
        for (var i = 0; i < 3; i++)
        {
            var code = "1000100" + (i + 2);
            state.identities[code] = new IdentityState { identityCode = code, legionBelonging = 5 };
            state.members.Add(new MemberState
            {
                memberId = code + "01",
                identityCode = code,
                legionId = "VIP",
                legionBelonging = 5,
            });
        }
        state.members.Add(new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIP",
            legionBelonging = 5,
            traitIds = { "trait_discord_source" },
        });
        IdentityMigrationService.EnsureFromMembers(state);

        TraitResolutionService.ResolveWindow(state, "post_ops_pre_combat", null);
        var totalBelonging = state.identities.Values.Sum(id => id.legionBelonging);
        Assert.That(totalBelonging, Is.EqualTo(5 * 4 - 5));
    }

    [Test]
    public void BoardFavor_RefundsOneThird_OnLegionSpend()
    {
        var state = new GameState();
        state.legions.Add(new LegionState
        {
            legionId = "VIP",
            isLocal = true,
            legionStock = { [CurrencyIds.StarCoin] = 1000 },
        });
        state.legionStock[CurrencyIds.StarCoin] = 1000;
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { BoardFavorTraitService.TraitId },
        };
        state.members.Add(new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIP",
            traitIds = { BoardFavorTraitService.TraitId },
        });
        IdentityMigrationService.EnsureFromMembers(state);

        Assert.That(MemberAssetService.TryDebitLegion(state, CurrencyIds.StarCoin, 300), Is.True);
        Assert.That(state.legionStock[CurrencyIds.StarCoin], Is.EqualTo(800));
    }

    [Test]
    public void Devotion_ListingPriceIsQuarterMarket()
    {
        var state = new GameState();
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { DevotionTraitService.TraitId },
        };
        var seller = new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIL",
            traitIds = { DevotionTraitService.TraitId },
        };
        state.members.Add(seller);
        state.market.priceByItemId["res_inorganic"] = 100;
        IdentityMigrationService.EnsureFromMembers(state);

        Assert.That(DevotionTraitService.PriceFromMarket(100), Is.EqualTo(25));
        var listing = LegionListingService.CreateLegionListing(state, seller, "res_inorganic", 1);
        Assert.That(listing.priceStarCoin, Is.EqualTo(25));
        Assert.That(listing.devotionListing, Is.True);
        Assert.That(listing.referenceMarketPrice, Is.EqualTo(100));
    }

    [Test]
    public void MechanismCatalog_LoadsSheepLineBatch()
    {
        var cat = Content.Mechanisms.MechanismCatalog.LoadDefault();
        Assert.That(cat.Find("devotion"), Is.Not.Null);
        Assert.That(cat.Find("duck_source"), Is.Not.Null);
        Assert.That(cat.Find("board_summon"), Is.Not.Null);
    }
}
