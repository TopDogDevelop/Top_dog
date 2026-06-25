using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Operations;

public static class BountySettlementService
{
    public static void SettleOperationPhase(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        foreach (var member in state.members)
        {
            var line = SettleMember(state, member, ships, modules);
            if (!string.IsNullOrWhiteSpace(line))
            {
                PushAlert(state, line);
            }
        }
    }

    private static string? SettleMember(
        GameState state,
        MemberState member,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        if (!DispatchIncomeHelper.IsBountyTask(member.assignedTask))
        {
            return null;
        }
        if (!DispatchIncomeHelper.IsLegionIncome(member))
        {
            return null;
        }
        var systemId = member.currentSolarSystemId ?? member.opsDeploySystemId;
        if (string.IsNullOrWhiteSpace(systemId))
        {
            return null;
        }
        if (!HasPirateRallyAt(state, systemId, member.opsDeployEventRegionId))
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(member.equippedHullId))
        {
            return null;
        }
        var hull = ships.FindHull(member.equippedHullId);
        if (hull == null)
        {
            return null;
        }
        var fit = MemberFittingService.Fittings(state, member);
        var coins = BountyYieldCalculator.ComputeDps(hull, fit, modules);
        if (coins <= 0)
        {
            return null;
        }
        DispatchIncomeHelper.CreditLegion(state, CurrencyIds.StarCoin, coins);
        return Display(member) + " 刷赏金 +" + coins + " 星币 → 军团机库";
    }

    private static bool HasPirateRallyAt(GameState state, string systemId, string? regionId)
    {
        if (!string.IsNullOrWhiteSpace(regionId))
        {
            var er = EventRegionPicker.FindRegion(state, systemId, regionId);
            return er != null && EventRegionKinds.PirateRally.Equals(er.kind, StringComparison.Ordinal);
        }
        return EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.PirateRally) != null;
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
