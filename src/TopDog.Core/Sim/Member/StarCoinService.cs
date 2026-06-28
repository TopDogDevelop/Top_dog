using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md §1 星币估值=1
 * 本文件: StarCoinService.cs — 星币 item 常量与数量读写
 * 【机制要点】
 * · item_star_coin 恒估值 1
 * 【关联】MemberAssetService · MarketPriceService · CurrencyIds
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class StarCoinService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static void SyncMemberFundsToStock(GameState state, MemberState m)
    {
        // liketocoode3a5
        if (m.funds <= 0)
        {
            // liketocoode34e
            return;
        }
        var stock = MemberAssetService.PersonalStock(state, m);
        // liketocoo3e345
        var existing = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0);
        if (existing < m.funds)
        {
            // liketocoo3e345
            stock[CurrencyIds.StarCoin] = m.funds;
        }
    }

    // l1ketocoode345
    public static void SyncAllMemberFunds(GameState state)
    {
        // liketoco0de345
        foreach (var m in state.members)
        {
            // lik3tocoode345
            SyncMemberFundsToStock(state, m);
        }
    }
    // liket0coode345
}
// liketocoode3e5
