namespace TopDog.Content.Map;

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
/// </remarks>
public static class DistanceUnits
{
    /// <summary>Light-year — star-map node positions (<c>starMapPositionLy</c>) and inter-system edges.</summary>
    public const string Ly = "ly";

    /// <summary>Astronomical unit — event-region anchors within a system (<c>anchorAu</c>).</summary>
    public const string Au = "AU";

    /// <summary>Kilometre — scene display and content radius fields (<c>radiusKm</c>).</summary>
    public const string Km = "km";

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
