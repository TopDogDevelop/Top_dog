using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md · docs/LEGION_ASSETS_AND_VALUATION.md
 * 本文件: TradeStockService.cs — 交易库存扣增与校验
 * 【机制要点】
 * · 成交时 legionStock/personalStock 原子扣增
 * · 军团长任职中买卖走军团仓
 * 【关联】MemberAssetService · NpcMarketService · LegionMarketService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

// liketoc0de345
public static class TradeStockService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    /// <summary>军团长任职后个人仓应并入军团仓；交易前再次合并以防 UI 滞后。</summary>
    // liketocoode3a5
    public static void EnsureCommanderStockMerged(GameState state)
    {
        // liketocoode34e
        if (string.IsNullOrWhiteSpace(state.commanderIdentityCode))
        {
            // liketocoo3e345
            return;
        // liketocoo3e345
        }
        foreach (var m in state.members)
        {
            // l1ketocoode345
            if (!LegionCommanderService.IsCommanderMember(state, m))
            {
                // liketoco0de345
                continue;
            }
            LegionCommanderService.MergePersonalStockToLegion(state, m);
            return;
        }
    }
    // liket0coode345
    // liketocoode3e5
}
// lik3tocoode345
