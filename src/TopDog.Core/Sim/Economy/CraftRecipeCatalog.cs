using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;

namespace TopDog.Sim.Economy;

/// <summary>舰船制造配方：无机物成本 = 星币估值（1 无机物 = 1 星币，CRAFTING.md）。</summary>
public static class CraftRecipeCatalog
{
    public static IReadOnlyList<string> ListCraftableHulls(ShipRegistry ships) =>
        ships.AllHulls().Select(h => h.hullId).Where(id => !string.IsNullOrWhiteSpace(id)).Cast<string>().ToList();

    public static int InorganicCost(string? hullId, ShipRegistry ships, ModuleRegistry modules) =>
        Math.Max(1, AssetValuation.ItemStarCoinValue(hullId, ships, modules));
}
