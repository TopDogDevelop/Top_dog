using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

public static class CraftService
{
    public static string TryCraftHull(
        GameState state,
        string? hullId,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        if (string.IsNullOrWhiteSpace(hullId))
        {
            return "未选择舰船";
        }

        var hull = ships.FindHull(hullId);
        if (hull == null)
        {
            return "未知舰型: " + hullId;
        }

        var cost = CraftRecipeCatalog.InorganicCost(hullId, ships, modules);
        if (!MemberAssetService.TryDebitLegion(state, ResourceIds.Inorganic, cost))
        {
            return "无机物不足（需要 " + cost + "，当前 "
                + state.legionStock.GetValueOrDefault(ResourceIds.Inorganic, 0) + "）";
        }

        state.legionStock[hullId] = state.legionStock.GetValueOrDefault(hullId, 0) + 1;
        var name = hull.displayName ?? hullId;
        return "制造完成: " + name + "（-" + cost + " 无机物）";
    }
}
