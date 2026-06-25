using TopDog.Foundation.Result;

namespace TopDog.Content.Map;

/// <summary>
/// Ensures each solar system has interior locations for operations / combat prep.
/// See MAP_SPEC §2.4 and OPERATIONS_UI §星系内部.
/// </summary>
public static class SystemInteriorPopulator
{
    public const float MaxRandomAuFromPlanet = 3f;
    public const int RandomPirateRallyCount = 5;
    public const int RandomOreBeltCount = 5;
    public const float JumpGateAu = 10f;

    public static void EnsureProject(MapProject project, int seed = 0)
    {
        if (project.systems.Count == 0)
        {
            return;
        }
        var baseSeed = seed == 0 ? project.projectName?.GetHashCode() ?? 1 : seed;
        for (var i = 0; i < project.systems.Count; i++)
        {
            var sys = project.systems[i];
            var sysSeed = baseSeed ^ (sys.solarSystemId?.GetHashCode() ?? i);
            EnsureSystem(sys, project, sysSeed);
        }
    }

    public static void EnsureSystem(SolarSystemDef system, MapProject project, int seed)
    {
        if (system.solarSystemId == null)
        {
            return;
        }
        var rng = new Random(seed == 0 ? 1 : seed);
        EnsureStar(system);
        var planet = EnsurePrimaryPlanet(system, rng);
        EnsureJumpBridgeSites(system, project, rng);
        EnsureRandomSites(
            system,
            planet,
            rng,
            EventRegionKinds.PirateRally,
            "海盗集结",
            RandomPirateRallyCount);
        EnsureRandomSites(
            system,
            planet,
            rng,
            EventRegionKinds.OreBelt,
            "矿带",
            RandomOreBeltCount);
    }

    private static void EnsureStar(SolarSystemDef system)
    {
        foreach (var er in system.eventRegions)
        {
            if (EventRegionKinds.IsStar(er.kind))
            {
                return;
            }
        }
        var id = system.solarSystemId!;
        system.eventRegions.Insert(0, new EventRegionDef
        {
            eventRegionId = $"er_{id}_star",
            kind = EventRegionKinds.Star,
            name = "恒星",
            radiusKm = 1_000_000,
            anchorAu = new[] { 0f, 0f, 0f },
        });
    }

    private static EventRegionDef EnsurePrimaryPlanet(SolarSystemDef system, Random rng)
    {
        foreach (var er in system.eventRegions)
        {
            if (EventRegionKinds.Planet.Equals(er.kind, StringComparison.Ordinal))
            {
                return er;
            }
        }
        var id = system.solarSystemId!;
        var angle = (float)(rng.NextDouble() * Math.PI * 2);
        var dist = 0.9f + (float)rng.NextDouble() * 0.8f;
        var planet = new EventRegionDef
        {
            eventRegionId = $"er_{id}_planet_1",
            kind = EventRegionKinds.Planet,
            name = "行星-1",
            radiusKm = 1_000_000,
            anchorAu = new[] { dist * MathF.Cos(angle), 0f, dist * MathF.Sin(angle) },
        };
        system.eventRegions.Add(planet);
        return planet;
    }

    private static void EnsureJumpBridgeSites(SolarSystemDef system, MapProject project, Random rng)
    {
        var sysId = system.solarSystemId!;
        var existing = new HashSet<string>(StringComparer.Ordinal);
        foreach (var er in system.eventRegions)
        {
            if (EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal)
                && er.bridgeId != null)
            {
                existing.Add(er.bridgeId);
            }
        }

