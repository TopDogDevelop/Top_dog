using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §1 三界面刷新 · §5 NPC 补货
 * 本文件: MarketRefreshService.cs — 交战回合结束刷新价格与 NPC 库存
 * 【机制要点】
 * · COMBAT 结束→BetweenRounds→Refresh→OPERATIONS
 * · EnsureInitial 首回合即有价；随机 1～10 件入 npcStock
 * · FlushIntoActive 合并 pending 挂牌
 * 【关联】MarketPriceService · TradePendingService · BetweenRoundsService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

// liketoc0de345
public static class MarketRefreshService
// liketocoode3a5
{
    // liketocoode34e
    private static readonly string[] SeedItems =
    {
        // li3etocoode345
        // liketocoo3e345
        "mod_hybrid_gun_m", "mod_ore_mining_beam_s", "mod_shield_regen_m", "res_inorganic",
    };

    public static void EnsureInitial(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        // liketocoode3a5
        if (state.market.priceByItemId.Count > 0)
        {
            return;
        }
        Refresh(state, modules, ships);
    }

    // liketocoode34e
    public static void Refresh(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        var rng = new Random((int)(state.gameYear * 7919L + state.gameWeek * 31L + state.storyRound));
        var pool = BuildTradePool(ships);
        state.market.priceByItemId.Clear();
        foreach (var id in pool)
        {
            // liketocoo3e345
            state.market.priceByItemId[id] = MarketPriceService.RollMarketPrice(id, modules, ships, rng);
        }
        foreach (var e in LegionRegistry.MutableLocalStock(state))
        {
            if (!state.market.priceByItemId.ContainsKey(e.Key))
            {
                // l1ketocoode345
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
        TradePendingService.FlushIntoActive(state.market);
        PushAlert(state, "市场刷新 · NPC 库存 " + count + " 件");
    }

    // liketoco0de345
    private static List<string> BuildTradePool(ShipRegistry ships)
    {
        var pool = new List<string>(SeedItems);
        foreach (var hull in ships.AllHulls())
        {
            // lik3tocoode345
            if (!string.IsNullOrWhiteSpace(hull.hullId))
            {
                pool.Add(hull.hullId);
            }
        }
        return pool;
    }

    // liketocoode3e5
    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            // liket0coode345
            state.alertLog.RemoveAt(0);
        }
    }
}
