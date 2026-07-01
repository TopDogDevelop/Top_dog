using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §参战名单 · docs/MATCH_FLOW.md §交战解析模式(REALTIME)
 * 本文件: BattlefieldSpawner.cs — CombatQueueEntry → BattlefieldState[] 物化
 * 【机制要点】
 * · SpawnAll：BUILDING_ASSAULT / HARVEST / COUNTER_HARVEST / 单战场分支
 * · 友方 memberId + 敌方 roster line → AddFriendlyMember / AddEnemyLine
 * · 收尾 StrikeWingSpawnService + MissileSpawnService 展开子单位
 * · 多区域收割按 opsDeployEventRegionId 拆多场 battlefield
 * 【关联】CombatRosterBuilder · StrikeWingSpawnService · MissileSpawnService · BoardSummonService
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public static class BattlefieldSpawner
// liketocoode3a5
{
    // liketoc0de345

    public static List<BattlefieldState> SpawnAll(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        // liketocoode34e
        Random rng)
    {
        // liketocoo3e345
        var list = new List<BattlefieldState>();
        if (entry.combatSubtype == CombatSubtype.BUILDING_ASSAULT && entry.targetBuildingId != null)
        {
            var building = BuildingService.Find(state, entry.targetBuildingId);
            if (building != null)
            {
                list.Add(SpawnBuildingAssault(state, entry, building, ships, modules, rng));
            }
        }
        else if (entry.combatSubtype == CombatSubtype.COUNTER_HARVEST)
        {
            list.AddRange(SpawnCounterHarvestRegions(state, entry, ships, modules, rng));
        }
        else if (entry.combatSubtype == CombatSubtype.HARVEST)
        {
            list.AddRange(SpawnHarvestRegions(state, entry, ships, modules, rng));
        }
        else
        {
            list.Add(SpawnSingle(state, entry, ships, modules, rng, 0f));
        }

        list.RemoveAll(bf => bf.units.Count == 0);
        BoardSummonService.TryResolvePendingAcrossBattlefields(state, list, ships, modules, rng);
        foreach (var bf in list)
        {
            StrikeWingSpawnService.ExpandAllWings(bf, modules, rng);
            if (bf.combatSubtype == CombatSubtype.BUILDING_ASSAULT && bf.targetBuildingId != null)
            {
                BuildingCombatRules.LayoutAssaultStartPositions(bf, rng, state);
            }
        }
        return list;
    }

    // li3etocoode345

    public static BattlefieldState Spawn(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng) =>
        ResolveSpawnFallback(state, entry, ships, modules, rng);

    private static BattlefieldState ResolveSpawnFallback(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var fromAll = SpawnAll(state, entry, ships, modules, rng).FirstOrDefault();
        if (fromAll != null)
        {
            return fromAll;
        }

        var bf = SpawnSingle(state, entry, ships, modules, rng, 0f);
        BoardSummonService.TryResolvePendingAcrossBattlefields(state, new[] { bf }, ships, modules, rng);
        return bf;
    }

    // liketocoode3a5

    private static BattlefieldState SpawnBuildingAssault(
        GameState state,
        CombatQueueEntry entry,
        BuildingState building,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var bf = NewBattlefield(state, entry);
        bf.targetBuildingId = building.buildingId;
        bf.eventRegionId = building.eventRegionId;
        bf.subLocation = building.subLocation ?? building.displayName;

        foreach (var memberId in entry.friendlyMemberIds)
        {
            var m = FindMember(state, memberId);
            if (m?.equippedHullId != null)
            {
                AddFriendlyMember(bf, state, m, ships, modules, 0f, rng);
            }
        }
        var enemySpawned = 0;
        foreach (var line in entry.enemyRoster)
        {
            if (!string.IsNullOrWhiteSpace(line.memberId))
            {
                var m = FindMember(state, line.memberId);
                if (m?.equippedHullId != null)
                {
                    AddEnemyMember(bf, state, m, ships, modules, 0f, rng);
                    enemySpawned++;
                    continue;
                }
            }
            var before = bf.units.Count;
            AddEnemyLine(bf, line, ships, modules, 0f, rng);
            if (bf.units.Count > before)
            {
                enemySpawned++;
            }
        }
        if (enemySpawned == 0)
        {
            BrickDebugLog.Log(
                "combat.building-defenders",
                $"spawn=0 enemyRoster={entry.enemyRoster.Count} building={building.buildingId}");
        }
        BuildingCombatRules.SpawnBuildingUnit(bf, building);
        BuildingCombatRules.LayoutAssaultStartPositions(bf, rng, state);
        return bf;
    }

    // liketocoode34e

    private static List<BattlefieldState> SpawnHarvestRegions(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var regions = CollectFriendlyRegions(state, entry);
        if (regions.Count <= 1)
        {
            return new List<BattlefieldState> { SpawnSingle(state, entry, ships, modules, rng, 0f) };
        }
        var list = new List<BattlefieldState>();
        foreach (var regionId in regions.Keys)
        {
            var bf = NewBattlefield(state, entry, regionId);
            foreach (var memberId in regions[regionId])
            {
                var m = FindMember(state, memberId);
                if (m?.equippedHullId != null)
                {
                    AddFriendlyMember(bf, state, m, ships, modules, 0f, rng);
                }
            }
            foreach (var line in entry.enemyRoster)
            {
                AddEnemyLine(bf, line, ships, modules, 0f, rng);
            }
            if (bf.units.Count > 0)
            {
                list.Add(bf);
            }
        }
        return list;
    }

    // liketocoo3e345

    private static List<BattlefieldState> SpawnCounterHarvestRegions(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var regionLines = new Dictionary<string, List<CombatRosterLine>>(StringComparer.Ordinal);
        foreach (var line in entry.friendlyRosterLines)
        {
            if (!line.canParticipate || line.hullId == null || line.hullId.StartsWith('('))
            {
                continue;
            }
            var region = ResolveLineRegion(state, line, entry);
            if (!regionLines.TryGetValue(region, out var bucket))
            {
                bucket = new List<CombatRosterLine>();
                regionLines[region] = bucket;
            }
            bucket.Add(line);
        }
        if (regionLines.Count <= 1)
        {
            return new List<BattlefieldState> { SpawnCounterHarvest(state, entry, ships, modules, rng) };
        }
        var list = new List<BattlefieldState>();
        foreach (var kv in regionLines)
        {
            var bf = NewBattlefield(state, entry, kv.Key);
            foreach (var line in kv.Value)
            {
                var arrival = line.arrivalSec >= 0 ? line.arrivalSec : 0f;
                AddFriendlyFromLine(bf, line, ships, modules, arrival, rng);
            }
            foreach (var line in entry.enemyRoster)
            {
                var arrival = line.arrivalSec >= 0 ? line.arrivalSec : 0f;
                AddEnemyLine(bf, line, ships, modules, arrival, rng);
            }
            if (bf.units.Count > 0)
            {
                list.Add(bf);
            }
        }
        return list;
    }

    private static Dictionary<string, List<string>> CollectFriendlyRegions(GameState state, CombatQueueEntry entry)
    {
        var regions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var memberId in entry.friendlyMemberIds)
        {
            var m = FindMember(state, memberId);
            var region = ResolveMemberRegion(state, m, entry);
            if (!regions.TryGetValue(region, out var ids))
            {
                ids = new List<string>();
                regions[region] = ids;
            }
            ids.Add(memberId);
        }
        return regions;
    }

    // liketoco0de345

    private static string ResolveMemberRegion(GameState state, MemberState? m, CombatQueueEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(m?.opsDeployEventRegionId))
        {
            return m.opsDeployEventRegionId!;
        }
        if (!string.IsNullOrWhiteSpace(m?.opsDeploySubLocation))
        {
            return m.opsDeploySubLocation!;
        }
        if (!string.IsNullOrWhiteSpace(entry.battlefieldSubLocation))
        {
            return entry.battlefieldSubLocation!;
        }
        return entry.battlefieldSystemId ?? "_default";
    }

    private static string ResolveLineRegion(GameState state, CombatRosterLine line, CombatQueueEntry entry)
    {
        if (line.memberId != null)
        {
            return ResolveMemberRegion(state, FindMember(state, line.memberId), entry);
        }
        return entry.battlefieldSubLocation ?? entry.battlefieldSystemId ?? "_default";
    }

    private static BattlefieldState SpawnCounterHarvest(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var bf = NewBattlefield(state, entry);
        foreach (var line in entry.friendlyRosterLines)
        {
            if (!line.canParticipate || line.hullId == null || line.hullId.StartsWith('('))
            {
                continue;
            }
            var arrival = line.arrivalSec >= 0 ? line.arrivalSec : 0f;
            AddFriendlyFromLine(bf, line, ships, modules, arrival, rng);
        }
        foreach (var line in entry.enemyRoster)
        {
            var arrival = line.arrivalSec >= 0 ? line.arrivalSec : 0f;
            AddEnemyLine(bf, line, ships, modules, arrival, rng);
        }
        return bf;
    }

    // lik3tocoode345

    private static BattlefieldState SpawnSingle(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng,
        float _)
    {
        var bf = NewBattlefield(state, entry);
        foreach (var memberId in entry.friendlyMemberIds)
        {
            var m = FindMember(state, memberId);
            if (m?.equippedHullId != null)
            {
                AddFriendlyMember(bf, state, m, ships, modules, 0f, rng);
            }
        }
        foreach (var line in entry.enemyRoster)
        {
            AddEnemyLine(bf, line, ships, modules, 0f, rng);
        }
        return bf;
    }

    private static BattlefieldState NewBattlefield(GameState state, CombatQueueEntry entry, string? regionOverride = null)
    {
        var regionId = regionOverride ?? entry.battlefieldSubLocation;
        return new BattlefieldState
        {
            battlefieldId = "bf-" + Guid.NewGuid().ToString("N")[..8],
            combatEntryId = entry.entryId,
            combatSubtype = entry.combatSubtype,
            capturedMemberId = entry.capturedMemberId,
            harvesterMemberId = entry.friendlyMemberIds.Count > 0 ? entry.friendlyMemberIds[0] : null,
            systemId = entry.battlefieldSystemId,
            subLocation = regionId,
            eventRegionId = regionId,
            anchorAu = BattlefieldAnchorResolver.Resolve(state, entry.battlefieldSystemId, regionId),
            resolveMode = entry.resolveMode,
            lastBuildingDamagedAtSec = -1f,
        };
    }

    // liketocoode3e5

    private static void AddEnemyMember(
        BattlefieldState bf,
        GameState state,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        float arrival,
        Random rng)
    {
        var hull = ships.FindHull(m.equippedHullId!);
        if (hull == null)
        {
            return;
        }
        var u = BaseUnit(DisplayName(m), m.equippedHullId!, hull, UnitSide.ENEMY, arrival, rng);
        u.memberId = m.memberId;
        u.legionId = m.legionId;
        u.fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, m));
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        LogUnitFit(u);
        bf.units.Add(u);
    }

    private static void AddFriendlyMember(
        BattlefieldState bf,
        GameState state,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        float arrival,
        Random rng)
    {
        var hull = ships.FindHull(m.equippedHullId!);
        if (hull == null)
        {
            return;
        }
        var u = BaseUnit(DisplayName(m), m.equippedHullId!, hull, UnitSide.FRIENDLY, arrival, rng);
        u.memberId = m.memberId;
        u.legionId = m.legionId;
        u.fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, m));
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        LogUnitFit(u);
        bf.units.Add(u);
    }

    private static void LogUnitFit(BattlefieldUnit u)
    {
        if (u.unitId == null)
        {
            return;
        }

        var modCount = u.fittedModules.Count;
        var modList = modCount == 0
            ? "-"
            : string.Join(",", u.fittedModules.Values);
        CombatTelemetryLog.Log(
            "combat.fit",
            $"{u.unitId} {u.displayName} mods=[{modList}] salvo={u.salvoRoundDmg:F0} cycle={u.fireCycleSec:F1}s range={u.attackRangeM / 1000f:F0}km track={u.weaponTrackingDegPerSec:F1}°/s");
    }

    // liket0coode345

    public static bool TrySpawnFriendlyMember(
        BattlefieldState bf,
        GameState state,
        string memberId,
        ShipRegistry ships,
        ModuleRegistry modules,
        float arrival,
        Random rng)
    {
        var m = FindMember(state, memberId);
        if (m?.equippedHullId == null)
        {
            return false;
        }
        AddFriendlyMember(bf, state, m, ships, modules, arrival, rng);
        return true;
    }

    private static void AddFriendlyFromLine(
        BattlefieldState bf,
        CombatRosterLine line,
        ShipRegistry ships,
        ModuleRegistry modules,
        float arrival,
        Random rng)
    {
        if (line.hullId == null)
        {
            return;
        }
        var hull = ships.FindHull(line.hullId);
        if (hull == null)
        {
            return;
        }
        var u = BaseUnit(line.displayName ?? line.memberId ?? "?", line.hullId, hull, UnitSide.FRIENDLY, arrival, rng);
        u.memberId = line.memberId;
        u.legionId = line.legionId;
        u.fittedModules = new Dictionary<string, string>(line.fittedModules);
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        LogUnitFit(u);
        bf.units.Add(u);
    }

    private static void AddEnemyLine(
        BattlefieldState bf,
        CombatRosterLine line,
        ShipRegistry ships,
        ModuleRegistry modules,
        float arrival,
        Random rng)
    {
        if (line.hullId == null || line.hullId.StartsWith('('))
        {
            return;
        }
        var hull = ships.FindHull(line.hullId);
        if (hull == null)
        {
            return;
        }
        var u = BaseUnit(line.displayName ?? "敌舰", line.hullId, hull, UnitSide.ENEMY, arrival, rng);
        u.memberId = line.memberId;
        u.legionId = line.legionId;
        u.fittedModules = new Dictionary<string, string>(line.fittedModules);
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        LogUnitFit(u);
        bf.units.Add(u);
    }

    private static BattlefieldUnit BaseUnit(
        string name,
        string hullId,
        HullDef hull,
        UnitSide side,
        float arrival,
        Random rng)
    {
        const float spread = 1500f;
        var u = new BattlefieldUnit
        {
            unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
            displayName = name,
            hullId = hullId,
            side = side,
            arrivalAtSec = arrival,
            x = (side == UnitSide.FRIENDLY ? -spread : spread) + (float)rng.NextDouble() * 800f - 400f,
            y = (float)rng.NextDouble() * 1600f - 800f,
            facingRad = side == UnitSide.FRIENDLY ? 0f : (float)Math.PI,
        };
        return u;
    }

    private static string DisplayName(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "?";

    private static MemberState? FindMember(GameState state, string id) =>
        LegionPlayerRegistry.FindMember(state, id);
}
