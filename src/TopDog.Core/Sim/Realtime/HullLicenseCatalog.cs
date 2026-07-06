using TopDog.Content.Modules;
using TopDog.Content.Ships;
using System.Linq;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIP_FITTING.md §许可 · CONTENT_FORMAT.md
 * 本文件: HullLicenseCatalog.cs — 船体许可键与配装校验
 * 【机制要点】
 * · requiredHullLicenses ⊆ hullLicenses
 * · boarding_module 向后兼容 allowedModuleKinds 或 boarding 许可
 * 【关联】FittingValidator · ModuleDef · HullDef
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class HullLicenseCatalog
{
    public const string Logistics = "logistics";
    public const string Boarding = "boarding";
    public const string ShieldFleet = "shield_fleet";
    public const string ArmorFleet = "armor_fleet";
    public const string AntiMissileLaser = "anti_missile_laser";

    public const string FleetProtectionFamily = "fleet_protection_field";

    public static bool ModuleFitsHullLicenses(HullDef? hull, ModuleDef mod)
    {
        if ("boarding_module".Equals(mod.moduleKind, StringComparison.Ordinal))
        {
            if (HullHasLicense(hull, Boarding))
            {
                return true;
            }

            return hull?.allowedModuleKinds != null
                   && hull.allowedModuleKinds.Any(k =>
                       "boarding_module".Equals(k, StringComparison.Ordinal));
        }

        if (mod.requiredHullLicenses == null || mod.requiredHullLicenses.Length == 0)
        {
            return true;
        }

        if (hull?.hullLicenses == null || hull.hullLicenses.Length == 0)
        {
            return false;
        }

        foreach (var required in mod.requiredHullLicenses)
        {
            if (string.IsNullOrWhiteSpace(required))
            {
                continue;
            }

            var found = false;
            foreach (var license in hull.hullLicenses)
            {
                if (required.Equals(license, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    public static bool HullHasLicense(HullDef? hull, string licenseKey)
    {
        if (hull?.hullLicenses == null)
        {
            return false;
        }

        foreach (var license in hull.hullLicenses)
        {
            if (licenseKey.Equals(license, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsFleetProtectionModule(ModuleDef? mod) =>
        mod != null
        && (FleetProtectionFamily.Equals(mod.moduleFamily, StringComparison.Ordinal)
            || "shield_fusion_field".Equals(mod.moduleKind, StringComparison.Ordinal)
            || "armor_link_field".Equals(mod.moduleKind, StringComparison.Ordinal));
}
