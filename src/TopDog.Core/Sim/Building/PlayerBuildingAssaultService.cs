using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

/// <summary>玩家运营阶段发起建筑约战（OPERATIONS_UI.md）。</summary>
public static class PlayerBuildingAssaultService
{
    public static string QueueAssaultOnSystem(GameState state, string? systemId, string? attackerLegionId = null)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
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
            return "敌方建筑约战已在队列中";
        }

        var legionId = attackerLegionId ?? CampaignLegionIds.Player;

        if (!state.activeSiegeSystemIds.Contains(systemId))
        {
            state.activeSiegeSystemIds.Add(systemId);
        }

        var queued = 0;
        foreach (var b in targets)
        {
            if (HasPendingAssault(state, b.buildingId))
            {
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
        var b = BuildingService.Find(state, buildingId);
        if (b?.buildingId == null || b.solarSystemId == null)
        {
            return "找不到建筑";
        }

        if (!IsAttackable(b))
        {
            return "该建筑不可攻击";
        }

        var legionId = attackerLegionId ?? CampaignLegionIds.Player;

        if (!state.activeSiegeSystemIds.Contains(b.solarSystemId))
        {
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

    public static bool IsSiegeActive(GameState state, string? systemId) =>
        systemId != null && state.activeSiegeSystemIds.Contains(systemId);

    public static List<BuildingState> AttackableInSystem(GameState state, string? systemId) =>
        BuildingsInSystem(state, systemId);

    private static List<BuildingState> BuildingsInSystem(GameState state, string systemId)
    {
        var list = new List<BuildingState>();
        foreach (var b in state.buildings)
        {
            if (systemId.Equals(b.solarSystemId, StringComparison.Ordinal) && IsAttackable(b))
            {
                list.Add(b);
            }
        }
        return list;
    }

    private static bool IsAttackable(BuildingState b) =>
        !b.playerOwned
        && (string.Equals(b.status, BuildingService.Normal, StringComparison.Ordinal)
            || string.Equals(b.status, BuildingService.Fragile, StringComparison.Ordinal));

    private static bool HasPendingAssault(GameState state, string? buildingId)
    {
        if (buildingId == null)
        {
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
