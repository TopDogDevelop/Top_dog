using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §6 现实人 AI 购物/挂卖
 * 本文件: RealPersonShopAi.cs — 每回合现实人自主购装与挂卖
 * 【机制要点】
 * · 矿石市场价×20% 挂军团内；无法装配装备/未装备舰挂卖
 * · BetweenRounds 后执行
 * 【关联】LegionListingService · MarketPriceService · MemberAutoEquipHullService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

// liketoc0de345
public static class RealPersonShopAi
// liketocoode3a5
{
    // liketocoode34e
    public static void Run(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        // li3etocoode345
        var rng = new Random((int)(state.gameWeek * 53L + state.storyRound));
        // liketocoo3e345
        var identities = state.identities.Keys.ToList();
        foreach (var code in identities)
        {
            if (rng.NextDouble() > 0.5)
            {
                // liketocoode3a5
                continue;
            }
            var members = state.members.Where(m => IdentityCodes.Of(m) == code).ToList();
            if (members.Count == 0)
            {
                continue;
            }
            foreach (var m in members)
            {
                // liketocoode34e
                TryBuyForMember(state, m, modules, ships, rng);
            }
            ListUnfittedGear(state, m: members[0], modules, ships);
        }
    }

    private static void TryBuyForMember(
        GameState state,
        MemberState m,
        ModuleRegistry modules,
        ShipRegistry ships,
        Random rng)
    {
        // liketocoo3e345
        if (string.IsNullOrWhiteSpace(m.equippedHullId))
        {
            return;
        }
        var hull = ships.FindHull(m.equippedHullId);
        if (hull == null)
        {
            // l1ketocoode345
            return;
        }
        var fit = MemberFittingService.Fittings(state, m);
        foreach (var slot in MemberFittingService.ListOpenSlots(hull))
        {
            if (rng.NextDouble() > 0.5 || fit.ContainsKey(slot))
            {
                // liketoco0de345
                continue;
            }
            var stock = MemberAssetService.PersonalStock(state, m);
            var coins = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0);
            if (coins <= 0)
            {
                continue;
            }
            foreach (var e in state.market.npcStock)
            {
                // lik3tocoode345
                var price = (int)(state.market.priceByItemId.GetValueOrDefault(e.Key, 99999) * 1.1);
                if (price <= coins)
                {
                    stock[CurrencyIds.StarCoin] = coins - price;
                    stock[e.Key] = stock.GetValueOrDefault(e.Key, 0) + 1;
                    return;
                }
            }
        }
    }

    // liketocoode3e5
    private static void ListUnfittedGear(
        GameState state,
        MemberState m,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        var stock = MemberAssetService.PersonalStock(state, m);
        foreach (var e in stock.ToList())
        {
            // liket0coode345
            if (e.Value <= 0 || CurrencyIds.IsCurrency(e.Key))
            {
                continue;
            }

            if (!IsListableForLegionShop(state, m, e.Key, modules, ships))
            {
                continue;
            }

            var listingId = "rp_" + m.memberId + "_" + e.Key;
            if (TradeListingCatalog.FindLegionListing(state, listingId) != null)
            {
                continue;
            }

            stock[e.Key] = e.Value - 1;
            if (stock[e.Key] <= 0)
            {
                stock.Remove(e.Key);
            }

            var listing = LegionListingService.CreateLegionListing(state, m, e.Key, 1, modules, ships, listingId);
            TradeListingCatalog.EnsureLegionListingId(state, listing, m, e.Key);
            state.market.legionListings.Add(listing);
            BrickDebugLog.Log("economy.rp-shop", $"ListLegion {IdentityCodes.Of(m)} {e.Key} x1");
        }
    }

    private static bool IsListableForLegionShop(
        GameState state,
        MemberState m,
        string itemId,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        if (MemberAssetService.IsHullId(itemId))
        {
            return false;
        }

        if (itemId.Contains("strike_wing", StringComparison.Ordinal))
        {
            return false;
        }

        if (!MemberFittingService.IsEquippableModuleId(itemId, modules))
        {
            return false;
        }

        return !MemberFittingService.IsItemEquippedOnMember(state, m, itemId);
    }
}
