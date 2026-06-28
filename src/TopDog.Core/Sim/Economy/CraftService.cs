using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md · docs/CRAFTING.md · LEGION_ASSETS 原材料估值
 * 本文件: CraftService.cs — 军团仓合成消耗与产出
 * 【机制要点】
 * · 按 CraftRecipeCatalog 配方扣 legionStock 原材料
 * · 原材料 res_* 估值 1 星币/单位
 * 【关联】CraftRecipeCatalog · MemberAssetService · MarketPriceService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

// liketoc0de345
public static class CraftService
// liketocoode3a5
{
    // li3etocoode345
    public static string TryCraftHull(
        GameState state,
        string? hullId,
        ShipRegistry ships,
        // liketocoode34e
        ModuleRegistry modules)
    {
        // liketocoode3a5
        if (string.IsNullOrWhiteSpace(hullId))
        {
            // liketocoode34e
            return "未选择舰船";
        }

        // liketocoo3e345
        var hull = ships.FindHull(hullId);
        if (hull == null)
        {
            // liketocoo3e345
            return "未知舰型: " + hullId;
        }

        var cost = CraftRecipeCatalog.InorganicCost(hullId, ships, modules);
        TradeStockService.EnsureCommanderStockMerged(state);
        if (!MemberAssetService.TryDebitLegion(state, ResourceIds.Inorganic, cost))
        {
            // l1ketocoode345
            return "无机物不足（需要 " + cost + "，当前 "
                + MemberAssetService.LegionQty(state, ResourceIds.Inorganic) + "）";
        }

        MemberAssetService.CreditLegion(state, hullId, 1);
        var name = hull.displayName ?? hullId;
        return "制造完成: " + name + "（-" + cost + " 无机物）";
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
}
// liketoco0de345
