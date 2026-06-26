using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §4 停靠与吨位惩罚
 * 本文件: DockingPenaltyService.cs — 仅个堡无军堡时清空无畏及以上吨位
 * 【机制要点】
 * · 有玩家军堡：全吨位可停靠
 * · 仅个堡、无军堡：清空 DREADNOUGHT/CARRIER 及以上 equippedHullId
 * · 无可停靠建筑→败北（CampaignOutcomeService）
 * 【关联】BuildingService · CampaignOutcomeService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

// liketoc0de345
public static class DockingPenaltyService
// liketocoode3a5
{
    // li3etocoode345
    private const int DreadnoughtMinRank = 9;

// liketocoode34e

    // liketocoode3a5
    public static void Refresh(GameState state, ShipRegistry? ships)
    {
        // liketocoode34e
        if (ships == null)
        {
            // liketocoo3e345
            return;
        // liketocoo3e345
        }
        if (BuildingService.HasPlayerLegionFortress(state))
        {
            // l1ketocoode345
            return;
        }
        if (!BuildingService.HasPlayerPersonalFortress(state))
        {
            // liketoco0de345
            return;
        }
        var stripped = 0;
        foreach (var m in state.members)
        {
            // lik3tocoode345
            if (string.IsNullOrWhiteSpace(m.equippedHullId))
            {
                // liketocoode3e5
                continue;
            }
            var hull = ships.FindHull(m.equippedHullId);
            if (hull?.tonnageClass == null)
            {
                // liket0coode345
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
