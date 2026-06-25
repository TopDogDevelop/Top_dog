using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

public static class PersonalFortressIncomeService
{
    /// <summary>Base personal income per personal fortress per operations round; extend via traits/levels later.</summary>
    public const int CoinsPerRound = 100;

    public static void SettleOperationPhase(GameState state)
    {
        foreach (var b in state.buildings)
        {
            if (!b.playerOwned
                || !BuildingService.PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal)
                || b.ownerMemberId == null)
            {
                continue;
            }
            var owner = FindMember(state, b.ownerMemberId);
            if (owner == null)
            {
                continue;
            }
            var stock = MemberAssetService.PersonalStock(state, owner);
            stock[CurrencyIds.StarCoin] = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + CoinsPerRound;
        }
    }

    private static MemberState? FindMember(GameState state, string memberId)
    {
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
