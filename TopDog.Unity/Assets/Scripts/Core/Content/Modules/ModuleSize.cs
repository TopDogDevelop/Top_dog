namespace TopDog.Content.Modules;

public static class ModuleSize
{
    public const string Small = "SMALL";
    public const string Medium = "MEDIUM";
    public const string Large = "LARGE";
    public const string ExtraLarge = "EXTRA_LARGE";
    public const string Youliang = "YOULIANG";

    public static int Ordinal(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return 1;
        }
        return size.Trim().ToUpperInvariant() switch
        {
            "SMALL" => 0,
            "MEDIUM" => 1,
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
