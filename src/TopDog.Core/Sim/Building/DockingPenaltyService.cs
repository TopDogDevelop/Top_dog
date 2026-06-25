using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

public static class DockingPenaltyService
{
    private const int DreadnoughtMinRank = 9;

    public static void Refresh(GameState state, ShipRegistry? ships)
    {
        if (ships == null)
        {
            return;
        }
        if (BuildingService.HasPlayerLegionFortress(state))
        {
            return;
        }
        if (!BuildingService.HasPlayerPersonalFortress(state))
        {
            return;
        }
        var stripped = 0;
        foreach (var m in state.members)
        {
            if (string.IsNullOrWhiteSpace(m.equippedHullId))
            {
                continue;
            }
            var hull = ships.FindHull(m.equippedHullId);
            if (hull?.tonnageClass == null)
            {
                continue;
            }
            if (CombatPowerCalculator.TonnageRankOf(hull.tonnageClass) < DreadnoughtMinRank)
            {
                continue;
            }
            m.equippedHullId = null;
            stripped++;
        }
        if (stripped > 0)
        {
            PushAlert(state, "仅个堡可停靠：已损失 " + stripped + " 艘无畏及以上舰船");
        }
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
