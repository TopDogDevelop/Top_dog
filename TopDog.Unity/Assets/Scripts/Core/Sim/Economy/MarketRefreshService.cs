using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

public static class MarketRefreshService
{
    private static readonly string[] SeedItems =
    {
        "mod_hybrid_gun_m", "mod_ore_mining_beam_s", "mod_shield_regen_m", "res_inorganic",
    };

    public static void EnsureInitial(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        if (state.market.priceByItemId.Count > 0)
        {
            return;
        }
        Refresh(state, modules, ships);
    }

    public static void Refresh(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        var rng = new Random((int)(state.gameYear * 7919L + state.gameWeek * 31L + state.storyRound));
        var pool = BuildTradePool(ships);
        state.market.priceByItemId.Clear();
        foreach (var id in pool)
        {
            state.market.priceByItemId[id] = MarketPriceService.RollMarketPrice(id, modules, ships, rng);
        }
        foreach (var e in LegionRegistry.MutableLocalStock(state))
        {
            if (!state.market.priceByItemId.ContainsKey(e.Key))
            {
                state.market.priceByItemId[e.Key] = MarketPriceService.RollMarketPrice(
                    e.Key, modules, ships, rng);
            }
        }
        state.market.npcStock.Clear();
        var count = rng.Next(1, 11);
        for (var i = 0; i < count; i++)
        {
            var id = pool[rng.Next(pool.Count)];
            state.market.npcStock[id] = state.market.npcStock.GetValueOrDefault(id, 0) + 1;
        }
        PushAlert(state, "市场刷新 · NPC 库存 " + count + " 件");
    }

    private static List<string> BuildTradePool(ShipRegistry ships)
    {
        var pool = new List<string>(SeedItems);
        foreach (var hull in ships.AllHulls())
        {
            if (!string.IsNullOrWhiteSpace(hull.hullId))
            {
                pool.Add(hull.hullId);
            }
        }
        return pool;
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
