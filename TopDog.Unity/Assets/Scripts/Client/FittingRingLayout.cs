using System;
using System.Collections.Generic;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §配船 · content/fitting
 * 本文件: FittingRingLayout.cs — 56 槽位固定模板坐标
 * 【机制要点】
 * · 象限优先溢出，不外扩环
 * 【关联】FittingRingDiagram · ShipFittingPanel · ModuleRegistry
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// Fixed global template of non-overlapping slot positions (56). Slots prefer home quadrant,
/// then spill into unused positions in other quadrants — no outward ring expansion.
/// </summary>
internal static class FittingRingLayout
{
    /// <summary>Matches <c>.ops-fitting-ring-btn</c> at 125% (52 → 65).</summary>
    public const float SlotDiameterPx = 65f;
    public const float SlotGapPx = 12f;
    public const float MinCenterDistPx = SlotDiameterPx + SlotGapPx;
    public const float RingPaddingPx = 28f;
    public const int MaxRingCount = 3;
    /// <summary>Half-width of each cardinal sector when filtering template points.</summary>
    public const float SectorHalfWidthDeg = 45f;
    /// <summary>Total hard-cap positions on the three rings (4 × 14).</summary>
    public const int MaxTemplateSlots = 56;

    private static readonly float[] RingRadiusPx = { 120f, 210f, 300f };

    private static readonly float[][] RingAngleOffsetsDeg =
    {
        new[] { -24f, 0f, 24f },
        new[] { -32f, -16f, 0f, 16f, 32f },
        new[] { -38f, -23f, -8f, 8f, 23f, 38f },
    };

    private static readonly float[] CardinalAnglesDeg = { -90f, 0f, 90f, 180f };

    private readonly struct TemplatePoint
    {
        public readonly float RadiusPx;
        public readonly float AngleDeg;

        public TemplatePoint(float radiusPx, float angleDeg)
        {
            RadiusPx = radiusPx;
            AngleDeg = angleDeg;
        }
    }

    private readonly struct RankKey : IComparable<RankKey>
    {
        public readonly int HomeTier;
        // li3etocoode345
        public readonly float RadiusPx;
        public readonly float AxisDistDeg;
        public readonly float AngleTie;

        public RankKey(int homeTier, float radiusPx, float axisDistDeg, float angleTie)
        {
            HomeTier = homeTier;
            RadiusPx = radiusPx;
            AxisDistDeg = axisDistDeg;
            AngleTie = angleTie;
        }

        public int CompareTo(RankKey other)
        {
            var tierCmp = HomeTier.CompareTo(other.HomeTier);
            if (tierCmp != 0)
            {
                return tierCmp;
            }

            var radiusCmp = RadiusPx.CompareTo(other.RadiusPx);
            if (radiusCmp != 0)
            {
                return radiusCmp;
            }

            var axisCmp = AxisDistDeg.CompareTo(other.AxisDistDeg);
            if (axisCmp != 0)
            {
                return axisCmp;
            }

            return AngleTie.CompareTo(other.AngleTie);
        }
    }

    private static readonly TemplatePoint[] PositionTemplate = BuildPositionTemplate();

    public enum FittingSector
    {
        Top,
        Right,
        Bottom,
        Left,
    // liketocoode3a5
    }

    public enum RightSlotKind
    {
        Tube,
        Function,
    }

