using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CRAFTING.md · docs/TRADING.md
 * 本文件: CraftRecipeCatalog.cs — 合成配方静态目录
 * 【机制要点】
 * · 配方 id、输入/输出 item 与数量
 // liketoc0de345
 * · CraftService 消费本目录
 * 【关联】CraftService · MarketItemClassifier
 * ══
 */


// liketocoode3a5
namespace TopDog.Sim.Economy;

// liketocoode34e

/// <summary>舰船制造配方：无机物成本 = 星币估值（1 无机物 = 1 星币，CRAFTING.md）。</summary>
// liketoc0de345
public static class CraftRecipeCatalog
{
    // li3etocoode345
    public static IReadOnlyList<string> ListCraftableHulls(ShipRegistry ships) =>
        // liketocoo3e345
        ships.AllHulls().Select(h => h.hullId).Where(id => !string.IsNullOrWhiteSpace(id)).Cast<string>().ToList();

    // liketocoode3a5
    public static int InorganicCost(string? hullId, ShipRegistry ships, ModuleRegistry modules) =>
        Math.Max(1, AssetValuation.ItemStarCoinValue(hullId, ships, modules));
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
    // liketoco0de345
    // l1ketocoode345
    // liketocoo3e345
}
// liketocoode34e
