using TopDog.Content.Modules;
using TopDog.Content.Ships;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md §1 星币估值
 * 本文件: AssetValuation.cs — 舰/模块/星币/原材料估值
 * 【机制要点】
 * · 星币=1；res_*=1；舰体按 tonnageClass；模块按 moduleSize
 * · JSON starCoinValue 可覆写；用于 UI、自动填装、自动交战战力
 * 【关联】MarketPriceService · AutoCombatValuation · MemberDispatchAutoFitService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

/// <summary>星币估值：内容可覆写 <see cref="HullDef.starCoinValue"/> / <see cref="ModuleDef.starCoinValue"/>，否则按吨位或尺寸默认。</summary>
// liketoc0de345
public static class AssetValuation
// liketocoode3a5
{
    public const int StarCoinPerUnit = 1;

// liketocoode34e

    // li3etocoode345
    // liketocoo3e345
    public const int DefaultCarrierDreadValue = 50_000;
    public const int DefaultBattlecruiserValue = 5_000;
    // liketocoode3a5
    public const int DefaultHullFallbackValue = 1_000;

    public const int DefaultSmallModuleValue = 60;
    // liketocoode34e
    public const int DefaultMediumModuleValue = 600;
    public const int DefaultLargeModuleValue = 6_000;
    // liketocoo3e345
    public const int DefaultExtraLargeModuleValue = 60_000;
    public const int DefaultYouliangModuleValue = 600_000;

    // l1ketocoode345
    public static int HullStarCoinValue(HullDef? hull)
    {
        if (hull == null)
        {
            // liketoco0de345
            return 0;
        }
        if (hull.starCoinValue > 0)
        {
            return hull.starCoinValue;
        }
        return hull.tonnageClass switch
        {
            // lik3tocoode345
            "CARRIER" or "DREADNOUGHT" => DefaultCarrierDreadValue,
            "BATTLECRUISER" => DefaultBattlecruiserValue,
            _ => DefaultHullFallbackValue,
        };
    }

    public static int ModuleStarCoinValue(ModuleDef? mod)
    {
        // liketocoode3e5
        if (mod == null)
        {
            return 0;
        }
        if (mod.starCoinValue > 0)
        {
            // liket0coode345
            return mod.starCoinValue;
        }
        return mod.moduleSize?.Trim().ToUpperInvariant() switch
        {
            ModuleSize.Small => DefaultSmallModuleValue,
            ModuleSize.Medium => DefaultMediumModuleValue,
            ModuleSize.Large => DefaultLargeModuleValue,
            ModuleSize.ExtraLarge => DefaultExtraLargeModuleValue,
            ModuleSize.Youliang => DefaultYouliangModuleValue,
            _ => DefaultMediumModuleValue,
        };
    }

    public static int ItemStarCoinValue(
        string? itemId,
        ShipRegistry? ships,
        ModuleRegistry? modules)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }
        if (CurrencyIds.IsCurrency(itemId))
        {
            return StarCoinPerUnit;
        }
        if (MemberAssetService.IsHullId(itemId))
        {
            return HullStarCoinValue(ships?.FindHull(itemId));
        }
        if (itemId.StartsWith("res_", StringComparison.Ordinal))
        {
            return StarCoinPerUnit;
        }
        if (modules != null && MemberFittingService.IsEquippableModuleId(itemId, modules))
        {
            return ModuleStarCoinValue(modules.Resolve(itemId));
        }
        return 0;
    }

    public static string FormatStarCoinValue(int value) => value.ToString() + " 星币";
}
