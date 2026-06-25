using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Traits;

/// <summary>董事会的青睐：军团星币支出返还 1/3（除不尽取整）。</summary>
public static class BoardFavorTraitService
{
    public const string TraitId = "trait_board_favor";

    public static void OnLegionStarCoinSpent(GameState state, string? legionId, int spentQty)
    {
        if (spentQty <= 0 || !LegionTraitQuery.LegionHasTrait(state, legionId, TraitId))
        {
            return;
        }
        var refund = spentQty / 3;
        if (refund <= 0)
        {
            return;
        }
        CreditLegion(state, legionId, refund);
        PushAlert(state, "董事会的青睐：星币支出返还 " + refund);
    }

    private static void CreditLegion(GameState state, string? legionId, int qty)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            Operations.DispatchIncomeHelper.CreditLegion(state, CurrencyIds.StarCoin, qty);
            return;
        }
        var legion = Legion.LegionRegistry.Find(state, legionId);
        if (legion == null)
        {
            return;
        }
        legion.legionStock[CurrencyIds.StarCoin] =
            legion.legionStock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + qty;
        if (legion.isLocal)
        {
            Legion.LegionRegistry.SyncLocalStockToLegacy(state);
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
