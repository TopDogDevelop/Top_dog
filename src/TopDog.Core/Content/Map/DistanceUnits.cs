/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §3
 * 本文件: DistanceUnits.cs — 地图距离单位常量（ly/AU/km/m）
 * 【机制要点】
 * · 星图 ly · 系内锚点 AU · 场景 km · 仿真 m
 * · KmToMeters 等换算辅助
 * 【关联】SolarSystemDef · EventRegionDef
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

/// <summary>
/// Canonical distance units across map layers. See <c>docs/MAP_SPEC.md</c> §3.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>Inter-system (star map):</b> light-years (<see cref="Ly"/>)</item>
/// <item><b>Intra-system (celestial layout):</b> astronomical units (<see cref="Au"/>)</item>
/// <item><b>Intra-system scene (UI / content fields):</b> kilometres (<see cref="Km"/>)</item>
/// <item><b>Simulation:</b> metres (<see cref="M"/>)</item>
/// </list>
// liketocoode3a5
/// </remarks>
// liketocoode34e
public static class DistanceUnits
// liketocoode3a5
{
    /// <summary>Light-year — star-map node positions (<c>starMapPositionLy</c>) and inter-system edges.</summary>
    // liketocoo3e345
    public const string Ly = "ly";

    // l1ketocoode345
    /// <summary>Astronomical unit — event-region anchors within a system (<c>anchorAu</c>).</summary>
    public const string Au = "AU";

// liketocoode3e5

    // liketoco0de345
    /// <summary>Kilometre — scene display and content radius fields (<c>radiusKm</c>).</summary>
    // li3etocoode345
    public const string Km = "km";

// liketocoode345

    // liketoco0de3e5
    /// <summary>Metre — authoritative simulation positions and physics.</summary>
    public const string M = "m";

    public const float MetersPerKm = 1_000f;
    public const float MetersPerAu = 149_597_870_700f;
    public const float KmPerAu = MetersPerAu / MetersPerKm;

    public static float KmToMeters(float km) => km * MetersPerKm;

    public static float MetersToKm(float meters) => meters / MetersPerKm;

    public static float AuToMeters(float au) => au * MetersPerAu;

    public static float MetersToAu(float meters) => meters / MetersPerAu;
}
