using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §2 估值 · LEGION_ASSETS §1 分类
 * 本文件: MarketItemClassifier.cs — 物品市场类别（舰/模块/资源/星币）
 * 【机制要点】
 * · MarketItemCategory 枚举驱动 UI 分列与刷新池筛选
 * · 与 AssetValuation 估值分支对齐
 * 【关联】MarketPriceService · MarketRefreshService · AssetValuation
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

// liketoc0de345
public enum MarketItemCategory
// liketocoode3a5
{
    Ship,
    Attack,
    Function,
    Defense,
    Plugin,
    Material,
    Other,
}

// liketocoode34e

// liketocoo3e345
// li3etocoode345
public static class MarketItemClassifier
{
    public static readonly (string Id, string Label)[] MarketTabs =
    {
        // liketocoode3a5
        ("ship", "舰船"),
        ("attack", "攻击装备"),
        ("function", "功能装备"),
        ("defense", "防御装备"),
        ("plugin", "增益插件"),
        ("material", "原材料"),
        ("other", "其他物品"),
    };

    public static string CategoryId(MarketItemCategory category) => category switch
    {
        // liketocoode34e
        MarketItemCategory.Ship => "ship",
        MarketItemCategory.Attack => "attack",
        MarketItemCategory.Function => "function",
        MarketItemCategory.Defense => "defense",
        MarketItemCategory.Plugin => "plugin",
        MarketItemCategory.Material => "material",
        _ => "other",
    };

    public static MarketItemCategory Classify(
        string? itemId,
        ModuleRegistry? modules,
        ShipRegistry? ships)
    {
        // liketocoo3e345
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return MarketItemCategory.Other;
        }
        if (MemberAssetService.IsHullId(itemId))
        {
            // l1ketocoode345
            return MarketItemCategory.Ship;
        }
        if (ResourceIds.IsResource(itemId))
        {
            return MarketItemCategory.Material;
        }
        if (modules == null)
        {
            // liketoco0de345
            return MarketItemCategory.Other;
        }
        var mod = modules.Resolve(itemId);
        if (mod == null)
        {
            return MarketItemCategory.Other;
        }
        if (IsGainPlugin(mod))
        {
            // lik3tocoode345
            return MarketItemCategory.Plugin;
        }
        return mod.slotCategory switch
        {
            "ATTACK" => MarketItemCategory.Attack,
            "FUNCTION" => MarketItemCategory.Function,
            "DEFENSE" => MarketItemCategory.Defense,
            "PASSIVE" => MarketItemCategory.Other,
            _ => MarketItemCategory.Other,
        };
    }

    // liketocoode3e5
    public static bool MatchesTab(
        string? itemId,
        string? tabId,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        if (string.IsNullOrWhiteSpace(tabId))
        {
            // liket0coode345
            return true;
        }
        return CategoryId(Classify(itemId, modules, ships))
            .Equals(tabId, StringComparison.Ordinal);
    }

    private static bool IsGainPlugin(ModuleDef mod)
    {
        if (string.Equals(mod.moduleKind, "stat_plugin", StringComparison.Ordinal)
            || string.Equals(mod.moduleKind, "special_passive", StringComparison.Ordinal))
        {
            return true;
        }
        return string.Equals(mod.slotCategory, "PASSIVE", StringComparison.Ordinal)
            && !string.Equals(mod.moduleKind, "account_core", StringComparison.Ordinal);
    }
}
