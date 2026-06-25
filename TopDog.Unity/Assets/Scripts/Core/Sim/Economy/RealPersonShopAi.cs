using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

public static class RealPersonShopAi
{
    public static void Run(GameState state, ModuleRegistry modules, ShipRegistry ships)
    {
        var rng = new Random((int)(state.gameWeek * 53L + state.storyRound));
        var identities = state.identities.Keys.ToList();
        foreach (var code in identities)
        {
            if (rng.NextDouble() > 0.5)
            {
                continue;
            }
            var members = state.members.Where(m => IdentityCodes.Of(m) == code).ToList();
            if (members.Count == 0)
            {
                continue;
            }
            foreach (var m in members)
            {
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
        if (string.IsNullOrWhiteSpace(m.equippedHullId))
        {
            return;
        }
        var hull = ships.FindHull(m.equippedHullId);
        if (hull == null)
        {
            return;
        }
        var fit = MemberFittingService.Fittings(state, m);
        foreach (var slot in MemberFittingService.ListOpenSlots(hull))
        {
            if (rng.NextDouble() > 0.5 || fit.ContainsKey(slot))
            {
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

    private static void ListUnfittedGear(
        GameState state,
        MemberState m,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        var stock = MemberAssetService.PersonalStock(state, m);
        foreach (var e in stock.ToList())
        {
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
