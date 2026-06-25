using TopDog.Content.Modules;
using TopDog.Content.Ships;

namespace TopDog.Sim.Member;

/// <summary>星币估值：内容可覆写 <see cref="HullDef.starCoinValue"/> / <see cref="ModuleDef.starCoinValue"/>，否则按吨位或尺寸默认。</summary>
public static class AssetValuation
{
    public const int StarCoinPerUnit = 1;

    public const int DefaultCarrierDreadValue = 50_000;
    public const int DefaultBattlecruiserValue = 5_000;
    public const int DefaultHullFallbackValue = 1_000;

    public const int DefaultSmallModuleValue = 60;
    public const int DefaultMediumModuleValue = 600;
    public const int DefaultLargeModuleValue = 6_000;
    public const int DefaultExtraLargeModuleValue = 60_000;
    public const int DefaultYouliangModuleValue = 600_000;

    public static int HullStarCoinValue(HullDef? hull)
    {
        if (hull == null)
        {
            return 0;
        }
        if (hull.starCoinValue > 0)
        {
            return hull.starCoinValue;
        }
        return hull.tonnageClass switch
        {
            "CARRIER" or "DREADNOUGHT" => DefaultCarrierDreadValue,
            "BATTLECRUISER" => DefaultBattlecruiserValue,
            _ => DefaultHullFallbackValue,
        };
    }

    public static int ModuleStarCoinValue(ModuleDef? mod)
    {
        if (mod == null)
        {
            return 0;
        }
        if (mod.starCoinValue > 0)
        {
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
        if (modules != null && MemberFittingService.IsEquippableModuleId(itemId, modules))
        {
            return ModuleStarCoinValue(modules.Resolve(itemId));
        }
        return 0;
    }

    public static string FormatStarCoinValue(int value) => value.ToString() + " 星币";
}
