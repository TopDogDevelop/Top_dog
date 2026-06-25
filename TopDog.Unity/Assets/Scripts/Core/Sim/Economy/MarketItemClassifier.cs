using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;

namespace TopDog.Sim.Economy;

public enum MarketItemCategory
{
    Ship,
    Attack,
    Function,
    Defense,
    Plugin,
    Material,
    Other,
}

public static class MarketItemClassifier
{
    public static readonly (string Id, string Label)[] MarketTabs =
    {
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
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return MarketItemCategory.Other;
        }
        if (MemberAssetService.IsHullId(itemId))
        {
            return MarketItemCategory.Ship;
        }
        if (ResourceIds.IsResource(itemId))
        {
            return MarketItemCategory.Material;
        }
        if (modules == null)
        {
            return MarketItemCategory.Other;
        }
        var mod = modules.Resolve(itemId);
        if (mod == null)
        {
            return MarketItemCategory.Other;
        }
        if (IsGainPlugin(mod))
        {
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

    public static bool MatchesTab(
        string? itemId,
        string? tabId,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        if (string.IsNullOrWhiteSpace(tabId))
        {
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
