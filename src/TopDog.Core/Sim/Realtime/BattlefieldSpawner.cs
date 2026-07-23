using System;
using System.Collections.Generic;
using System.Linq;
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
 * ?? ?????? ??
 * ??: docs/COMBAT_ROSTER.md �???? � docs/MATCH_FLOW.md �??????(REALTIME)
 * ???: BattlefieldSpawner.cs ? CombatQueueEntry ? BattlefieldState[] ??
 * ??????
 * � SpawnAll?BUILDING_ASSAULT / HARVEST / COUNTER_HARVEST / ?????
 * � ?? memberId + ?? roster line ? AddFriendlyMember / AddEnemyLine
 * � ????? spawn ????????? DeployForFocusCommand?
 * � ?????? opsDeployEventRegionId ??? battlefield
 * ????CombatRosterBuilder � StrikeWingSpawnService � MissileSpawnService � BoardSummonService
 * ??
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
                list.AddRange(SpawnBuildingAssaultRegions(state, entry, building, ships, modules, rng));
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
            list.AddRange(SpawnFriendlySplitRegions(state, entry, ships, modules, rng, includeEnemiesOnBattlefield: true));
        }

        list.RemoveAll(bf => bf.units.Count == 0);
        BoardSummonService.TryResolvePendingAcrossBattlefields(state, list, ships, modules, rng);
        foreach (var bf in list)
        {
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

    private const char SpawnSiteSep = '\u001f';

    private static List<BattlefieldState> SpawnBuildingAssaultRegions(
        GameState state,
        CombatQueueEntry entry,
        BuildingState building,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var sites = CollectFriendlySpawnSites(state, entry, rng);
        var list = new List<BattlefieldState>();
        foreach (var kv in sites)
        {
            ParseSpawnSiteKey(kv.Key, out var sysId, out var regionId);
            var onAssault = IsBattlefieldSystem(sysId, entry);
            var bf = NewBattlefield(state, entry, regionId, onAssault ? entry.battlefieldSystemId : sysId);
            if (onAssault)
            {
                bf.targetBuildingId = building.buildingId;
                bf.eventRegionId = building.eventRegionId ?? regionId;
                bf.subLocation = building.subLocation ?? building.displayName ?? regionId;
            }

            foreach (var memberId in kv.Value)
            {
                var m = FindMember(state, memberId);
                if (m?.equippedHullId != null)
                {
                    AddFriendlyMember(bf, state, m, ships, modules, 0f, rng);
                }
            }

            if (onAssault)
            {
                var enemySpawned = 0;
                foreach (var line in entry.enemyRoster)
                {
                    if (!string.IsNullOrWhiteSpace(line.memberId))
                    {
                        var em = FindMember(state, line.memberId);
                        if (em?.equippedHullId != null)
                        {
                            AddEnemyMember(bf, state, em, ships, modules, 0f, rng);
                            enemySpawned++;
                            continue;
                        }
                    }
                    var before = bf.units.Count;
                    AddEnemyLine(bf, state, line, ships, modules, 0f, rng);
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
            }

            if (bf.units.Count > 0)
            {
                list.Add(bf);
            }
        }

        if (list.Count == 0)
        {
            var bf = NewBattlefield(state, entry);
            bf.targetBuildingId = building.buildingId;
            bf.eventRegionId = building.eventRegionId;
            bf.subLocation = building.subLocation ?? building.displayName;
            BuildingCombatRules.SpawnBuildingUnit(bf, building);
            list.Add(bf);
        }

        return list;
    }

    // liketocoode34e

    private static List<BattlefieldState> SpawnHarvestRegions(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        return SpawnFriendlySplitRegions(state, entry, ships, modules, rng, includeEnemiesOnBattlefield: true);
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
            if (CombatAttendancePolicies.ShouldExcludeFromFight(state, entry, line.memberId))
            {
                continue;
            }
            var key = ResolveLineSpawnSiteKey(state, line, entry, rng);
            if (!regionLines.TryGetValue(key, out var bucket))
            {
                bucket = new List<CombatRosterLine>();
                regionLines[key] = bucket;
            }
            bucket.Add(line);
        }
        if (regionLines.Count == 0)
        {
            return SpawnFriendlySplitRegions(state, entry, ships, modules, rng, includeEnemiesOnBattlefield: true);
        }

        var list = new List<BattlefieldState>();
        foreach (var kv in regionLines)
        {
            ParseSpawnSiteKey(kv.Key, out var sysId, out var regionId);
            var onBattle = IsBattlefieldSystem(sysId, entry);
            var bf = NewBattlefield(state, entry, regionId, onBattle ? entry.battlefieldSystemId : sysId);
            foreach (var line in kv.Value)
            {
                var arrival = line.arrivalSec >= 0 ? line.arrivalSec : 0f;
                AddFriendlyFromLine(bf, state, line, ships, modules, arrival, rng);
            }
            if (onBattle)
            {
                foreach (var line in entry.enemyRoster)
                {
                    var arrival = line.arrivalSec >= 0 ? line.arrivalSec : 0f;
                    AddEnemyLine(bf, state, line, ships, modules, arrival, rng);
                }
            }
            if (bf.units.Count > 0)
            {
                list.Add(bf);
            }
        }
        return list;
    }

    private static List<BattlefieldState> SpawnFriendlySplitRegions(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng,
        bool includeEnemiesOnBattlefield)
    {
        var sites = CollectFriendlySpawnSites(state, entry, rng);
        var list = new List<BattlefieldState>();
        foreach (var kv in sites)
        {
            ParseSpawnSiteKey(kv.Key, out var sysId, out var regionId);
            var onBattle = IsBattlefieldSystem(sysId, entry);
            var bf = NewBattlefield(state, entry, regionId, onBattle ? entry.battlefieldSystemId : sysId);
            foreach (var memberId in kv.Value)
            {
                var m = FindMember(state, memberId);
                if (m?.equippedHullId != null)
                {
                    AddFriendlyMember(bf, state, m, ships, modules, 0f, rng);
                }
            }
            if (includeEnemiesOnBattlefield && onBattle)
            {
                foreach (var line in entry.enemyRoster)
                {
                    AddEnemyLine(bf, state, line, ships, modules, 0f, rng);
                }
            }
            if (bf.units.Count > 0)
            {
                list.Add(bf);
            }
        }
        if (list.Count == 0)
        {
            var bf = NewBattlefield(state, entry);
            if (includeEnemiesOnBattlefield)
            {
                foreach (var line in entry.enemyRoster)
                {
                    AddEnemyLine(bf, state, line, ships, modules, 0f, rng);
                }
            }
            list.Add(bf);
        }
        return list;
    }

    private static Dictionary<string, List<string>> CollectFriendlySpawnSites(
        GameState state,
        CombatQueueEntry entry,
        Random rng)
    {
        var sites = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var memberId in entry.friendlyMemberIds)
        {
            if (CombatAttendancePolicies.ShouldExcludeFromFight(state, entry, memberId))
            {
                continue;
            }
            var m = FindMember(state, memberId);
            var key = ResolveMemberSpawnSiteKey(state, m, entry, rng);
            if (!sites.TryGetValue(key, out var ids))
            {
                ids = new List<string>();
                sites[key] = ids;
            }
            ids.Add(memberId);
        }
        return sites;
    }

    // liketoco0de345

    private static string ResolveMemberSpawnSiteKey(
        GameState state,
        MemberState? m,
        CombatQueueEntry entry,
        Random rng)
    {
        if (IsDeployedToBattlefield(m, entry))
        {
            var region = !string.IsNullOrWhiteSpace(m!.opsDeployEventRegionId)
                ? m.opsDeployEventRegionId!
                : !string.IsNullOrWhiteSpace(m.opsDeploySubLocation)
                    ? m.opsDeploySubLocation!
                    : entry.battlefieldSubLocation ?? entry.battlefieldSystemId ?? "_default";
            return MakeSpawnSiteKey(entry.battlefieldSystemId ?? "_default", region);
        }

        return MakeHomeBaseSpawnSiteKey(state, m, entry, rng);
    }

    private static string ResolveLineSpawnSiteKey(
        GameState state,
        CombatRosterLine line,
        CombatQueueEntry entry,
        Random rng)
    {
        if (line.memberId != null)
        {
            return ResolveMemberSpawnSiteKey(state, FindMember(state, line.memberId), entry, rng);
        }
        return MakeSpawnSiteKey(
            entry.battlefieldSystemId ?? "_default",
            entry.battlefieldSubLocation ?? entry.battlefieldSystemId ?? "_default");
    }

    private static bool IsDeployedToBattlefield(MemberState? m, CombatQueueEntry entry)
    {
        if (m == null || string.IsNullOrWhiteSpace(entry.battlefieldSystemId))
        {
            return false;
        }
        if (entry.battlefieldSystemId.Equals(m.opsDeploySystemId, StringComparison.Ordinal))
        {
            return true;
        }
        return OpsDeploymentHelper.MustAttendSystemCombat(m, entry.battlefieldSystemId);
    }

    private static string MakeHomeBaseSpawnSiteKey(
        GameState state,
        MemberState? m,
        CombatQueueEntry entry,
        Random rng)
    {
        var docks = BuildingService.PlayerDockableBuildings(state);
        if (docks.Count > 0)
        {
            var b = docks[rng.Next(docks.Count)];
            var region = !string.IsNullOrWhiteSpace(b.eventRegionId)
                ? b.eventRegionId!
                : !string.IsNullOrWhiteSpace(b.subLocation)
                    ? b.subLocation!
                    : BattlefieldLocations.RandomSubLocation(rng);
            var sys = b.solarSystemId ?? m?.currentSolarSystemId ?? "_home";
            if (m != null && !string.IsNullOrWhiteSpace(b.solarSystemId))
            {
                m.currentSolarSystemId = b.solarSystemId;
            }
            return MakeSpawnSiteKey(sys, region);
        }

        var homeSys = m?.currentSolarSystemId ?? m?.opsDeploySystemId ?? "_home";
        if (IsBattlefieldSystem(homeSys, entry))
        {
            homeSys = "_home";
        }
        return MakeSpawnSiteKey(homeSys, BattlefieldLocations.RandomSubLocation(rng));
    }

    private static string MakeSpawnSiteKey(string systemId, string regionId) =>
        systemId + SpawnSiteSep + regionId;

    private static void ParseSpawnSiteKey(string key, out string systemId, out string regionId)
    {
        var i = key.IndexOf(SpawnSiteSep);
        if (i < 0)
        {
            systemId = key;
            regionId = key;
            return;
        }
        systemId = key[..i];
        regionId = key[(i + 1)..];
    }

    private static bool IsBattlefieldSystem(string? systemId, CombatQueueEntry entry) =>
        !string.IsNullOrWhiteSpace(systemId)
        && !string.IsNullOrWhiteSpace(entry.battlefieldSystemId)
        && systemId.Equals(entry.battlefieldSystemId, StringComparison.Ordinal);

    // lik3tocoode345

    private static BattlefieldState SpawnSingle(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng,
        float _)
    {
        var multi = SpawnFriendlySplitRegions(state, entry, ships, modules, rng, includeEnemiesOnBattlefield: true);
        foreach (var bf in multi)
        {
            if (IsBattlefieldSystem(bf.systemId, entry))
            {
                return bf;
            }
        }
        return multi[0];
    }

    private static BattlefieldState NewBattlefield(
        GameState state,
        CombatQueueEntry entry,
        string? regionOverride = null,
        string? systemOverride = null)
    {
        var regionId = regionOverride ?? entry.battlefieldSubLocation;
        var systemId = systemOverride ?? entry.battlefieldSystemId;
        return new BattlefieldState
        {
            battlefieldId = "bf-" + Guid.NewGuid().ToString("N")[..8],
            combatEntryId = entry.entryId,
            combatSubtype = entry.combatSubtype,
            capturedMemberId = entry.capturedMemberId,
            harvesterMemberId = entry.friendlyMemberIds.Count > 0 ? entry.friendlyMemberIds[0] : null,
            systemId = systemId,
            subLocation = regionId,
            eventRegionId = regionId,
            anchorAu = BattlefieldAnchorResolver.Resolve(state, systemId, regionId),
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
        u.shipInstanceId = m.equippedShipInstanceId;
        AttachCarriedShipInstances(state, u);
        u.legionId = m.legionId;
        u.fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, m));
        TraitGrantedModuleService.ApplyForMember(m, u, modules);
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
        u.shipInstanceId = m.equippedShipInstanceId;
        AttachCarriedShipInstances(state, u);
        u.legionId = m.legionId;
        u.fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, m));
        TraitGrantedModuleService.ApplyForMember(m, u, modules);
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
            $"{u.unitId} {u.displayName} mods=[{modList}] salvo={u.salvoRoundDmg:F0} cycle={u.fireCycleSec:F1}s range={u.attackRangeM / 1000f:F0}km track={u.weaponTrackingDegPerSec:F1}�/s");
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
        GameState state,
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
        TraitGrantedModuleService.ApplyToSpawnedUnit(state, u, modules);
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        LogUnitFit(u);
        bf.units.Add(u);
    }

    private static void AddEnemyLine(
        BattlefieldState bf,
        GameState state,
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
        var u = BaseUnit(line.displayName ?? "??", line.hullId, hull, UnitSide.ENEMY, arrival, rng);
        u.memberId = line.memberId;
        u.legionId = line.legionId;
        u.fittedModules = new Dictionary<string, string>(line.fittedModules);
        TraitGrantedModuleService.ApplyToSpawnedUnit(state, u, modules);
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

    private static void AttachCarriedShipInstances(GameState state, BattlefieldUnit unit)
    {
        if (string.IsNullOrWhiteSpace(unit.shipInstanceId))
        {
            return;
        }
        unit.carriedShipsBySlot = BuildCarriedPayloads(
            state,
            unit.shipInstanceId,
            new HashSet<string>(StringComparer.Ordinal),
            depth: 0);
    }

    private static Dictionary<string, CarriedShipPayload> BuildCarriedPayloads(
        GameState state,
        string carrierShipInstanceId,
        HashSet<string> path,
        int depth)
    {
        var result = new Dictionary<string, CarriedShipPayload>(StringComparer.Ordinal);
        if (depth >= CarriedUnitDeploymentService.MaxCarryDepth || !path.Add(carrierShipInstanceId))
        {
            return result;
        }
        foreach (var ship in state.shipInstances
                     .Where(ship => !ship.destroyed
                                    && carrierShipInstanceId.Equals(
                                        ship.carrierShipInstanceId,
                                        StringComparison.Ordinal)
                                    && !string.IsNullOrWhiteSpace(ship.carrierBaySlot))
                     .OrderBy(ship => ship.shipInstanceId, StringComparer.Ordinal))
        {
            if (result.ContainsKey(ship.carrierBaySlot!))
            {
                continue;
            }
            result[ship.carrierBaySlot!] = new CarriedShipPayload
            {
                shipInstanceId = ship.shipInstanceId,
                hullId = ship.hullId,
                operatorMemberId = ship.operatorMemberId,
                fittedModules = new Dictionary<string, string>(ship.fittedModules, StringComparer.Ordinal),
                carriedShipsBySlot = BuildCarriedPayloads(
                    state,
                    ship.shipInstanceId,
                    new HashSet<string>(path, StringComparer.Ordinal),
                    depth + 1),
                shieldHp = ship.shieldHp,
                armorHp = ship.armorHp,
                structureHp = ship.structureHp,
            };
        }
        return result;
    }

    private static MemberState? FindMember(GameState state, string id) =>
        LegionPlayerRegistry.FindMember(state, id);
}
