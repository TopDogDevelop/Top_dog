using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

public static class CombatQueueCompiler
{
    public static void Compile(GameState state, ShipRegistry ships, ModuleRegistry? modules)
    {
        state.combatQueue.Clear();
        state.combatQueueIndex = 0;
        CampaignOutcomeService.ResetCombatRoundEliminations(state);
        if (state.map?.Project == null || state.map.Project.systems.Count == 0)
        {
            return;
        }

        SyncOpsDeployFromTasks(state);
        var rng = new Random((int)(state.gameYear * 1000L + state.gameWeek * 17L + state.storyRound));

        var harvests = new List<OpponentHarvestOp>(state.opponentHarvestOps);
        var plannedTotal = harvests.Count;
        foreach (var assault in state.aiPendingAssaults)
        {
            if (BuildingService.Find(state, assault.buildingId) != null)
            {
                plannedTotal++;
            }
        }
        foreach (var assault in state.playerPendingAssaults)
        {
            if (BuildingService.Find(state, assault.buildingId) != null)
            {
                plannedTotal++;
            }
        }
        foreach (var buildingId in state.aiPendingAssaultBuildingIds)
        {
            if (BuildingService.Find(state, buildingId) != null)
            {
                plannedTotal++;
            }
        }
        foreach (var m in state.members)
        {
            if (MemberDispatchService.TaskHarvest.Equals(m.assignedTask, StringComparison.Ordinal))
            {
                plannedTotal++;
            }
        }

        var ordinal = 0;
        var bridgeAmbushes = new List<CombatQueueEntry>();
        BridgeAmbushService.EnqueueBridgeAmbushes(state, ships, modules, rng, bridgeAmbushes);
        plannedTotal += bridgeAmbushes.Count;
        foreach (var entry in bridgeAmbushes)
        {
            state.combatQueue.Add(TagOrdinal(entry, ++ordinal, Math.Max(1, plannedTotal)));
        }

        foreach (var harvest in harvests)
        {
            if (!CounterHarvestOddsService.RollTrigger(state, harvest.targetSystemId, rng))
            {
                continue;
            }
            state.combatQueue.Add(TagOrdinal(
                BuildCounterHarvest(state, harvest, ships, modules, rng),
                ++ordinal,
                Math.Max(1, plannedTotal)));
        }
        state.opponentHarvestOps.Clear();

        foreach (var m in state.members)
        {
            if (!MemberDispatchService.TaskHarvest.Equals(m.assignedTask, StringComparison.Ordinal))
            {
                continue;
            }
            var sys = m.opsDeploySystemId ?? m.currentSolarSystemId;
            if (sys == null)
            {
                continue;
            }
            state.combatQueue.Add(TagOrdinal(
                BuildMemberHarvest(state, m, sys, ships, modules, rng),
                ++ordinal,
                Math.Max(1, plannedTotal)));
        }

        foreach (var assault in state.aiPendingAssaults.ToList())
        {
            var b = BuildingService.Find(state, assault.buildingId);
            if (b != null)
            {
                state.combatQueue.Add(TagOrdinal(
                    BuildBuildingAssault(state, b, ships, modules, rng, true, assault.attackerLegionId),
                    ++ordinal,
                    Math.Max(1, plannedTotal)));
            }
        }
        state.aiPendingAssaults.Clear();

        foreach (var assault in state.playerPendingAssaults.ToList())
        {
            var b = BuildingService.Find(state, assault.buildingId);
            if (b != null)
            {
                if (assault.systemId != null && !state.activeSiegeSystemIds.Contains(assault.systemId))
                {
                    state.activeSiegeSystemIds.Add(assault.systemId);
                }
                state.combatQueue.Add(TagOrdinal(
                    BuildBuildingAssault(state, b, ships, modules, rng, false, assault.attackerLegionId),
                    ++ordinal,
                    Math.Max(1, plannedTotal)));
            }
        }
        state.playerPendingAssaults.Clear();

        foreach (var buildingId in state.aiPendingAssaultBuildingIds.ToList())
        {
            var b = BuildingService.Find(state, buildingId);
            if (b != null)
            {
                state.combatQueue.Add(TagOrdinal(
                    BuildBuildingAssault(state, b, ships, modules, rng, true, CampaignLegionIds.Ai),
                    ++ordinal,
                    Math.Max(1, plannedTotal)));
            }
        }
        state.aiPendingAssaultBuildingIds.Clear();

        if (state.combatQueue.Count == 0)
        {
            var patrol = BuildPatrolFromDeployments(state, ships, modules, rng);
            if (patrol != null)
            {
                state.combatQueue.Add(TagOrdinal(patrol, 1, 1));
            }
        }
    }

