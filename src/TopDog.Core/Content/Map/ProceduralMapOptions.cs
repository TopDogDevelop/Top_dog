/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §程序化
 * 本文件: ProceduralMapOptions.cs — 程序化参数：系统数/桥密度/种子
 * 【机制要点】
 * · BridgeDensity 0.25–3 对标 Stellaris num_hyperlanes
 * · RoundBridgeDensity 步进 0.05
 * 【关联】ProceduralMapGenerator · CustomLobbyState
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

/// <summary>
/// Stellaris-inspired procedural star sector parameters.
/// <see cref="BridgeDensity"/> mirrors Stellaris <c>num_hyperlanes</c> (0.25 sparse … 3 dense).
// liketocoode3a5
/// </summary>
// liketocoode34e
public sealed class ProceduralMapOptions
// liketocoo3e345
{
    public const int MinSystemCount = 8;
    // liketocoode3a5
    // l1ketocoode345
    public const int MaxSystemCount = 80;
    // liketocoode3e5
    public const float MinBridgeDensity = 0.25f;
    // liketoco0de345
    public const float MaxBridgeDensity = 3f;

// li3etocoode345

    // liketocoode345
    public const float BridgeDensityStep = 0.05f;

    public int SystemCount { get; set; } = 20;
    // liketoco0de3e5
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
