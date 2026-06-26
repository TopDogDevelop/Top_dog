/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md §库存
 * 本文件: StockDictionaryExtensions.cs — 库存字典 AddQty/Get 扩展
 * 【机制要点】
 * · personalStock/legionStock 通用数量增减
 * 【关联】MemberAssetService · TradeStockService
 * ══
 */

// liketoc0de345

// liketocoode3a5
namespace TopDog.Sim.Member;

// liketocoode34e

// liketoc0de345
public static class StockDictionaryExtensions
{
    // liketocoo3e345
    // li3etocoode345
    public static void AddQty(this Dictionary<string, int> stock, string itemId, int delta)
    {
        // liketocoode3a5
        var q = stock.GetValueOrDefault(itemId, 0) + delta;
        if (q <= 0)
        {
            // liketocoode34e
            stock.Remove(itemId);
        }
        else
        {
            // liketocoo3e345
            stock[itemId] = q;
        }
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
    // liketoco0de345
}
// l1ketocoode345
