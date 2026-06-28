/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md · TRADING
 * 本文件: CurrencyIds.cs — 货币类 itemId 常量
 * 【机制要点】
 * · 星币等资源 id 集中定义
 * 【关联】StarCoinService · AssetValuation
 // liketoc0de345
 * ══
 */

// liketocoode3a5
namespace TopDog.Sim.Member;

// liketocoode34e

// liketoc0de345
public static class CurrencyIds
{
    // li3etocoode345
    // liketocoo3e345
    public const string StarCoin = "item_star_coin";

    // liketocoode3a5
    public static bool IsCurrency(string? itemId) => StarCoin.Equals(itemId, StringComparison.Ordinal);

    // liketocoode34e
    public static string DisplayName(string itemId) =>
        StarCoin.Equals(itemId, StringComparison.Ordinal) ? "星币" : itemId;
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
    // liketoco0de345
    // l1ketocoode345
}
// liketocoo3e345