    private static CombatQueueEntry BuildCounterHarvest(
        GameState state,
        OpponentHarvestOp harvest,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        var sys = harvest.targetSystemId;
        var e = new CombatQueueEntry
        {
            entryId = Guid.NewGuid().ToString("N"),
            combatSubtype = CombatSubtype.COUNTER_HARVEST,
            battlefieldSystemId = sys,
            linkedHarvestId = harvest.opId,
            capturedMemberId = harvest.capturedMemberId,
            capturedFormationId = harvest.capturedFormationId,
            label = "反收割 @ " + (sys ?? "?")
                + "（触发率 " + CounterHarvestOddsService.ComputePercent(state, sys) + "%）",
        };
        if (!string.IsNullOrWhiteSpace(harvest.capturedMemberId))
        {
            e.friendlyMemberIds.Add(harvest.capturedMemberId);
            e.mandatoryAttendeeByMember[harvest.capturedMemberId] = true;
        }
        if (!string.IsNullOrWhiteSpace(harvest.capturedFormationId))
        {
            foreach (var m in state.members)
            {
                if (harvest.capturedFormationId.Equals(m.formationId, StringComparison.Ordinal))
                {
                    e.friendlyMemberIds.Add(m.memberId!);
                    e.mandatoryAttendeeByMember[m.memberId!] = true;
                }
            }
        }
        foreach (var m in OpsDeploymentHelper.PickEncounterParticipants(state, sys, 5, rng))
        {
            if (!e.friendlyMemberIds.Contains(m.memberId!))
            {
                e.friendlyMemberIds.Add(m.memberId!);
            }
        }
        var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
        var maxHullId = maxHull?.hullId ?? "hull_bc_spear";
        e.enemyRoster.Add(new CombatRosterLine
        {
            displayName = "敌方收割编队",
            hullId = maxHullId,
            tonnageClass = maxHull?.tonnageClass ?? "BATTLECRUISER",
            combatPower = AssetValuation.HullStarCoinValue(maxHull),
        });
        LegionQuery.TagCombatLegions(
            e,
            CampaignLegionIds.Ai,
            LegionQuery.PrimaryFromMemberIds(state, e.friendlyMemberIds));
        return e;
    }

    private static CombatQueueEntry BuildMemberHarvest(
        GameState state,
        MemberState harvester,
        string systemId,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        var e = new CombatQueueEntry
        {
            entryId = Guid.NewGuid().ToString("N"),
            combatSubtype = CombatSubtype.HARVEST,
            battlefieldSystemId = systemId,
            battlefieldSubLocation = harvester.opsDeployEventRegionId ?? harvester.opsDeploySubLocation,
            label = "收割 @ " + systemId
                + (harvester.opsDeployEventRegionId != null ? " · " + harvester.opsDeployEventRegionId : ""),
        };
        e.friendlyMemberIds.Add(harvester.memberId!);
        foreach (var m in OpsDeploymentHelper.PickEncounterParticipants(state, systemId, 3, rng))
        {
            if (!e.friendlyMemberIds.Contains(m.memberId!))
            {
                e.friendlyMemberIds.Add(m.memberId!);
            }
        }
        var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
        var maxHullId = maxHull?.hullId ?? "hull_bc_spear";
        e.enemyRoster.Add(new CombatRosterLine
        {
            displayName = "遭遇守军",
            hullId = maxHullId,
            tonnageClass = maxHull?.tonnageClass ?? "BATTLECRUISER",
            combatPower = AssetValuation.HullStarCoinValue(maxHull),
        });
        LegionQuery.TagCombatLegions(
            e,
            LegionQuery.OfMember(harvester) ?? LegionQuery.PrimaryFromMemberIds(state, e.friendlyMemberIds),
            CampaignLegionIds.Ai);
        return e;
    }

