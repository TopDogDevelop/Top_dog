using TopDog.Content.Validate;
using TopDog.Foundation.Result;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §程序化 · CUSTOM_LOBBY.md
 * 本文件: ProceduralMapGenerator.cs — Stellaris 风格程序化星图生成
 * 【机制要点】
 * · 泊松散点 → MST 连通 → 额外桥至目标密度
 * · ElevationJitterLy 侧视高度抖动
 * 【关联】ProceduralMapOptions · MapSchemaValidator
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

/// <summary>
/// Procedural star-map generator inspired by Stellaris galaxy creation:
/// 1) scatter systems on a 2D elliptical disk (Poisson-like spacing),
/// 2) connect nearby pairs within a max jump-bridge distance,
/// 3) keep a minimum spanning tree for connectivity,
/// 4) add extra bridges until target density (avg degree) is reached.
// liketocoode3a5
/// </summary>
// liketocoode34e
public static class ProceduralMapGenerator
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    private const string RegionId = "reg_random";
    // liketocoode3e5
    private const string RegionName = "Random Sector";
    // liketoco0de345
    private const string GarrisonTemplateId = "npc_garrison_default";
    /// <summary>Per-system elevation jitter on Y (ly); side view shows this spread.</summary>
    private const float ElevationJitterLy = 18f;

