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
        Assert.That(state.market.sessionSeed, Is.Not.EqualTo(0));
        Assert.That(state.market.priceByItemId["mod_hybrid_gun_m"], Is.GreaterThan(0));
    }

    [Test]
    public void CampaignBootstrap_HasMarketPricesOnCreate()
    {
        var core = CampaignBootstrap.Create(CampaignBootstrap.Profile.SHIPS_AND_MAP, WorldlineType.STORY);
        Assert.That(core.State.market.priceByItemId, Is.Not.Empty);
        Assert.That(core.State.market.npcStock, Is.Not.Empty);
        Assert.That(core.State.market.sessionSeed, Is.Not.EqualTo(0));
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

    [Test]
    public void LegacyDeterministicSeed_ProducedIdenticalFirstRoundMarkets()
    {
        const int legacySeed = 1 * 7919 + 1 * 31 + 0;
        Assert.That(legacySeed, Is.EqualTo(7950));

        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var pool = new List<string> { "mod_hybrid_gun_m", "mod_ore_mining_beam_s", "mod_shield_regen_m", "res_inorganic" };
        foreach (var hull in ships.AllHulls())
        {
            if (!string.IsNullOrWhiteSpace(hull.hullId))
            {
                pool.Add(hull.hullId);
            }
        }

        string Snapshot(int seed)
        {
            var rng = new Random(seed);
            var count = rng.Next(1, 11);
            var stock = new Dictionary<string, int>();
            for (var i = 0; i < count; i++)
            {
                var id = pool[rng.Next(pool.Count)];
                stock[id] = stock.GetValueOrDefault(id, 0) + 1;
            }
            return string.Join("|", stock.OrderBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value));
        }

        Assert.That(Snapshot(legacySeed), Is.EqualTo(Snapshot(legacySeed)),
            "旧公式 year/week/round 在新局恒为 7950，每局首回合 NPC 货完全相同");
    }

    [Test]
    public void TwoCampaigns_WithDifferentSessionSeed_DifferOnFirstRound()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();

        static string NpcSnapshot(GameState state, ModuleRegistry modules, ShipRegistry ships)
        {
            MarketRefreshService.Refresh(state, modules, ships);
            return string.Join("|",
                state.market.npcStock.OrderBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value));
        }

        var a = new GameState();
        a.market.sessionSeed = 101;
        var b = new GameState();
        b.market.sessionSeed = 909_909;

        var snapA = NpcSnapshot(a, modules, ships);
        var snapB = NpcSnapshot(b, modules, ships);
        Assert.That(snapA, Is.Not.EqualTo(snapB));
    }
}
