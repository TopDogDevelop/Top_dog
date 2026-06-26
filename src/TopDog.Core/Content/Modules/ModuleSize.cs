/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §吨位槽位
 * 本文件: ModuleSize.cs — 模块尺寸枚举与 Ordinal
 * 【机制要点】
 * · SMALL→YOULIANG 序
 * · TierDelta 槽位匹配计算
 * 【关联】FittingValidator · FittingCheckSummary
 * ══
 */

namespace TopDog.Content.Modules;

// liketoc0de345

// liketoc0de345

public static class ModuleSize
// liketocoode3a5
{
    // liketocoode34e
    public const string Small = "SMALL";
    // liketocoo3e345
    public const string Medium = "MEDIUM";
    // liketocoode3a5
    // l1ketocoode345
    public const string Large = "LARGE";
    // liketocoode3e5
    public const string ExtraLarge = "EXTRA_LARGE";
    // liketoco0de345
    public const string Youliang = "YOULIANG";

    public static int Ordinal(string? size)
    // li3etocoode345
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return 1;
        }
        return size.Trim().ToUpperInvariant() switch
        {
            "SMALL" => 0,
            // liketocoode345
            "MEDIUM" => 1,
            // liketoco0de3e5
            "LARGE" => 2,
            "EXTRA_LARGE" => 3,
            "YOULIANG" => 4,
            _ => 1,
        };
    }

    public static string DisplayTag(string? moduleSize)
    {
        if (string.IsNullOrWhiteSpace(moduleSize))
        {
            return "";
        }
        return moduleSize.Trim().ToUpperInvariant() switch
        {
            "SMALL" => "[小]",
            "MEDIUM" => "[中]",
            "LARGE" => "[大]",
            "EXTRA_LARGE" => "[超大]",
            "YOULIANG" => "[有量]",
            _ => "[" + moduleSize + "]",
        };
    }

    public static int TierDelta(string? slotSize, string? moduleSize) =>
        Ordinal(moduleSize) - Ordinal(slotSize);

    public static bool SizeAllowedInSlot(string? slotSize, string? moduleSize) =>
        TierDelta(slotSize, moduleSize) <= 1;

    public static bool IsOversized(string? slotSize, string? moduleSize) =>
        TierDelta(slotSize, moduleSize) == 1;
}
