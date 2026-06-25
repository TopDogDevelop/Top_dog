using System.Collections.Generic;
using TopDog.Content.Map;
using UnityEngine;

namespace TopDog.Client.StarMap;

internal static class StarMapMath
{
    private static readonly Color HighSec = new(0.3f, 0.55f, 0.95f, 1f);
    private static readonly Color LowSec = new(0.85f, 0.55f, 0.2f, 1f);
    private static readonly Color NullSec = new(0.82f, 0.28f, 0.28f, 1f);
    private static readonly Color PlayerTint = new(0.35f, 0.9f, 0.55f, 1f);
    private static readonly Color DispatchTint = new(0.95f, 0.88f, 0.35f, 1f);
    private static readonly Color HighlightTint = new(0.55f, 0.95f, 0.65f, 1f);

    public const float IconRadiusPx = 9f;
    public const float PickRadiusPx = 16f;
    /// <summary>Legacy constant; star map no longer separates overlapping markers.</summary>
    public const float MinMarkerScreenGapPx = 88f;
    public const float LyToWorldScale = 3f;

    public static Vector3 LyToWorld(float[]? ly)
    {
        if (ly == null || ly.Length < 3)
        {
            return Vector3.zero;
        }
        return new Vector3(ly[0] * LyToWorldScale, ly[1] * LyToWorldScale, ly[2] * LyToWorldScale);
    }

    /// <summary>Strategic star-map world position from ly (full 3D; ly[1] is height axis).</summary>
    public static Vector3 LyToWorldStrategic(float[]? ly) => LyToWorld(ly);

    private static readonly Color FriendlyAnchoredTint = new(80f / 255f, 160f / 255f, 255f / 255f, 1f);
    private static readonly Color FriendlyUnanchoredTint = new(1f, 1f, 1f, 1f);
    private static readonly Color EnemyFortTint = new(220f / 255f, 70f / 255f, 70f / 255f, 1f);

    public static Color SecurityColor(float security, SecurityBands? bands = null)
    {
        if (bands != null)
        {
            var hex = bands.ColorForSecurity(security);
            if (ColorUtility.TryParseHtmlString(hex, out var parsed))
            {
                return parsed;
            }

            TopDog.App.Brick.BrickDebugLog.Log("starmap.color", "band parse fallback security=" + security + " hex=" + hex);
        }

        if (security >= 0.5f)
        {
            return HighSec;
        }
        if (security > 0f)
        {
            return LowSec;
        }
        return NullSec;
    }

    public static Color SystemIconColor(
        SolarSystemDef system,
        StarMapSystemBadge? badge,
        string? dispatchTargetId,
        string? highlightedId,
        SecurityBands? securityBands = null)
    {
        if (system.solarSystemId != null && system.solarSystemId == dispatchTargetId)
        {
            return DispatchTint;
        }
        if (system.solarSystemId != null && system.solarSystemId == highlightedId)
        {
            return HighlightTint;
        }

        if (badge != null)
        {
            switch (badge.fortSovereignty)
            {
                case FortSovereignty.Enemy:
                    return EnemyFortTint;
                case FortSovereignty.FriendlyAnchored:
                    return FriendlyAnchoredTint;
                case FortSovereignty.FriendlyUnanchored:
                    return FriendlyUnanchoredTint;
            }
        }

        var c = SecurityColor(system.securityLevel, securityBands);
        if (badge is { playerPresence: true })
        {
            c = Color.Lerp(c, PlayerTint, 0.35f);
        }
        else if (badge is { hostilePresence: true })
        {
            c = Color.Lerp(c, EnemyFortTint, 0.22f);
        }
        return c;
    }

    public static string BridgeKey(string from, string to)
    {
        return string.CompareOrdinal(from, to) <= 0 ? $"{from}|{to}" : $"{to}|{from}";
    }

    /// <summary>Mean of strategic world positions (3D pivot for orbit).</summary>
    public static Vector3 ComputeMapViewCenter(IReadOnlyList<SolarSystemDef> systems)
    {
        if (systems == null || systems.Count == 0)
        {
            return Vector3.zero;
        }

        var sum = Vector3.zero;
        var count = 0;
        foreach (var sys in systems)
        {
            var ly = sys.starMapPositionLy;
            if (ly == null || ly.Length < 3)
            {
                continue;
            }
            sum += LyToWorldStrategic(ly);
            count++;
        }

        if (count == 0)
        {
            return Vector3.zero;
        }

        return sum / count;
    }

    public static void ComputeStrategicExtents(
        IReadOnlyList<SolarSystemDef> systems,
        out Vector3 center,
        out float halfSpanX,
        out float halfSpanZ,
        out float halfSpanY,
        out float minPairSepXz)
    {
        center = ComputeMapViewCenter(systems);
        halfSpanX = 0f;
        halfSpanZ = 0f;
        halfSpanY = 0f;
        minPairSepXz = float.MaxValue;
        if (systems == null || systems.Count == 0)
        {
            minPairSepXz = 0f;
            return;
        }

        var xz = new List<Vector2>();
        foreach (var sys in systems)
        {
            var w = LyToWorldStrategic(sys.starMapPositionLy);
            halfSpanX = Mathf.Max(halfSpanX, Mathf.Abs(w.x - center.x));
            halfSpanY = Mathf.Max(halfSpanY, Mathf.Abs(w.y - center.y));
            halfSpanZ = Mathf.Max(halfSpanZ, Mathf.Abs(w.z - center.z));
            xz.Add(new Vector2(w.x, w.z));
        }

        for (var i = 0; i < xz.Count; i++)
        {
            for (var j = i + 1; j < xz.Count; j++)
            {
                minPairSepXz = Mathf.Min(minPairSepXz, Vector2.Distance(xz[i], xz[j]));
            }
        }

        if (float.IsPositiveInfinity(minPairSepXz))
        {
            minPairSepXz = 0f;
        }
    }
}
