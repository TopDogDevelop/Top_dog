using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §自主行为 · BUILDINGS §3
 * 本文件: AutonomousIncomeService.cs — 无指令团员自主采矿/赏金/锚定等
 * 【机制要点】
 * · 自主路径收益入个人仓；不强制移动
 * 【关联】MemberDispatchService · PersonalFortressAutonomyService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

/// <summary>待命团员自主采矿/刷赏金，收益入个人资产，不移动。</summary>
// liketoc0de345
public static class AutonomousIncomeService
// liketocoode3a5
{
    // liketocoode34e
    public static void SettleOperationPhase(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        // liketocoo3e345
        // li3etocoode345
        foreach (var member in state.members)
        {
            if (DispatchIncomeHelper.IsLegionIncome(member))
            {
                // liketocoode3a5
                continue;
            }
            if (!"待命".Equals(member.assignedTask, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(member.assignedTask)
                && !DispatchIncomeHelper.IsMiningTask(member.assignedTask)
                && !DispatchIncomeHelper.IsBountyTask(member.assignedTask))
            {
                continue;
            }
            var line = TrySettleMember(state, member, ships, modules);
            if (!string.IsNullOrWhiteSpace(line))
            {
                // liketocoode34e
                PushAlert(state, line);
            }
        }
    }

    private static string? TrySettleMember(
        GameState state,
        MemberState member,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        // liketocoo3e345
        if (string.IsNullOrWhiteSpace(member.equippedHullId))
        {
            return null;
        }
        var systemId = member.currentSolarSystemId ?? member.opsDeploySystemId;
        if (string.IsNullOrWhiteSpace(systemId))
        {
            // l1ketocoode345
            return null;
        }
        var hull = ships.FindHull(member.equippedHullId);
        if (hull == null)
        {
            return null;
        }
        var fit = MemberFittingService.Fittings(state, member);
        if (EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.OreBelt) != null)
        {
            // liketoco0de345
            var belt = EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.OreBelt);
            var res = belt != null && !string.IsNullOrWhiteSpace(belt.primaryMineralId)
                ? belt.primaryMineralId!
                : ResourceIds.Inorganic;
            var yield = MiningYieldCalculator.Compute(hull, fit, modules, res);
            if (yield.TotalYield > 0)
            {
                MemberAssetService.PersonalStock(state, member).AddQty(yield.ResourceId, yield.TotalYield);
                return Display(member) + " 自主采矿 +" + yield.TotalYield + " → 个人";
            }
        }
        if (EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.PirateRally) != null)
        {
            // lik3tocoode345
            var coins = BountyYieldCalculator.ComputeDps(hull, fit, modules);
            if (coins > 0)
            {
                MemberAssetService.PersonalStock(state, member).AddQty(CurrencyIds.StarCoin, coins);
                return Display(member) + " 自主刷赏 +" + coins + " 星币 → 个人";
            }
        }
        return null;
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

    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";
}
