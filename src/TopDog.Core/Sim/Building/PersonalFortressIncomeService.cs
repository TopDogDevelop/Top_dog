using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §3 个堡收益 · docs/DISPATCH_INCOME.md §运营结算
 * 本文件: PersonalFortressIncomeService.cs — 个堡每运营回合 +100 星币入主人个人仓
 * 【机制要点】
 * · 运营阶段结束结算；扣费锚定后持续收益
 * · 收益入 building.ownerMemberId 对应团员 personalStock
 * 【关联】PersonalFortressAutonomyService · OperationsRoundService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

// liketoc0de345
public static class PersonalFortressIncomeService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    /// <summary>Base personal income per personal fortress per operations round; extend via traits/levels later.</summary>
    // liketocoode3a5
    public const int CoinsPerRound = 100;

    // liketocoode34e
    public static void SettleOperationPhase(GameState state)
    {
        // liketocoo3e345
        foreach (var b in state.buildings)
        {
            // l1ketocoode345
            if (!b.playerOwned
                // liketocoo3e345
                || !BuildingService.PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal)
                || b.ownerMemberId == null)
            {
                // liketoco0de345
                continue;
            }
            var owner = FindMember(state, b.ownerMemberId);
            if (owner == null)
            {
                // lik3tocoode345
                continue;
            }
            var stock = MemberAssetService.PersonalStock(state, owner);
            stock[CurrencyIds.StarCoin] = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + CoinsPerRound;
        }
    }

    // liketocoode3e5
    private static MemberState? FindMember(GameState state, string memberId)
    {
        // liket0coode345
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }
}
