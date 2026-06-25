namespace TopDog.Content.Map;

/// <summary>
/// Stellaris-inspired procedural star sector parameters.
/// <see cref="BridgeDensity"/> mirrors Stellaris <c>num_hyperlanes</c> (0.25 sparse … 3 dense).
/// </summary>
public sealed class ProceduralMapOptions
{
    public const int MinSystemCount = 8;
    public const int MaxSystemCount = 80;
    public const float MinBridgeDensity = 0.25f;
    public const float MaxBridgeDensity = 3f;

    public const float BridgeDensityStep = 0.05f;

    public int SystemCount { get; set; } = 20;
    public float BridgeDensity { get; set; } = 1f;
    public int Seed { get; set; }

    public static float RoundBridgeDensity(float value)
    {
        var clamped = Math.Clamp(value, MinBridgeDensity, MaxBridgeDensity);
        return MathF.Round(clamped / BridgeDensityStep) * BridgeDensityStep;
    }

    public void Clamp()
    {
        SystemCount = Math.Clamp(SystemCount, MinSystemCount, MaxSystemCount);
        BridgeDensity = RoundBridgeDensity(BridgeDensity);
    }
}
