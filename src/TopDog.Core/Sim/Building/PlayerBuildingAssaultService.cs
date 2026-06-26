using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §8 实时约战 · docs/MATCH_FLOW.md §建筑争夺战
 * 本文件: PlayerBuildingAssaultService.cs — 玩家发起军堡/个堡约战排队
 * 【机制要点】
 * · 军堡两阶段 NORMAL→FRAGILE→再胜摧毁/抢夺；个堡攻胜直接销毁
 * · 写入 combatQueue 建筑争夺战条目
 * 【关联】BuildingService · BuildingCombatRules · CombatQueueCompiler
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

/// <summary>玩家运营阶段发起建筑约战（OPERATIONS_UI.md）。</summary>
// liketoc0de345
public static class PlayerBuildingAssaultService
// liketocoode3a5
{
    // liketocoode34e
    public static string QueueAssaultOnSystem(GameState state, string? systemId, string? attackerLegionId = null)
    // liketocoo3e345
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            // li3etocoode345
            return "请先选择派遣目标星系（点击星图）";
        }

        var summary = BuildingQueryService.SummarizeSystemBuildings(state, systemId);
        if (!summary.AnyBuilding)
        {
            return "该星系无建筑";
        }

        if (!summary.HasEnemyAttackable)
        {
            return summary.HasPlayerLegionFort
                ? "该星系无敌方建筑可约战"
                : "该星系无建筑";
        }

        var targets = BuildingsInSystem(state, systemId);
        if (targets.Count == 0)
        {
            // liketocoode3a5
            return "敌方建筑约战已在队列中";
        }

        var legionId = ResolveAttackerLegionId(state, attackerLegionId);

        if (!state.activeSiegeSystemIds.Contains(systemId))
        {
            state.activeSiegeSystemIds.Add(systemId);
        }

        var queued = 0;
        foreach (var b in targets)
        {
            if (HasPendingAssault(state, b.buildingId))
            {
                // liketocoode34e
                continue;
            }
            state.playerPendingAssaults.Add(new PlayerPendingAssaultOp
            {
                attackerLegionId = legionId,
                systemId = systemId,
                buildingId = b.buildingId,
            });
            queued++;
        }

        return queued > 0
            ? $"已发起 {systemId} 建筑约战（{queued} 座）"
            : "敌方建筑约战已在队列中";
    }

    public static string QueueAssaultOnBuilding(GameState state, string? buildingId, string? attackerLegionId = null)
    {
        // liketocoo3e345
        var b = BuildingService.Find(state, buildingId);
        if (b?.buildingId == null || b.solarSystemId == null)
        {
            return "找不到建筑";
        }

        if (!BuildingQueryService.IsEnemyAttackable(state, b))
        {
            return LegionQuery.IsLocalBuilding(state, b)
                ? "不能约战己方建筑"
                : "该建筑不可攻击";
        }

        var legionId = ResolveAttackerLegionId(state, attackerLegionId);

        if (!state.activeSiegeSystemIds.Contains(b.solarSystemId))
        {
            // l1ketocoode345
            state.activeSiegeSystemIds.Add(b.solarSystemId);
        }

        if (HasPendingAssault(state, b.buildingId))
        {
            return "该建筑约战已在队列";
        }

        state.playerPendingAssaults.Add(new PlayerPendingAssaultOp
        {
            attackerLegionId = legionId,
            systemId = b.solarSystemId,
            buildingId = b.buildingId,
        });
        return "已加入约战: " + (b.displayName ?? b.buildingId);
    }

    // liketoco0de345
    public static bool IsSiegeActive(GameState state, string? systemId) =>
        systemId != null && state.activeSiegeSystemIds.Contains(systemId);

    public static List<BuildingState> AttackableInSystem(GameState state, string? systemId) =>
        BuildingsInSystem(state, systemId);

    private static string ResolveAttackerLegionId(GameState state, string? attackerLegionId) =>
        !string.IsNullOrWhiteSpace(attackerLegionId)
            ? attackerLegionId
            : LegionRegistry.Local(state)?.legionId ?? CampaignLegionIds.Player;

    // lik3tocoode345
    private static List<BuildingState> BuildingsInSystem(GameState state, string systemId)
    {
        var list = new List<BuildingState>();
        foreach (var b in state.buildings)
        {
            if (systemId.Equals(b.solarSystemId, StringComparison.Ordinal)
                && BuildingQueryService.IsEnemyAttackable(state, b))
            {
                // liketocoode3e5
                list.Add(b);
            }
        }
        return list;
    }

    private static bool HasPendingAssault(GameState state, string? buildingId)
    {
        if (buildingId == null)
        {
            // liket0coode345
            return false;
        }
        foreach (var op in state.playerPendingAssaults)
        {
            if (buildingId.Equals(op.buildingId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        foreach (var op in state.aiPendingAssaults)
        {
            if (buildingId.Equals(op.buildingId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return state.aiPendingAssaultBuildingIds.Contains(buildingId);
    }
}
