using TopDog.Sim.Economy;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TradeStockServiceTests
{
    [Test]
    public void EnsureCommanderStockMerged_MovesPersonalBeforeTrade()
    {
        var state = new GameState
        {
            commanderIdentityCode = "10000001",
            identities =
            {
                ["10000001"] = new IdentityState { identityCode = "10000001", isLegionCommander = true },
            },
            members =
            {
                new MemberState
                {
                    memberId = "1000000101",
                    identityCode = "10000001",
                    multiboxGroupId = "mb_10000001",
                },
            },
        };
        state.personalStockByGroup["mb_10000001"] = new Dictionary<string, int> { ["mod_hybrid_gun_m"] = 2 };
        TradeStockService.EnsureCommanderStockMerged(state);
        Assert.That(state.legionStock.GetValueOrDefault("mod_hybrid_gun_m"), Is.EqualTo(2));
        Assert.That(state.personalStockByGroup["mb_10000001"], Is.Empty);
    }

    [Test]
    public void SellToMarket_UsesMergedCommanderStock()
    {
        var state = new GameState
        {
            commanderIdentityCode = "10000001",
            members =
            {
                new MemberState
                {
                    memberId = "1000000101",
                    identityCode = "10000001",
                    multiboxGroupId = "mb_10000001",
                },
            },
        };
        state.personalStockByGroup["mb_10000001"] = new Dictionary<string, int> { ["res_inorganic"] = 3 };
        state.market.priceByItemId["res_inorganic"] = 100;
        var msg = NpcMarketService.SellToMarket(state, "res_inorganic", 1);
        Assert.That(msg, Does.Contain("售出"));
        Assert.That(state.legionStock.GetValueOrDefault("res_inorganic"), Is.EqualTo(2));
    }
}
