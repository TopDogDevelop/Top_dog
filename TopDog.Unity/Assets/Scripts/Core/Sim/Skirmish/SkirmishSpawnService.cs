using TopDog.App.Brick;
using TopDog.Content.Balance;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §3 · docs/LEGION_SKIRMISH.md §2
 * 本文件: SkirmishSpawnService.cs — 约战战场 bootstrap 与名册 spawn
 * 【机制要点】
 * · BootstrapBattlefields：建全部 eventRegion 战场、spawn 名册、设 activeBattlefieldId
 * · 末段 SeedAllSkirmishSceneProxies + SyncSkirmishLabels（标签仅开局一次）
 * 【实现逻辑】
 * · SeedAllSkirmishSceneProxies：对各 bf 调 SeedSceneProxies（非 tick 重复 sync）
 * · SeedInitialVisionFocus：首个锚点团员附身或 tacticalCameraUnitId
 * 【关联】BattlefieldSceneProxyService · SkirmishDisplayNames · VisionAnchorService
 * ══
 */

namespace TopDog.Sim.Skirmish;

public static class SkirmishSpawnService
{
    public static void BootstrapBattlefields(
        GameState state,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (state.map?.Project.systems.Count == 0)
        {
            return;
        }

        state.battlefields.Clear();
        var sys = state.map.Project.systems[0];
        foreach (var er in sys.eventRegions)
        {
            if (er.eventRegionId == null || EventRegionKinds.IsStar(er.kind))
            {
                continue;
            }

            var bf = new BattlefieldState
            {
                battlefieldId = "skirmish_bf_" + er.eventRegionId,
                systemId = sys.solarSystemId,
                eventRegionId = er.eventRegionId,
                anchorAu = er.anchorAu,
                subLocation = er.name,
            };

            foreach (var building in state.buildings)
            {
                if (building.eventRegionId != null
                    && building.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal))
                {
                    SkirmishBuildingRules.SpawnBuildingUnit(state, bf, building);
                }
            }

            state.battlefields.Add(bf);
        }

        foreach (var legion in state.legions)
        {
            if (legion.legionId == null)
            {
                continue;
            }

            var fortressRegion = FindLegionFortressRegion(state, legion.legionId);
            var bf = state.battlefields.Find(b =>
                fortressRegion != null
                && fortressRegion.Equals(b.eventRegionId, StringComparison.Ordinal));
            if (bf == null)
            {
                continue;
            }

            var spawned = SpawnLegionRoster(state, bf, legion, ships, modules, rng);
            if (spawned == 0 && legion.isLocal)
            {
                BrickDebugLog.Log(
                    "skirmish.spawn",
                    $"local legion {legion.legionId} @ {fortressRegion}: 0 ships (check hullId / content/ships)");
            }
        }

        SetInitialActiveBattlefield(state);
        SkirmishDisplayNames.SyncSkirmishLabels(state);
        SeedInitialVisionFocus(state);
        SyncAllSkirmishSceneProxiesInternal(state);

