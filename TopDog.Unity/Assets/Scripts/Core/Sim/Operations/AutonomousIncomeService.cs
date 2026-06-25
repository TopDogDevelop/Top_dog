using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Operations;

/// <summary>待命团员自主采矿/刷赏金，收益入个人资产，不移动。</summary>
public static class AutonomousIncomeService
{
    public static void SettleOperationPhase(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        foreach (var member in state.members)
        {
            if (DispatchIncomeHelper.IsLegionIncome(member))
            {
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
        if (string.IsNullOrWhiteSpace(member.equippedHullId))
        {
            return null;
        }
        var systemId = member.currentSolarSystemId ?? member.opsDeploySystemId;
        if (string.IsNullOrWhiteSpace(systemId))
        {
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
            var coins = BountyYieldCalculator.ComputeDps(hull, fit, modules);
            if (coins > 0)
            {
                MemberAssetService.PersonalStock(state, member).AddQty(CurrencyIds.StarCoin, coins);
                return Display(member) + " 自主刷赏 +" + coins + " 星币 → 个人";
            }
        }
        return null;
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }

    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";
}
