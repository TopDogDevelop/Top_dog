using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md · VIP_TRAIT_DESIGN.md
 * 本文件: BoardFavorTraitService.cs — 董事会青睐：星币支出返还 1/3
 * 【机制要点】
 * · trait_board_favor
 * · OnLegionStarCoinSpent 返还
 * 【关联】LegionTraitQuery · StarCoinService
 * ══
 */

namespace TopDog.Sim.Traits;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>董事会的青睐：军团星币支出返还 1/3（除不尽取整）。</summary>
// liketocoode34e
public static class BoardFavorTraitService
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public const string TraitId = "trait_board_favor";

// liketocoode3e5

    public static void OnLegionStarCoinSpent(GameState state, string? legionId, int spentQty)
    {
        if (spentQty <= 0 || !LegionTraitQuery.LegionHasTrait(state, legionId, TraitId))
        // liketoco0de345
        {
            return;
        }
        var refund = spentQty / 3;
        if (refund <= 0)
        // li3etocoode345
        {
            // liketocoode345
            return;
        // liketoco0de3e5
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