        SkirmishPhaseRules.EnsureRealtimeCombat(state);
        MatchMemberBaselineService.EnsureSnapshot(state);
    }

    /// <summary>军堡 spawn 半径内均匀立体随机偏移（球体内）。</summary>
    public static void ApplyFortressSpawnOffset(BattlefieldUnit u, Random rng, float radiusM)
    {
        var cosPhi = 1f - 2f * (float)rng.NextDouble();
        var sinPhi = MathF.Sqrt(MathF.Max(0f, 1f - cosPhi * cosPhi));
        var theta = (float)(rng.NextDouble() * Math.PI * 2);
        var r = radiusM * MathF.Pow((float)rng.NextDouble(), 1f / 3f);
        u.x = r * sinPhi * MathF.Cos(theta);
        u.y = r * sinPhi * MathF.Sin(theta);
        u.z = r * cosPhi;
    }

    public static int SpawnLegionRoster(
        GameState state,
        BattlefieldState bf,
        LegionState legion,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var balance = SkirmishBalanceConfig.LoadDefault();
        var radius = balance.spawnRadiusM;
        var side = legion.isLocal ? UnitSide.FRIENDLY : UnitSide.ENEMY;
        var spawned = 0;
        var skippedNoHull = 0;
        var fallbackHull = 0;

        foreach (var member in state.members)
        {
            if (string.IsNullOrEmpty(member.legionId)
                || !member.legionId.Equals(legion.legionId, StringComparison.Ordinal))
            {
                continue;
            }

            var hull = ResolveSpawnHull(member, ships, out var usedFallback);
            if (hull?.hullId == null)
            {
                skippedNoHull++;
                continue;
            }

            if (usedFallback)
            {
                fallbackHull++;
            }

            var u = new BattlefieldUnit
            {
                unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
                displayName = string.IsNullOrWhiteSpace(member.name) ? member.memberId ?? "?" : member.name,
                hullId = hull.hullId,
                memberId = member.memberId,
                legionId = legion.legionId,
                side = side,
                arrivalAtSec = 0f,
                facingRad = side == UnitSide.FRIENDLY ? 0f : (float)Math.PI,
            };
            ApplyFortressSpawnOffset(u, rng, radius);
            u.fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, member));
            ModuleRuntime.ApplyToUnit(u, hull, modules);
            bf.units.Add(u);
            spawned++;
        }

        if (skippedNoHull > 0 || fallbackHull > 0)
        {
            BrickDebugLog.Log(
                "skirmish.spawn",
                $"legion={legion.legionId} spawned={spawned} fallback_hull={fallbackHull} skipped_no_hull={skippedNoHull}");
        }

        return spawned;
    }

    private static HullDef? ResolveSpawnHull(MemberState member, ShipRegistry ships, out bool usedFallback)
    {
        usedFallback = false;
        if (!string.IsNullOrWhiteSpace(member.equippedHullId))
        {
            var hull = ships.FindHull(member.equippedHullId);
            if (hull?.hullId != null)
            {
                return hull;
            }
        }

        foreach (var fallbackId in new[] { "hull_frigate_pineapple", "hull_frigate_shortlegwolf" })
        {
            var hull = ships.FindHull(fallbackId);
            if (hull?.hullId != null)
            {
                usedFallback = true;
                return hull;
            }
        }

        foreach (var hull in ships.AllHulls())
        {
            if (hull.hullId != null)
            {
                usedFallback = true;
                return hull;
            }
        }

        return null;
    }

    private static void SeedInitialVisionFocus(GameState state)
    {
        MemberState? pickMember = null;
        BattlefieldUnit? pickUnit = null;
        var preferPossess = false;

        foreach (var member in state.members)
        {
            if (member.memberId == null || !VisionLocationService.IsVisionEligibleMember(member, state))
            {
                continue;
            }

            var legion = state.legions.Find(l =>
                member.legionId != null
                && member.legionId.Equals(l.legionId, StringComparison.Ordinal));
            if (legion == null || !legion.isLocal)
            {
                continue;
            }

            foreach (var bf in state.battlefields)
            {
                if (bf.finished)
                {
                    continue;
                }

                foreach (var u in bf.units)
                {
                    if (!member.memberId.Equals(u.memberId, StringComparison.Ordinal)
                        || u.side != UnitSide.FRIENDLY
                        || u.IsDestroyed()
                        || !VisionGate.IsRailEligibleFriendly(u, bf.timeSec))
                    {
                        continue;
                    }

                    var hasPossess = VisionLocationService.HasPossessTrait(member);
                    if (pickMember == null || (hasPossess && !preferPossess))
                    {
                        pickMember = member;
                        pickUnit = u;
                        preferPossess = hasPossess;
                        if (preferPossess)
                        {
                            break;
                        }
                    }
                }

                if (preferPossess)
                {
                    break;
                }
            }

            if (preferPossess)
            {
                break;
            }
        }

        if (pickMember?.memberId == null || pickUnit?.unitId == null)
        {
            return;
        }

        if (VisionLocationService.HasPossessTrait(pickMember))
        {
            state.possessingMemberId = pickMember.memberId;
            pickUnit.aiOrder = UnitAiOrder.MANUAL;
            return;
        }

        state.possessingMemberId = null;
        state.tacticalCameraUnitId = pickUnit.unitId;
    }

    private static void SetInitialActiveBattlefield(GameState state)
    {
        foreach (var legion in state.legions)
        {
            if (!legion.isLocal || legion.legionId == null)
            {
                continue;
            }

            var fortressRegion = FindLegionFortressRegion(state, legion.legionId);
            if (fortressRegion == null)
            {
                break;
            }

            foreach (var bf in state.battlefields)
            {
                if (fortressRegion.Equals(bf.eventRegionId, StringComparison.Ordinal))
                {
                    state.activeBattlefieldId = bf.battlefieldId;
                    return;
                }
            }

            break;
        }

        foreach (var bf in state.battlefields)
        {
            if (VisionGate.CountFriendlyPresence(state, bf) > 0)
            {
                state.activeBattlefieldId = bf.battlefieldId;
                return;
            }
        }

        if (state.battlefields.Count > 0)
        {
            state.activeBattlefieldId = state.battlefields[0].battlefieldId;
        }
    }

    public static void SyncAllSkirmishSceneProxies(GameState state)
    {
        foreach (var bf in state.battlefields)
        {
            if (!bf.finished && bf.battlefieldId != null)
            {
                BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
            }
        }
    }

    private static void SyncAllSkirmishSceneProxiesInternal(GameState state) =>
        SyncAllSkirmishSceneProxies(state);

    private static void SyncActiveBattlefieldSceneProxies(GameState state)
    {
        if (state.activeBattlefieldId == null)
        {
            return;
        }

        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
                return;
            }
        }
    }

    private static string? FindLegionFortressRegion(GameState state, string legionId)
    {
        foreach (var building in state.buildings)
        {
            if (building.legionId != null
                && building.legionId.Equals(legionId, StringComparison.Ordinal)
                && string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
            {
                return building.eventRegionId;
            }
        }

        return null;
    }
}
