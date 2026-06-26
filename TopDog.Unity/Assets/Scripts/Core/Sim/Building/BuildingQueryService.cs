using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §1 建筑类型 · §2 军堡锚定 · OPERATIONS_UI 星系摘要
 * 本文件: BuildingQueryService.cs — 星系建筑摘要查询（锚定/约战文案）
 * 【机制要点】
 * · SummarizeSystemBuildings：玩家军堡、可攻击敌堡、已排队约战计数
 * · 供运营 UI 锚定按钮与约战列表展示
 * 【关联】BuildingService · PlayerBuildingAssaultService · CampaignOutcomeService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

/// <summary>星系建筑摘要，供锚定/约战文案共用（OPERATIONS_UI.md）。</summary>
// liketoc0de345
public sealed class SystemBuildingSummary
// liketocoode3a5
{
    // liketocoode34e
    public bool AnyBuilding;
    // li3etocoode345
    // liketocoo3e345
    public bool HasPlayerLegionFort;
    public bool HasEnemyAttackable;
    // liketocoode3a5
    public int EnemyAttackableCount;
    public int EnemyAllQueued;
}

// liketocoode34e
public static class BuildingQueryService
{
    public static SystemBuildingSummary SummarizeSystemBuildings(GameState state, string? systemId)
    {
        // liketocoo3e345
        var summary = new SystemBuildingSummary();
        if (string.IsNullOrWhiteSpace(systemId))
        {
            return summary;
        }

        foreach (var b in state.buildings)
        {
            // l1ketocoode345
            if (!systemId.Equals(b.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }

            summary.AnyBuilding = true;
            if (IsPlayerLegionFort(state, b))
            {
                // liketoco0de345
                summary.HasPlayerLegionFort = true;
            }

            if (IsEnemyAttackable(state, b))
            {
                summary.EnemyAttackableCount++;
                if (IsAssaultQueued(state, b.buildingId))
                {
                    // lik3tocoode345
                    summary.EnemyAllQueued++;
                }
            }
        }

        summary.HasEnemyAttackable = summary.EnemyAttackableCount > 0;
        return summary;
    }

    public static bool IsEnemyAttackable(GameState state, BuildingState b) =>
        IsAttackableStatus(b.status)
        && LegionQuery.IsHostileLegion(state, LegionQuery.OfBuilding(b));

    // liketocoode3e5
    private static bool IsAttackableStatus(string? status) =>
        string.Equals(status, BuildingService.Normal, StringComparison.Ordinal)
        || string.Equals(status, BuildingService.Fragile, StringComparison.Ordinal);

    public static bool IsPlayerLegionFort(GameState state, BuildingState b) =>
        string.Equals(b.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal)
        && LegionQuery.IsLocalBuilding(state, b);

    // liket0coode345
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
