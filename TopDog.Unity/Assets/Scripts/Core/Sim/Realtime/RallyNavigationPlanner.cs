using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.State;
using TopDog.Sim.Vision;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §8 · TACTICAL_WARP_AND_ORDERS.md §2.1.2
 * 本文件: RallyNavigationPlanner.cs — 跨场景集结路径规划
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class RallyNavigationPlanner
{
    public static RallyAnchor ResolveShipPositionAnchor(GameState state, BattlefieldState viewBf)
    {
        var possessor = BattlefieldSystem.FindPossessedUnit(state, viewBf);
        if (possessor != null)
        {
            return new RallyAnchor
            {
                Kind = RallyAnchorKind.ShipPosition,
                SystemId = FindUnitSystemId(state, possessor),
                EventRegionId = FindUnitEventRegionId(state, possessor),
                BattlefieldId = FindUnitBattlefieldId(state, possessor),
                X = possessor.x,
                Y = possessor.y,
                Z = possessor.z,
            };
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(state, viewBf);
        return new RallyAnchor
        {
            Kind = RallyAnchorKind.ShipPosition,
            SystemId = viewBf.systemId,
            EventRegionId = viewBf.eventRegionId,
            BattlefieldId = viewBf.battlefieldId,
            X = focus?.x ?? 0f,
            Y = focus?.y ?? 0f,
            Z = focus?.z ?? 0f,
        };
    }

    public static RallyAnchor ResolveSystemAnchor(string systemId) =>
        new()
        {
            Kind = RallyAnchorKind.SystemOnly,
            SystemId = systemId,
        };

    public static RallyAnchor ResolveSceneAnchor(
        GameState state,
        string systemId,
        string eventRegionId,
        float landingDistM)
    {
        var bf = TacticalSceneBattlefieldService.EnsureSceneBattlefield(state, systemId, eventRegionId);
        return new RallyAnchor
        {
            Kind = RallyAnchorKind.SceneLanding,
            SystemId = systemId,
            EventRegionId = eventRegionId,
            BattlefieldId = bf.battlefieldId,
            LandingDistM = landingDistM,
        };
    }

    public static List<string> PlanRoute(GameState state, BattlefieldUnit unit, RallyAnchor anchor)
    {
        var steps = new List<string>();
        if (anchor.SystemId == null)
        {
            return steps;
        }

        var unitBf = FindBattlefieldForUnit(state, unit);
        var unitSystemId = unitBf?.systemId;
        if (unitSystemId == null)
        {
            return steps;
        }

        if (!unitSystemId.Equals(anchor.SystemId, StringComparison.Ordinal))
        {
            var path = PlanBridgePath(state, unitSystemId, anchor.SystemId);
            for (var i = 0; i < path.Count - 1; i++)
            {
                var bridge = JumpBridgeResolver.FindBridge(
                    state.map?.Project,
                    path[i],
                    path[i + 1]);
                if (bridge?.bridgeId == null)
                {
                    steps.Clear();
                    return steps;
                }

                steps.Add(RallyStepCodec.Gate(bridge.bridgeId, path[i + 1]));
            }
        }

        if (anchor.Kind == RallyAnchorKind.SystemOnly)
        {
            return steps;
        }

        if (anchor.BattlefieldId == null || anchor.EventRegionId == null)
        {
            return steps;
        }

        unitBf = FindBattlefieldForUnit(state, unit);
        var sameBf = unitBf?.battlefieldId != null
            && unitBf.battlefieldId.Equals(anchor.BattlefieldId, StringComparison.Ordinal);

        if (sameBf)
        {
            if (anchor.Kind == RallyAnchorKind.ShipPosition)
            {
                steps.Add(RallyStepCodec.Navigate(anchor.X, anchor.Y, anchor.Z));
            }

            return steps;
        }

        if (anchor.Kind == RallyAnchorKind.SceneLanding)
        {
            steps.Add(RallyStepCodec.WarpLanding(
                anchor.BattlefieldId,
                anchor.LandingDistM > 0f
                    ? anchor.LandingDistM
                    : TacticalWarpLandingService.ResolveLandingDistM(state)));
        }
        else if (anchor.Kind == RallyAnchorKind.ShipPosition)
        {
            steps.Add(RallyStepCodec.WarpLanding(anchor.BattlefieldId, 0f));
            steps.Add(RallyStepCodec.Navigate(anchor.X, anchor.Y, anchor.Z));
        }

        return steps;
    }

    public static List<string> PlanBridgePath(GameState state, string fromSystemId, string toSystemId)
    {
        var result = new List<string>();
        if (fromSystemId.Equals(toSystemId, StringComparison.Ordinal))
        {
            result.Add(fromSystemId);
            return result;
        }

        var project = state.map?.Project;
        if (project == null)
        {
            return result;
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

        if (!idToIndex.ContainsKey(fromSystemId) || !idToIndex.ContainsKey(toSystemId))
        {
            return result;
        }

        var adj = new List<(int next, string bridgeId)>[project.systems.Count];
        for (var i = 0; i < adj.Length; i++)
        {
            adj[i] = new List<(int, string)>();
        }

        foreach (var br in project.bridges)
        {
            if (br.fromSystemId == null || br.toSystemId == null || br.bridgeId == null)
            {
                continue;
            }

            if (!idToIndex.TryGetValue(br.fromSystemId, out var a)
                || !idToIndex.TryGetValue(br.toSystemId, out var b))
            {
                continue;
            }

            adj[a].Add((b, br.bridgeId));
            adj[b].Add((a, br.bridgeId));
        }

        var start = idToIndex[fromSystemId];
        var goal = idToIndex[toSystemId];
        var prev = new int[project.systems.Count];
        for (var i = 0; i < prev.Length; i++)
        {
            prev[i] = -1;
        }

        var seen = new bool[project.systems.Count];
        var queue = new Queue<int>();
        seen[start] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == goal)
            {
                break;
            }

            foreach (var (next, _) in adj[cur])
            {
                if (seen[next])
                {
                    continue;
                }

                seen[next] = true;
                prev[next] = cur;
                queue.Enqueue(next);
            }
        }

        if (!seen[goal])
        {
            return result;
        }

        var chain = new List<int>();
        for (var at = goal; at >= 0; at = prev[at])
        {
            chain.Add(at);
        }

        chain.Reverse();
        foreach (var idx in chain)
        {
            var id = project.systems[idx].solarSystemId;
            if (id != null)
            {
                result.Add(id);
            }
        }

        return result;
    }

    public static bool ApplyFirstStep(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState unitBf,
        List<string> steps,
        ShipRegistry ships)
    {
        if (steps.Count == 0)
        {
            return false;
        }

        var step = steps[0];
        if (RallyStepCodec.TryParseNavigate(step, out var nx, out var ny, out var nz))
        {
            NavigationService.AssignNavigate(unit, nx, ny, nz);
            return true;
        }

        if (RallyStepCodec.TryParseWarpLanding(step, out var targetBfId, out var landingDistM))
        {
            var target = TacticalWarpService.FindBattlefield(state, targetBfId);
            if (target == null)
            {
                return false;
            }

            if (unitBf.battlefieldId != null
                && unitBf.battlefieldId.Equals(targetBfId, StringComparison.Ordinal))
            {
                if (steps.Count > 1 && RallyStepCodec.TryParseNavigate(steps[1], out nx, out ny, out nz))
                {
                    NavigationService.AssignNavigate(unit, nx, ny, nz);
                    return true;
                }

                return false;
            }

            var dist = landingDistM > 0f
                ? landingDistM
                : TacticalWarpLandingService.ResolveLandingDistM(state);
            FieldAuraWarpGate.PrepareHolderForWarp(unitBf, unit);
            return TacticalWarpService.TryOrderWarp(
                state,
                unit,
                unitBf,
                target,
                ships.FindHull(unit.hullId),
                dist) == null;
        }

        if (RallyStepCodec.TryParseGate(step, out var bridgeId, out var targetSystemId))
        {
            return TryBeginGateStep(state, unit, unitBf, bridgeId, targetSystemId, ships);
        }

        return false;
    }

    private static bool TryBeginGateStep(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState unitBf,
        string bridgeId,
        string targetSystemId,
        ShipRegistry ships)
    {
        var gateUnit = FindJumpBridgeUnit(unitBf, bridgeId);
        if (gateUnit != null)
        {
            return JumpBridgeTransitService.TryTransit(state, unit, unitBf, gateUnit, out _);
        }

        var gateRegion = JumpBridgeResolver.FindGateRegion(state, unitBf.systemId, bridgeId);
        if (gateRegion?.eventRegionId == null || unitBf.systemId == null)
        {
            return false;
        }

        if (!unitBf.eventRegionId?.Equals(gateRegion.eventRegionId, StringComparison.Ordinal) ?? true)
        {
            var gateBf = TacticalSceneBattlefieldService.EnsureSceneBattlefield(
                state,
                unitBf.systemId,
                gateRegion.eventRegionId);
            FieldAuraWarpGate.PrepareHolderForWarp(unitBf, unit);
            return TacticalWarpService.TryOrderWarp(
                state,
                unit,
                unitBf,
                gateBf,
                ships.FindHull(unit.hullId),
                TacticalWarpLandingService.ResolveLandingDistM(state)) == null;
        }

        gateUnit = FindJumpBridgeUnit(unitBf, bridgeId);
        return gateUnit != null
            && JumpBridgeTransitService.TryTransit(state, unit, unitBf, gateUnit, out _);
    }

    private static BattlefieldUnit? FindJumpBridgeUnit(BattlefieldState bf, string bridgeId)
    {
        foreach (var u in bf.units)
        {
            if (u.isBuilding
                && bridgeId.Equals(u.bridgeId, StringComparison.Ordinal)
                && JumpBridgeUnitService.IsJumpBridgeBuilding(u))
            {
                return u;
            }
        }

        return null;
    }

    public static BattlefieldState? FindBattlefieldForUnit(GameState state, BattlefieldUnit unit)
    {
        if (unit.unitId == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            foreach (var u in bf.units)
            {
                if (unit.unitId.Equals(u.unitId, StringComparison.Ordinal))
                {
                    return bf;
                }
            }
        }

        return null;
    }

    private static string? FindUnitSystemId(GameState state, BattlefieldUnit unit) =>
        FindBattlefieldForUnit(state, unit)?.systemId;

    private static string? FindUnitEventRegionId(GameState state, BattlefieldUnit unit) =>
        FindBattlefieldForUnit(state, unit)?.eventRegionId;

    private static string? FindUnitBattlefieldId(GameState state, BattlefieldUnit unit) =>
        FindBattlefieldForUnit(state, unit)?.battlefieldId;
}
