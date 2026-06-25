using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

public static class TradeStockService
{
    /// <summary>军团长任职后个人仓应并入军团仓；交易前再次合并以防 UI 滞后。</summary>
    public static void EnsureCommanderStockMerged(GameState state)
    {
        if (string.IsNullOrWhiteSpace(state.commanderIdentityCode))
        {
            return;
        }
        foreach (var m in state.members)
        {
            if (!LegionCommanderService.IsCommanderMember(state, m))
            {
                continue;
            }
            LegionCommanderService.MergePersonalStockToLegion(state, m);
            return;
        }
    }
}
