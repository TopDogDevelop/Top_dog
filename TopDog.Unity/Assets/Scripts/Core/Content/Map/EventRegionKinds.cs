namespace TopDog.Content.Map;

public static class EventRegionKinds
{
    public const string Star = "star";
    public const string Planet = "planet";
    public const string OreBelt = "oreBelt";
    public const string PirateRally = "pirateRally";
    public const string LegionStructure = "legionStructure";
    public const string JumpBridge = "jumpBridge";
    public const string DeployedStructure = "deployedStructure";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        Star, Planet, OreBelt, PirateRally, LegionStructure, JumpBridge, DeployedStructure,
    };

    public static bool IsStar(string? kind) => Star.Equals(kind, StringComparison.Ordinal);
}
