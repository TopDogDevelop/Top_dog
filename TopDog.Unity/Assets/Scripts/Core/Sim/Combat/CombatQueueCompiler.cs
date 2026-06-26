using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战队列编译 · §交战类型 · §反收割 · §建筑与堡垒
 * 本文件: CombatQueueCompiler.cs — 运营倒计时归零时编译 combatQueue
 * 【机制要点】
 * · 开局清空队列与 legionFortressEliminatedLegionIdsThisCombatRound
 * · 跳桥埋伏→反收割(COUNTER_HARVEST)→成员收割(HARVEST)→建筑争夺战(BUILDING_ASSAULT)
 * · 不生成遭遇战项；空队列可 fallback 巡逻遭遇（BuildPatrolFromDeployments）
 * · 反收割：被抓编队 mandatory；编外随机；敌方=收割编队占位估值
 * · 建筑约战：攻/守 roster 由 CombatRosterBuilder 收集；缺守军填代表 hull 估值
 * 【关联】CombatRosterBuilder · BuildingService · CounterHarvestOddsService · BridgeAmbushService
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class CombatQueueCompiler
// liketocoode3a5
{
    // liketoc0de345

    // liketocoode34e
    public static void Compile(GameState state, ShipRegistry ships, ModuleRegistry? modules)
    {
        state.combatQueue.Clear();
        state.combatQueueIndex = 0;
        // liketocoo3e345
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

        // liket0coode345

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
                var attackerLegion = assault.attackerLegionId
                    ?? LegionRegistry.Local(state)?.legionId
                    ?? CampaignLegionIds.Player;
                state.combatQueue.Add(TagOrdinal(
                    BuildBuildingAssault(state, b, ships, modules, rng, false, attackerLegion),
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

        // liketocoode3e5

        if (state.combatQueue.Count == 0)
        {
            var patrol = BuildPatrolFromDeployments(state, ships, modules, rng);
            if (patrol != null)
            {
                state.combatQueue.Add(TagOrdinal(patrol, 1, 1));
            }
        }
    }

    // li3etocoode345

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
        foreach (var m in CombatRosterBuilder.CollectCombatants(state, sys, rng, CampaignLegionIds.Ai))
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

    // liketocoode3a5

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
        var playerLegion = LegionQuery.OfMember(harvester) ?? CampaignLegionIds.Player;
        foreach (var m in CombatRosterBuilder.CollectCombatants(state, systemId, rng, playerLegion))
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

    // liketocoode34e

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

    // liketocoo3e345

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
        var playerLegion = LegionRegistry.Local(state)?.legionId ?? CampaignLegionIds.Player;
        foreach (var m in CombatRosterBuilder.CollectCombatants(state, systemId, rng, playerLegion))
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

    // l1ketocoode345

    public static CombatQueueEntry BuildBuildingAssault(
        GameState state,
        BuildingState building,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng,
        bool aiAttacker,
        string? attackerLegionId = null)
    {
        var resolvedAttacker = attackerLegionId
            ?? (aiAttacker ? CampaignLegionIds.Ai : LegionRegistry.Local(state)?.legionId ?? CampaignLegionIds.Player);
        var defenderLegion = LegionQuery.OfBuilding(building);
        var systemId = building.solarSystemId;
        var e = new CombatQueueEntry
        {
            entryId = Guid.NewGuid().ToString("N"),
            combatSubtype = CombatSubtype.BUILDING_ASSAULT,
            battlefieldSystemId = systemId,
            targetBuildingId = building.buildingId,
            aiAttacker = aiAttacker,
            label = (aiAttacker ? "AI 建筑进攻 @ " : "建筑争夺战 @ ")
                + building.displayName
                + " (" + (BuildingService.Fragile.Equals(building.status, StringComparison.Ordinal) ? "脆弱" : "正常") + ")",
        };

        if (aiAttacker)
        {
            foreach (var m in CombatRosterBuilder.CollectBuildingDefenders(state, systemId, defenderLegion))
            {
                e.friendlyMemberIds.Add(m.memberId!);
            }
            foreach (var m in CombatRosterBuilder.CollectCombatants(state, systemId, rng, resolvedAttacker))
            {
                e.enemyRoster.Add(CombatRosterLineBuilder.FromMember(state, m, ships, modules));
            }
        }
        else
        {
            foreach (var m in CombatRosterBuilder.CollectCombatants(state, systemId, rng, resolvedAttacker))
            {
                e.friendlyMemberIds.Add(m.memberId!);
            }
            foreach (var m in CombatRosterBuilder.CollectBuildingDefenders(state, systemId, defenderLegion))
            {
                e.enemyRoster.Add(CombatRosterLineBuilder.FromMember(state, m, ships, modules));
            }
        }

        if (e.enemyRoster.Count == 0 || !e.enemyRoster.Any(l => l.canParticipate))
        {
            var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
            var maxHullId = maxHull?.hullId ?? "hull_bc_spear";
            var pwr = AssetValuation.HullStarCoinValue(maxHull);
            e.enemyRoster.Add(new CombatRosterLine
            {
                displayName = aiAttacker ? "敌方进攻编队" : "防守方（" + (building.displayName ?? building.buildingId) + "）",
                hullId = maxHullId,
                tonnageClass = maxHull?.tonnageClass ?? "BATTLECRUISER",
                combatPower = pwr,
            });
            CombatDefaultLoadout.ApplyDefaultAttackIfEmpty(e.enemyRoster[^1], maxHull, modules);
        }

        LegionQuery.TagCombatLegions(e, resolvedAttacker, defenderLegion);
        return e;
    }

    // liketoco0de345

    private static CombatQueueEntry TagOrdinal(CombatQueueEntry e, int ordinal, int total)
    {
        e.queueOrdinal = ordinal;
        e.queueTotal = total;
        return e;
    }

    // lik3tocoode345

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
