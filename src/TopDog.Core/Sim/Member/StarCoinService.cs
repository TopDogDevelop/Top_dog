using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class StarCoinService
{
    public static void SyncMemberFundsToStock(GameState state, MemberState m)
    {
        if (m.funds <= 0)
        {
            return;
        }
        var stock = MemberAssetService.PersonalStock(state, m);
        var existing = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0);
        if (existing < m.funds)
        {
            stock[CurrencyIds.StarCoin] = m.funds;
        }
    }

    public static void SyncAllMemberFunds(GameState state)
    {
        foreach (var m in state.members)
        {
            SyncMemberFundsToStock(state, m);
        }
    }
}
