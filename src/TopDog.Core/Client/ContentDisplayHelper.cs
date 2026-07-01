using TopDog.Content.Modules;
using TopDog.Content.Ships;

namespace TopDog.Client;

public static class ContentDisplayHelper
{
    public const string DefaultModuleBrief = "平平无奇的制式装备";

    public static string ModuleBrief(ModuleDef? module) =>
        string.IsNullOrWhiteSpace(module?.moduleBrief) ? DefaultModuleBrief : module.moduleBrief!;

    public static string HullBrief(HullDef? hull) =>
        string.IsNullOrWhiteSpace(hull?.hullBrief) ? "" : hull.hullBrief!;
}