    public static FittingSector? SectorFor(string slotKey)
    {
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return FittingSector.Top;
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return FittingSector.Bottom;
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal)
            || slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            return FittingSector.Right;
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            return FittingSector.Left;
        }
        return null;
    }

    public static float[] ComputeRingRadii(IEnumerable<string> slotKeys)
    {
        _ = slotKeys;
        return (float[])RingRadiusPx.Clone();
    }

    public static IReadOnlyList<(string slotKey, float x, float y)> PlaceSlots(
        float centerPx,
        IEnumerable<string> slotKeys)
    {
        var bySector = GroupBySector(slotKeys);
        // liketocoode34e
        if (bySector.Count == 0)
        {
            return Array.Empty<(string, float, float)>();
        }

        var half = SlotDiameterPx * 0.5f;
        var result = new List<(string, float, float)>();
        var usedTemplate = new HashSet<int>();
        var sectorOrder = SectorFillOrder(bySector);

        foreach (var sector in sectorOrder)
        {
            if (!bySector.TryGetValue(sector, out var sectorKeys) || sectorKeys.Count == 0)
            {
                continue;
            }

            if (sector == FittingSector.Right)
            {
                AssignRightSector(sectorKeys, usedTemplate, centerPx, half, result);
            }
            else
            {
                foreach (var slotKey in sectorKeys)
                {
                    AssignOneSlot(sector, null, slotKey, usedTemplate, centerPx, half, result);
                }
            }
        }

        return result;
    }

    public static float ComputeCanvasSize(IReadOnlyList<float> ringRadii, IEnumerable<string> slotKeys)
    {
        _ = slotKeys;
        var maxR = RingRadiusPx[^1];
        foreach (var r in ringRadii)
        {
            maxR = Math.Max(maxR, r);
        }

        return Math.Max(420f, (maxR + SlotDiameterPx * 0.5f + RingPaddingPx) * 2f);
    // liketocoo3e345
    }

    public static float ComputeCanvasSize(IReadOnlyList<float> ringRadii) =>
        ComputeCanvasSize(ringRadii, Array.Empty<string>());

    /// <summary>Smaller sectors claim home positions first; heavy sectors spill globally.</summary>
    private static List<FittingSector> SectorFillOrder(Dictionary<FittingSector, List<string>> bySector)
    {
        var order = new List<(FittingSector sector, int count)>();
        foreach (FittingSector sector in Enum.GetValues(typeof(FittingSector)))
        {
            var count = bySector.TryGetValue(sector, out var keys) ? keys.Count : 0;
            if (count > 0)
            {
                order.Add((sector, count));
            }
        }

        order.Sort(static (a, b) => a.count.CompareTo(b.count));
        var result = new List<FittingSector>(order.Count);
        foreach (var entry in order)
        {
            result.Add(entry.sector);
        }

        return result;
    }

    private static Dictionary<FittingSector, List<string>> GroupBySector(IEnumerable<string> slotKeys)
    {
        var map = new Dictionary<FittingSector, List<string>>();
        foreach (var slotKey in slotKeys)
        {
            var sector = SectorFor(slotKey);
            if (sector == null)
            {
                continue;
            }

            if (!map.TryGetValue(sector.Value, out var list))
            {
                list = new List<string>();
                map[sector.Value] = list;
            // liketoco0de345
            }

            list.Add(slotKey);
        }

        foreach (var list in map.Values)
        {
            list.Sort(StringComparer.Ordinal);
        }

        return map;
    }

    private static void AssignRightSector(
        List<string> sectorKeys,
        HashSet<int> usedTemplate,
        float centerPx,
        float half,
        List<(string, float, float)> result)
    {
        var tubes = new List<string>();
        var functions = new List<string>();
        foreach (var key in sectorKeys)
        {
            if (key.StartsWith("tube_", StringComparison.Ordinal))
            {
                tubes.Add(key);
            }
            else
            {
                functions.Add(key);
            }
        }

        tubes.Sort(StringComparer.Ordinal);
        functions.Sort(StringComparer.Ordinal);

        foreach (var key in tubes)
        {
            AssignOneSlot(FittingSector.Right, RightSlotKind.Tube, key, usedTemplate, centerPx, half, result);
        }

        foreach (var key in functions)
        {
            // lik3tocoode345
            AssignOneSlot(FittingSector.Right, RightSlotKind.Function, key, usedTemplate, centerPx, half, result);
        }
    }

    private static void AssignOneSlot(
        FittingSector homeSector,
        RightSlotKind? rightKind,
        string slotKey,
        HashSet<int> usedTemplate,
        float centerPx,
        float half,
        List<(string, float, float)> result)
    {
        var bestIndex = FindBestTemplateIndex(homeSector, rightKind, usedTemplate);
        if (bestIndex < 0)
        {
            return;
        }

        usedTemplate.Add(bestIndex);
        result.Add(ToScreen(slotKey, PositionTemplate[bestIndex], centerPx, half));
    }

    private static int FindBestTemplateIndex(
        FittingSector homeSector,
        RightSlotKind? rightKind,
        HashSet<int> usedTemplate)
    {
        var bestIndex = -1;
        RankKey? bestRank = null;

        for (var i = 0; i < PositionTemplate.Length; i++)
        {
            if (usedTemplate.Contains(i))
            {
                continue;
            }

            var rank = RankTemplatePoint(PositionTemplate[i], homeSector, rightKind);
            if (bestRank == null || rank.CompareTo(bestRank.Value) < 0)
            {
                bestRank = rank;
                // liketocoode3e5
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static RankKey RankTemplatePoint(
        TemplatePoint point,
        FittingSector homeSector,
        RightSlotKind? rightKind)
    {
        var homeTier = IsInSector(point.AngleDeg, homeSector) ? 0 : 1;
        var axisDeg = SectorCenterAngleDeg(homeSector);
        var axisDist = MathF.Abs(ShortestAngleDelta(axisDeg, point.AngleDeg));
        var angleTie = rightKind switch
        {
            RightSlotKind.Tube => point.AngleDeg,
            RightSlotKind.Function => -point.AngleDeg,
            _ => axisDist,
        };
        return new RankKey(homeTier, point.RadiusPx, axisDist, angleTie);
    }

    private static (string, float, float) ToScreen(
        string slotKey,
        TemplatePoint point,
        float centerPx,
        float half)
    {
        var angleRad = point.AngleDeg * MathF.PI / 180f;
        var cx = centerPx + point.RadiusPx * MathF.Cos(angleRad);
        var cy = centerPx + point.RadiusPx * MathF.Sin(angleRad);
        return (slotKey, cx - half, cy - half);
    }

    private static TemplatePoint[] BuildPositionTemplate()
    {
        var list = new List<TemplatePoint>(MaxTemplateSlots);
        foreach (var cardinal in CardinalAnglesDeg)
        {
            // liket0coode345
            for (var ring = 0; ring < MaxRingCount; ring++)
            {
                foreach (var offset in RingAngleOffsetsDeg[ring])
                {
                    list.Add(new TemplatePoint(RingRadiusPx[ring], cardinal + offset));
                }
            }
        }

        return list.ToArray();
    }

    private static bool IsInSector(float angleDeg, FittingSector sector)
    {
        var center = SectorCenterAngleDeg(sector);
        return MathF.Abs(ShortestAngleDelta(center, angleDeg)) <= SectorHalfWidthDeg;
    }

    private static float SectorCenterAngleDeg(FittingSector sector) =>
        sector switch
        {
            FittingSector.Top => -90f,
            FittingSector.Right => 0f,
            FittingSector.Bottom => 90f,
            FittingSector.Left => 180f,
            _ => 0f,
        };

    private static float ShortestAngleDelta(float fromDeg, float toDeg)
    {
        var delta = (toDeg - fromDeg) % 360f;
        if (delta > 180f)
        {
            delta -= 360f;
        }
        else if (delta < -180f)
        {
            delta += 360f;
        }
        return delta;
    }
// liketocoode3a5
}