    private static CombatQueueEntry? BuildPatrolFromDeployments(
        GameState state,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        string? sys = null;
        foreach (var m in state.members)
        {
            if (m.assignedTask != "待命" && !string.IsNullOrEmpty(m.opsDeploySystemId))
            {
                sys = m.opsDeploySystemId;
                break;
            }
        }
        if (sys == null)
        {
            return null;
        }
        return BuildPatrolSkirmish(state, sys, ships, modules, rng);
    }

    private static CombatQueueEntry BuildPatrolSkirmish(
        GameState state,
        string? systemId,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        var e = new CombatQueueEntry
        {
            entryId = Guid.NewGuid().ToString("N"),
            combatSubtype = CombatSubtype.HARVEST,
            battlefieldSystemId = systemId,
            label = "巡逻遭遇战 @ " + (systemId ?? "?"),
        };
        foreach (var m in OpsDeploymentHelper.PickEncounterParticipants(state, systemId, 3, rng))
        {
            e.friendlyMemberIds.Add(m.memberId!);
        }
        var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
        var maxHullId = maxHull?.hullId ?? "hull_bc_spear";
        var pwr = AssetValuation.HullStarCoinValue(maxHull);
        e.enemyRoster.Add(new CombatRosterLine
        {
            displayName = "敌方巡逻编队",
            hullId = maxHullId,
            tonnageClass = maxHull?.tonnageClass ?? "BATTLECRUISER",
            combatPower = pwr,
        });
        LegionQuery.TagCombatLegions(
            e,
            CampaignLegionIds.Ai,
            LegionQuery.PrimaryFromMemberIds(state, e.friendlyMemberIds));
        return e;
    }

    public static CombatQueueEntry BuildBuildingAssault(
        GameState state,
        BuildingState building,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng,
        bool aiAttacker,
        string? attackerLegionId = null)
    {
        var e = new CombatQueueEntry
        {
            entryId = Guid.NewGuid().ToString("N"),
            combatSubtype = CombatSubtype.BUILDING_ASSAULT,
            battlefieldSystemId = building.solarSystemId,
            targetBuildingId = building.buildingId,
            aiAttacker = aiAttacker,
            label = "建筑争夺战 @ " + building.displayName
                + " (" + (BuildingService.Fragile.Equals(building.status, StringComparison.Ordinal) ? "脆弱" : "正常") + ")",
        };
        foreach (var m in OpsDeploymentHelper.PickEncounterParticipants(
                     state, e.battlefieldSystemId, 3, rng))
        {
            e.friendlyMemberIds.Add(m.memberId!);
        }
        var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
        var maxHullId = maxHull?.hullId ?? "hull_bc_spear";
        var pwr = AssetValuation.HullStarCoinValue(maxHull);
        e.enemyRoster.Add(new CombatRosterLine
        {
            displayName = "敌方进攻编队",
            hullId = maxHullId,
            tonnageClass = maxHull?.tonnageClass ?? "BATTLECRUISER",
            combatPower = pwr,
        });
        if (aiAttacker)
        {
            LegionQuery.TagCombatLegions(
                e,
                attackerLegionId ?? CampaignLegionIds.Ai,
                LegionQuery.OfBuilding(building));
        }
        else
        {
            LegionQuery.TagCombatLegions(
                e,
                LegionQuery.PrimaryFromMemberIds(state, e.friendlyMemberIds),
                LegionQuery.OfBuilding(building));
        }
        return e;
    }

    private static CombatQueueEntry TagOrdinal(CombatQueueEntry e, int ordinal, int total)
    {
        e.queueOrdinal = ordinal;
        e.queueTotal = total;
        return e;
    }

    private static void SyncOpsDeployFromTasks(GameState state)
    {
        foreach (var m in state.members)
        {
            if (m.assignedTask != "待命" && m.currentSolarSystemId != null)
            {
                m.opsDeploySystemId = m.currentSolarSystemId;
            }
        }
    }
}
