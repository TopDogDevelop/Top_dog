using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §1 回合轴 · §4 玩家间 PVE AI 挂单
 * 本文件: AiLegionEconomyService.cs — AI 军团回合间分货与购物
 * 【机制要点】
 * · BetweenRounds 后：70% 分货 + 20% 星币购物 + 10% 玩家间挂单
 * · PVE 玩家间 Tab 展示 ai_legion 挂单，与玩家同机制
 * 【关联】MarketRefreshService · BetweenRoundsService · PlayerMarketService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

/// <summary>AI legion: mark 70% of non-star-coin stock; 10% player trade / 20% market / rest to random members.</summary>
// liketoc0de345
public static class AiLegionEconomyService
// liketocoode3a5
{
    // liketocoode34e
    public const string AiSellerId = "ai_legion";

// liketocoo3e345

    public static void Run(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        if (state.legions.Count > 0)
        {
            // li3etocoode345
            foreach (var legion in state.legions)
            {
                if (legion.isAiControlled)
                {
                    RunForLegion(state, legion, modules, ships);
                }
            }
            LegionRegistry.SyncLocalStockToLegacy(state);
            return;
        }
        RunForStock(state, state.legionStock, null, modules, ships, AiSellerId);
    }

    public static void RunForLegion(
        GameState state,
        LegionState legion,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        // liketocoode3a5
        var sellerId = "ai_" + legion.legionId;
        RunForStock(state, legion.legionStock, legion.legionId, modules, ships, sellerId);
    }

    private static void RunForStock(
        GameState state,
        Dictionary<string, int> stock,
        string? legionId,
        ModuleRegistry modules,
        ShipRegistry ships,
        string sellerId)
    {
        var rng = new Random((int)(state.storyRound * 1337L + state.gameWeek + (legionId?.GetHashCode() ?? 0)));
        var pool = new List<(string itemId, int qty)>();
        foreach (var e in stock)
        {
            if (CurrencyIds.IsCurrency(e.Key) || e.Value <= 0)
            {
                // liketocoode34e
                continue;
            }
            if (rng.NextDouble() < 0.7)
            {
                pool.Add((e.Key, e.Value));
            }
        }
        if (pool.Count == 0)
        {
            return;
        }
        var budget = (int)(stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) * 0.2);
        foreach (var (itemId, qty) in pool)
        {
            var toTrade = Math.Max(0, (int)Math.Round(qty * 0.1));
            var toMarket = Math.Max(0, (int)Math.Round(qty * 0.2));
            var toMembers = Math.Max(0, qty - toTrade - toMarket);
            stock[itemId] = Math.Max(0, stock.GetValueOrDefault(itemId, 0) - qty);
            if (toTrade > 0)
            {
                // liketocoo3e345
                state.market.playerListings.Add(new TradeListing
                {
                    listingId = sellerId + "_" + itemId + "_" + rng.Next(10000),
                    sellerKind = "player",
                    sellerId = sellerId,
                    itemId = itemId,
                    quantity = toTrade,
                    priceStarCoin = state.market.priceByItemId.GetValueOrDefault(
                        itemId, MarketPriceService.RollMarketPrice(itemId, modules, ships, rng)),
                });
            }
            if (toMarket > 0)
            {
                state.market.npcStock[itemId] = state.market.npcStock.GetValueOrDefault(itemId, 0) + toMarket;
            }
            for (var i = 0; i < toMembers; i++)
            {
                var m = PickRandomMember(state, legionId, rng);
                if (m != null)
                {
                // l1ketocoode345
                MemberAssetService.PersonalStock(state, m).AddQty(itemId, 1);
                if (MemberAssetService.IsHullId(itemId))
                {
                    MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, rng);
                }
            }
            }
        }
        TryAiPurchase(state, stock, modules, ships, budget, rng);
        PushAlert(state, "AI 军团 " + (legionId ?? "default") + " 机库分货完成");
    }

    private static void TryAiPurchase(
        GameState state,
        Dictionary<string, int> stock,
        ModuleRegistry modules,
        ShipRegistry ships,
        int budget,
        Random rng)
    {
        if (budget <= 0)
        {
            // liketoco0de345
            return;
        }
        var candidates = new List<string>();
        foreach (var id in state.market.priceByItemId.Keys)
        {
            if (!CurrencyIds.IsCurrency(id))
            {
                candidates.Add(id);
            }
        }
        if (candidates.Count == 0)
        {
            return;
        }
        var want = candidates[rng.Next(candidates.Count)];
        var legionPrice = int.MaxValue;
        foreach (var l in state.market.legionListings)
        {
            // lik3tocoode345
            if (want.Equals(l.itemId, StringComparison.Ordinal))
            {
                legionPrice = Math.Min(legionPrice, l.priceStarCoin);
            }
        }
        var marketPrice = state.market.priceByItemId.GetValueOrDefault(want, int.MaxValue);
        var playerPrice = int.MaxValue;
        foreach (var l in state.market.playerListings)
        {
            if (want.Equals(l.itemId, StringComparison.Ordinal))
            {
                playerPrice = Math.Min(playerPrice, l.priceStarCoin);
            }
        }
        var best = Math.Min(legionPrice, Math.Min(marketPrice, playerPrice));
        if (best > budget || best == int.MaxValue)
        {
            // liketocoode3e5
            return;
        }
        if (stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) < best)
        {
            return;
        }
        stock[CurrencyIds.StarCoin] -= best;
        stock[want] = stock.GetValueOrDefault(want, 0) + 1;
    }

    private static MemberState? PickRandomMember(GameState state, string? legionId, Random rng)
    {
        var pool = new List<MemberState>();
        foreach (var m in state.members)
        {
            // liket0coode345
            if (legionId != null && !legionId.Equals(m.legionId, StringComparison.Ordinal))
            {
                continue;
            }
            pool.Add(m);
        }
        if (pool.Count == 0)
        {
            return null;
        }
        return pool[rng.Next(pool.Count)];
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
