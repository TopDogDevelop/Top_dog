using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

/// <summary>星系建筑摘要，供锚定/约战文案共用（OPERATIONS_UI.md）。</summary>
public sealed class SystemBuildingSummary
{
    public bool AnyBuilding;
    public bool HasPlayerLegionFort;
    public bool HasEnemyAttackable;
    public int EnemyAttackableCount;
    public int EnemyAllQueued;
}

public static class BuildingQueryService
{
    public static SystemBuildingSummary SummarizeSystemBuildings(GameState state, string? systemId)
    {
        var summary = new SystemBuildingSummary();
        if (string.IsNullOrWhiteSpace(systemId))
        {
            return summary;
        }

        foreach (var b in state.buildings)
        {
            if (!systemId.Equals(b.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }

            summary.AnyBuilding = true;
            if (IsPlayerLegionFort(state, b))
            {
                summary.HasPlayerLegionFort = true;
            }

            if (IsEnemyAttackable(b))
            {
                summary.EnemyAttackableCount++;
                if (IsAssaultQueued(state, b.buildingId))
                {
                    summary.EnemyAllQueued++;
                }
            }
        }

        summary.HasEnemyAttackable = summary.EnemyAttackableCount > 0;
        return summary;
    }

    public static bool IsPlayerLegionFort(GameState state, BuildingState b) =>
        string.Equals(b.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal)
        && LegionQuery.IsLocalBuilding(state, b);

    public static bool IsEnemyAttackable(BuildingState b) =>
        !b.playerOwned
        && (string.Equals(b.status, BuildingService.Normal, StringComparison.Ordinal)
            || string.Equals(b.status, BuildingService.Fragile, StringComparison.Ordinal));

    private static bool IsAssaultQueued(GameState state, string? buildingId)
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
