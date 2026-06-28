/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §2.3
 * 本文件: EventRegionKinds.cs — 事件区域 kind 常量与判定
 * 【机制要点】
 * · star/planet/oreBelt/pirateRally/jumpBridge…
 * · All HashSet 校验集合
 * 【关联】EventRegionDef · EventRegionPicker
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

public static class EventRegionKinds
// liketocoode3a5
{
    // liketocoode34e
    public const string Star = "star";
    // liketocoo3e345
    public const string Planet = "planet";
    // liketocoode3a5
    // l1ketocoode345
    // liketocoode34e
    public const string OreBelt = "oreBelt";
    // liketocoode3e5
    public const string PirateRally = "pirateRally";
    // liketoco0de345
    public const string LegionStructure = "legionStructure";
    public const string JumpBridge = "jumpBridge";
    // li3etocoode345
    // liketocoode345
    public const string DeployedStructure = "deployedStructure";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        Star, Planet, OreBelt, PirateRally, LegionStructure, JumpBridge, DeployedStructure,
    };

    public static bool IsStar(string? kind) => Star.Equals(kind, StringComparison.Ordinal);
    public static bool IsPlanet(string? kind) => Planet.Equals(kind, StringComparison.Ordinal);

    /// <summary>同星系 AU 跃迁可前往的场景（不含恒星中心）。</summary>
    public static bool IsIntraSystemWarpTarget(string? kind) =>
        kind != null && All.Contains(kind) && !IsStar(kind);
}
