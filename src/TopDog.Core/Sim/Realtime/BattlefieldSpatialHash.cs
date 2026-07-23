/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §2 · §4
 * 本文件: BattlefieldSpatialHash.cs — 场景空间哈希邻域索引
 * 【机制要点】
 * · cellSize 取格宽；QueryNeighbors 只扫邻桶
 * · AOE / 选敌共用；每 tick 可 Rebuild
 * 【关联】AoeDamageService · AutoFireTargetingService
 * ══
 */

namespace TopDog.Sim.Realtime;

public sealed class BattlefieldSpatialHash
{
    private readonly Dictionary<(int, int, int), List<BattlefieldUnit>> _cells = new();
    public float CellSize { get; private set; } = 10_000f;

    public void Clear() => _cells.Clear();

    public void Rebuild(IReadOnlyList<BattlefieldUnit> units, float cellSize)
    {
        CellSize = Math.Max(1000f, cellSize);
        _cells.Clear();
        foreach (var u in units)
        {
            if (u == null || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            var key = CellOf(u.x, u.y, u.z);
            if (!_cells.TryGetValue(key, out var bucket))
            {
                bucket = new List<BattlefieldUnit>(8);
                _cells[key] = bucket;
            }

            bucket.Add(u);
        }
    }

    public (int, int, int) CellOf(float x, float y, float z)
    {
        var s = CellSize;
        return ((int)MathF.Floor(x / s), (int)MathF.Floor(y / s), (int)MathF.Floor(z / s));
    }

    /// <summary>枚举与世界坐标 AABB 相交的桶；结果按 unitId 确定性排序。</summary>
    public IEnumerable<BattlefieldUnit> QueryBounds(
        float minX, float minY, float minZ,
        float maxX, float maxY, float maxZ,
        int maxExplore)
    {
        if (maxExplore <= 0)
        {
            yield break;
        }

        var min = CellOf(Math.Min(minX, maxX), Math.Min(minY, maxY), Math.Min(minZ, maxZ));
        var max = CellOf(Math.Max(minX, maxX), Math.Max(minY, maxY), Math.Max(minZ, maxZ));
        var buffer = new List<BattlefieldUnit>();
        for (var x = min.Item1; x <= max.Item1; x++)
        {
            for (var y = min.Item2; y <= max.Item2; y++)
            {
                for (var z = min.Item3; z <= max.Item3; z++)
                {
                    if (_cells.TryGetValue((x, y, z), out var bucket))
                    {
                        buffer.AddRange(bucket);
                    }
                }
            }
        }

        buffer.Sort((a, b) => string.CompareOrdinal(a.unitId, b.unitId));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var emitted = 0;
        foreach (var unit in buffer)
        {
            if (unit.unitId == null || !seen.Add(unit.unitId))
            {
                continue;
            }

            yield return unit;
            emitted++;
            if (emitted >= maxExplore)
            {
                yield break;
            }
        }
    }

    /// <summary>按切比雪夫环数由近及远产出候选（稳定：同格 unitId 序）。</summary>
    public IEnumerable<BattlefieldUnit> QueryExpanding(float ox, float oy, float oz, int maxExplore)
    {
        if (maxExplore <= 0)
        {
            yield break;
        }

        var origin = CellOf(ox, oy, oz);
        var emitted = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var ring = 0; emitted < maxExplore; ring++)
        {
            var anyCell = false;
            var buffer = new List<BattlefieldUnit>();
            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    for (var dz = -ring; dz <= ring; dz++)
                    {
                        if (ring > 0 && Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz)) != ring)
                        {
                            continue;
                        }

                        anyCell = true;
                        var key = (origin.Item1 + dx, origin.Item2 + dy, origin.Item3 + dz);
                        if (!_cells.TryGetValue(key, out var bucket))
                        {
                            continue;
                        }

                        buffer.AddRange(bucket);
                    }
                }
            }

            if (!anyCell && ring > 0)
            {
                yield break;
            }

            buffer.Sort((a, b) => string.CompareOrdinal(a.unitId, b.unitId));
            foreach (var u in buffer)
            {
                if (u.unitId == null || !seen.Add(u.unitId))
                {
                    continue;
                }

                yield return u;
                emitted++;
                if (emitted >= maxExplore)
                {
                    yield break;
                }
            }

            // Empty space: stop after large empty rings
            if (ring > 64)
            {
                yield break;
            }
        }
    }
}