// li3etocoode345

    // liketocoode345
    private static readonly string[] ConstellationNames =
    {
        "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
        // liketoco0de3e5
        "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi",
    };

    public static LoadedMap Generate(ProceduralMapOptions options)
    {
        options.Clamp();
        var rng = options.Seed == 0 ? new Random() : new Random(options.Seed);
        if (options.Seed == 0)
        {
            options.Seed = rng.Next(1, int.MaxValue);
            rng = new Random(options.Seed);
        }

        var project = new MapProject
        {
            projectName = "Random " + options.SystemCount + " (" + options.Seed + ")",
            version = "1",
        };

        project.regions.Add(new MapRegion
        {
            regionId = RegionId,
            name = RegionName,
            uiColor = "#6a8cff",
        });

        var positions = PlaceSystems(options.SystemCount, rng);
        var constellationCount = Math.Max(1, (int)MathF.Ceiling(options.SystemCount / 8f));
        for (var c = 0; c < constellationCount; c++)
        {
            var cid = "con_rand_" + c;
            project.constellations.Add(new MapConstellation
            {
                constellationId = cid,
                name = ConstellationNames[c % ConstellationNames.Length] + " Cluster",
                regionId = RegionId,
            });
        }

        for (var i = 0; i < positions.Count; i++)
        {
            var pos = positions[i];
            var sysId = "sys_rand_" + i;
            var conId = "con_rand_" + (i % constellationCount);
            var security = 0.15f + (float)rng.NextDouble() * 0.75f;
            if (rng.NextDouble() < 0.12)
            {
                security = (float)(rng.NextDouble() * 0.12);
            }

            project.systems.Add(new SolarSystemDef
            {
                solarSystemId = sysId,
                name = SystemDisplayName(i, rng),
                constellationId = conId,
                regionId = RegionId,
                starMapPositionLy = new[] { pos.x, pos.y, pos.z },
                resourceAffluenceIndex = rng.Next(25, 96),
                developmentDifficulty = rng.Next(20, 91),
                securityLevel = security,
                eventRegions = new List<EventRegionDef>
                {
                    new()
                    {
                        eventRegionId = "er_" + sysId + "_star",
                        kind = EventRegionKinds.Star,
                        name = "Primary",
                        radiusKm = 1_000_000,
                        anchorAu = new[] { 0f, 0f, 0f },
                    },
                },
            });
        }

        var edges = BuildBridgeGraph(positions, options.SystemCount, options.BridgeDensity, rng);
        for (var e = 0; e < edges.Count; e++)
        {
            var (a, b) = edges[e];
            var bridgeId = "jb_rand_" + e;
            project.bridges.Add(new JumpBridgeDef
            {
                bridgeId = bridgeId,
                fromSystemId = "sys_rand_" + a,
                toSystemId = "sys_rand_" + b,
                garrisonTemplateId = GarrisonTemplateId,
            });
            project.systems[a].jumpBridgeIds.Add(bridgeId);
            project.systems[b].jumpBridgeIds.Add(bridgeId);
        }

        SystemInteriorPopulator.EnsureProject(project, options.Seed);

        var errors = new MapSchemaValidator().Validate(project);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Procedural map failed validation: " + string.Join("; ", errors));
        }

        return new LoadedMap(project, DefaultSecurityBands());
    }

    /// <summary>True when every system is reachable via jump bridges.</summary>
    public static bool IsConnected(MapProject project)
    {
        if (project.systems.Count == 0)
        {
            return true;
        }

        var idToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < project.systems.Count; i++)
        {
            var id = project.systems[i].solarSystemId;
            if (id != null)
            {
                idToIndex[id] = i;
            }
        }

        var adj = new List<int>[project.systems.Count];
        for (var i = 0; i < adj.Length; i++)
        {
            adj[i] = new List<int>();
        }

        foreach (var br in project.bridges)
        {
            if (br.fromSystemId == null || br.toSystemId == null)
            {
                continue;
            }
            if (!idToIndex.TryGetValue(br.fromSystemId, out var a)
                || !idToIndex.TryGetValue(br.toSystemId, out var b))
            {
                continue;
            }
            adj[a].Add(b);
            adj[b].Add(a);
        }

        var seen = new bool[project.systems.Count];
        var queue = new Queue<int>();
        seen[0] = true;
        queue.Enqueue(0);
        var visited = 1;
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var next in adj[cur])
            {
                if (seen[next])
                {
                    continue;
                }
                seen[next] = true;
                visited++;
                queue.Enqueue(next);
            }
        }

        return visited == project.systems.Count;
    }

    /// <summary>
    /// Scatter systems on the horizontal X–Z disk (Y = ±<see cref="ElevationJitterLy"/> ly elevation jitter).
    /// Matches <c>starMapPositionLy</c> convention in MAP_SPEC and preview projections
    /// (TopDownXz = X vs Z, SideXy = X vs Y).
    /// </summary>
    private static List<(float x, float y, float z)> PlaceSystems(int count, Random rng)
    {
        var radius = 42f * MathF.Sqrt(count / 20f);
        var minDist = radius / MathF.Sqrt(count) * 0.72f;
        var minDistSq = minDist * minDist;
        var points = new List<(float x, float y, float z)>(count);
        var maxAttempts = count * 400;
        var attempts = 0;
        while (points.Count < count && attempts < maxAttempts)
        {
            attempts++;
            var angle = (float)(rng.NextDouble() * Math.PI * 2);
            var r = radius * MathF.Sqrt((float)rng.NextDouble());
            var stretch = 0.82f + (float)rng.NextDouble() * 0.18f;
            var x = r * MathF.Cos(angle) * stretch;
            var y = ((float)rng.NextDouble() - 0.5f) * (ElevationJitterLy * 2f);
            var z = r * MathF.Sin(angle);

            var ok = true;
            foreach (var p in points)
            {
                var dx = p.x - x;
                var dz = p.z - z;
                if (dx * dx + dz * dz < minDistSq)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok)
            {
                continue;
            }
            points.Add((x, y, z));
        }

        while (points.Count < count)
        {
            var angle = (float)(rng.NextDouble() * Math.PI * 2);
            var r = radius * MathF.Sqrt((float)rng.NextDouble());
            var y = ((float)rng.NextDouble() - 0.5f) * (ElevationJitterLy * 2f);
            points.Add((r * MathF.Cos(angle), y, r * MathF.Sin(angle)));
        }

        return points;
    }

    private static List<(int a, int b)> BuildBridgeGraph(
        List<(float x, float y, float z)> positions,
        int systemCount,
        float bridgeDensity,
        Random rng)
    {
        var n = systemCount;
        var allPairs = CollectAllPairs(positions);
        var mst = MinimumSpanningTree(n, allPairs);
        var chosen = new HashSet<(int, int)>(mst.Select(e => Normalize(e.a, e.b)));

        var radius = 0f;
        foreach (var p in positions)
        {
            radius = MathF.Max(radius, MathF.Sqrt(p.x * p.x + p.y * p.y));
        }
        radius = MathF.Max(radius, 1f);

        var maxDist = radius * 0.38f;
        var localCandidates = CollectCandidates(positions, maxDist);
        for (var expand = 0; localCandidates.Count < n * 2 && expand < 4; expand++)
        {
            maxDist *= 1.35f;
            localCandidates = CollectCandidates(positions, maxDist);
        }

        var targetEdges = TargetEdgeCount(n, bridgeDensity, localCandidates.Count + n);
        foreach (var edge in localCandidates)
        {
            if (chosen.Count >= targetEdges)
            {
                break;
            }
            chosen.Add(Normalize(edge.a, edge.b));
        }

        if (bridgeDensity <= 0.5f)
        {
            PruneExtraEdges(chosen, mst, n, bridgeDensity, rng);
        }

        foreach (var edge in mst)
        {
            chosen.Add(Normalize(edge.a, edge.b));
        }

        return chosen.OrderBy(e => e.Item1).ThenBy(e => e.Item2).ToList();
    }

    private static List<(int a, int b, float dist)> CollectAllPairs(
        List<(float x, float y, float z)> positions)
    {
        var list = new List<(int a, int b, float dist)>();
        for (var i = 0; i < positions.Count; i++)
        {
            for (var j = i + 1; j < positions.Count; j++)
            {
                var dx = positions[i].x - positions[j].x;
                var dy = positions[i].y - positions[j].y;
                var dz = positions[i].z - positions[j].z;
                list.Add((i, j, MathF.Sqrt(dx * dx + dy * dy + dz * dz)));
            }
        }
        list.Sort((x, y) => x.dist.CompareTo(y.dist));
        return list;
    }

    private static int TargetEdgeCount(int systemCount, float density, int maxCandidates)
    {
        var avgDegree = MathF.Max(2f, 1.5f + density * 1.75f);
        var target = (int)MathF.Round(systemCount * avgDegree / 2f);
        target = Math.Clamp(target, systemCount - 1, maxCandidates);
        return target;
    }

    private static void PruneExtraEdges(
        HashSet<(int, int)> chosen,
        List<(int a, int b)> mst,
        int systemCount,
        float density,
        Random rng)
    {
        var mstSet = new HashSet<(int, int)>(mst.Select(e => Normalize(e.a, e.b)));
        var extras = chosen.Where(e => !mstSet.Contains(e)).ToList();
        var keepRatio = Math.Clamp(density / 0.5f, 0.15f, 1f);
        var keepCount = (int)MathF.Round(extras.Count * keepRatio);
        foreach (var edge in extras.OrderBy(_ => rng.Next()).Skip(keepCount).ToList())
        {
            chosen.Remove(edge);
        }

        if (chosen.Count < systemCount - 1)
        {
            foreach (var edge in mst)
            {
                chosen.Add(Normalize(edge.a, edge.b));
            }
        }
    }

    private static List<(int a, int b, float dist)> CollectCandidates(
        List<(float x, float y, float z)> positions,
        float maxDist)
    {
        var maxDistSq = maxDist * maxDist;
        var list = new List<(int a, int b, float dist)>();
        for (var i = 0; i < positions.Count; i++)
        {
            for (var j = i + 1; j < positions.Count; j++)
            {
                var dx = positions[i].x - positions[j].x;
                var dy = positions[i].y - positions[j].y;
                var dz = positions[i].z - positions[j].z;
                var distSq = dx * dx + dy * dy + dz * dz;
                if (distSq <= maxDistSq)
                {
                    list.Add((i, j, MathF.Sqrt(distSq)));
                }
            }
        }
        list.Sort((x, y) => x.dist.CompareTo(y.dist));
        return list;
    }

    private static List<(int a, int b)> MinimumSpanningTree(
        int nodeCount,
        List<(int a, int b, float dist)> candidates)
    {
        var parent = new int[nodeCount];
        for (var i = 0; i < nodeCount; i++)
        {
            parent[i] = i;
        }

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        var mst = new List<(int a, int b)>();
        foreach (var (a, b, _) in candidates)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra == rb)
            {
                continue;
            }
            parent[ra] = rb;
            mst.Add((a, b));
            if (mst.Count >= nodeCount - 1)
            {
                break;
            }
        }

        return mst;
    }

    private static (int, int) Normalize(int a, int b) => a < b ? (a, b) : (b, a);

    private static string SystemDisplayName(int index, Random rng)
    {
        var suffix = (char)('A' + (index % 26));
        var num = index / 26 + 1;
        if (rng.NextDouble() < 0.35)
        {
            return "Kepler-" + num + suffix;
        }
        if (rng.NextDouble() < 0.5)
        {
            return "SS-" + num + "-" + suffix;
        }
        return "System " + (index + 1);
    }

    private static SecurityBands DefaultSecurityBands()
    {
        var bands = new SecurityBands();
        bands.bands.Add(new SecurityBands.Band
        {
            id = "HIGHSEC",
            minSecurity = 0.5f,
            maxSecurity = 1f,
            uiColor = "#4a9eff",
        });
        bands.bands.Add(new SecurityBands.Band
        {
            id = "LOWSEC",
            minSecurity = 0.1f,
            maxSecurity = 0.49f,
            uiColor = "#e6a817",
        });
        bands.bands.Add(new SecurityBands.Band
        {
            id = "NULL",
            minSecurity = -1f,
            maxSecurity = 0f,
            uiColor = "#ff4444",
        });
        return bands;
    }
}