        var gateIndex = existing.Count;
        foreach (var jb in project.bridges)
        {
            if (jb.bridgeId == null)
            {
                continue;
            }
            if (!sysId.Equals(jb.fromSystemId, StringComparison.Ordinal)
                && !sysId.Equals(jb.toSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            if (existing.Contains(jb.bridgeId))
            {
                continue;
            }
            gateIndex++;
            var angle = (float)(rng.NextDouble() * Math.PI * 2);
            system.eventRegions.Add(new EventRegionDef
            {
                eventRegionId = $"er_{sysId}_gate_{gateIndex}",
                kind = EventRegionKinds.JumpBridge,
                name = "跳桥位",
                radiusKm = 1_000_000,
                bridgeId = jb.bridgeId,
                targetSystemId = sysId.Equals(jb.fromSystemId, StringComparison.Ordinal)
                    ? jb.toSystemId
                    : jb.fromSystemId,
                anchorAu = GateAnchorAu(angle),
            });
            existing.Add(jb.bridgeId);
        }

        if (gateIndex == 0 && system.jumpBridgeIds.Count > 0)
        {
            foreach (var bridgeId in system.jumpBridgeIds)
            {
                if (bridgeId == null || existing.Contains(bridgeId))
                {
                    continue;
                }
                gateIndex++;
                var angle = (float)(rng.NextDouble() * Math.PI * 2);
                system.eventRegions.Add(new EventRegionDef
                {
                    eventRegionId = $"er_{sysId}_gate_{gateIndex}",
                    kind = EventRegionKinds.JumpBridge,
                    name = "跳桥位",
                    radiusKm = 1_000_000,
                    bridgeId = bridgeId,
                    anchorAu = GateAnchorAu(angle),
                });
                existing.Add(bridgeId);
            }
        }
    }

    private static float[] GateAnchorAu(float angle) =>
        new[] { JumpGateAu * MathF.Cos(angle), 0f, JumpGateAu * MathF.Sin(angle) };

    private static void EnsureRandomSites(
        SolarSystemDef system,
        EventRegionDef planet,
        Random rng,
        string kind,
        string namePrefix,
        int targetCount)
    {
        var sysId = system.solarSystemId!;
        var prefix = kind switch
        {
            EventRegionKinds.OreBelt => "belt",
            EventRegionKinds.PirateRally => "pirate",
            _ => "site",
        };
        var existing = 0;
        foreach (var er in system.eventRegions)
        {
            if (kind.Equals(er.kind, StringComparison.Ordinal))
            {
                existing++;
            }
        }
        var planetAnchor = planet.anchorAu;
        for (var n = existing + 1; n <= targetCount; n++)
        {
            var anchor = RandomAnchorNearPlanet(planetAnchor, rng);
            system.eventRegions.Add(new EventRegionDef
            {
                eventRegionId = $"er_{sysId}_{prefix}_{n}",
                kind = kind,
                name = $"{namePrefix}-{n}",
                radiusKm = 800_000,
                anchorAu = anchor,
            });
        }
    }

    /// <summary>Random point within <see cref="MaxRandomAuFromPlanet"/> AU of planet (incl. buildings).</summary>
    public static float[] RandomAnchorNearPlanet(float[]? planetAnchorAu, Random rng)
    {
        var px = planetAnchorAu is { Length: >= 3 } ? planetAnchorAu[0] : 1f;
        var py = planetAnchorAu is { Length: >= 3 } ? planetAnchorAu[1] : 0f;
        var pz = planetAnchorAu is { Length: >= 3 } ? planetAnchorAu[2] : 0f;
        var r = (float)rng.NextDouble() * MaxRandomAuFromPlanet;
        var theta = (float)(rng.NextDouble() * Math.PI * 2);
        var phi = (float)(rng.NextDouble() * Math.PI);
        var dx = r * MathF.Sin(phi) * MathF.Cos(theta);
        var dy = r * MathF.Cos(phi) * 0.35f;
        var dz = r * MathF.Sin(phi) * MathF.Sin(theta);
        return new[] { px + dx, py + dy, pz + dz };
    }

    public static bool IsWithinPlanetShell(float[] anchorAu, float[] planetAnchorAu)
    {
        if (anchorAu.Length < 3 || planetAnchorAu.Length < 3)
        {
            return false;
        }
        var dx = anchorAu[0] - planetAnchorAu[0];
        var dy = anchorAu[1] - planetAnchorAu[1];
        var dz = anchorAu[2] - planetAnchorAu[2];
        var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        return dist <= MaxRandomAuFromPlanet + 0.001f;
    }
}
