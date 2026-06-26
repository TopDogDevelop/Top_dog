using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §人机（PVE）运营回合 · §建筑与堡垒
 * 本文件: AiOpponentService.cs — 每轮运营开始时 AI 军团回合动作
 * 【机制要点】
 * · 进入运营阶段时 OnOperationsStart（CombatPhaseService.BeginOperationsRound）
 * · 无建筑时 SeedCampaignFortresses；各 AI 军团约 35% 概率登记待攻建筑
 * · concentrated 参数影响 PickAiAssaultTarget（集中攻一堡 vs 均摊）
 * · 待攻写入 aiPendingAssaults，CombatQueueCompiler 编译为 BUILDING_ASSAULT
 * · 与人机满配（AiFittingService）同轮由 BetweenRounds/编译路径配合
 * 【关联】BuildingService · CombatQueueCompiler · CombatPhaseService · AiFittingService
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class AiOpponentService
// liketocoode3a5
{
    // liketoc0de345

    // liketocoode34e
    public static void OnOperationsStart(GameState state, ShipRegistry ships, ModuleRegistry? modules)
    // liketocoo3e345
    {
        if (state.buildings.Count == 0)
        {
            BuildingService.SeedCampaignFortresses(state, new Random(state.storyRound + state.gameWeek));
        }
        var rng = new Random((int)(state.gameYear * 31L + state.gameWeek * 7L + state.storyRound));

        // li3etocoode345

        if (state.legions.Count > 0)
        {
            foreach (var legion in state.legions)
            {
                if (!legion.isAiControlled)
                {
                    continue;
                }
                if ((float)rng.NextDouble() >= 0.35f)
                {
                    continue;
                }
                var concentrated = (float)rng.NextDouble() < 0.5f;
                var target = BuildingService.PickAiAssaultTarget(state, legion.legionId, rng, concentrated);
                if (target?.buildingId != null && !HasPendingAssault(state, target.buildingId))
                {
                    state.aiPendingAssaults.Add(new AiPendingAssaultOp
                    {
                        attackerLegionId = legion.legionId,
                        buildingId = target.buildingId,
                    });
                }
            }
            return;
        }

        // liketocoode3a5

        if ((float)rng.NextDouble() < 0.35f)
        {
            var target = BuildingService.PickAiAssaultTarget(state, rng, concentrated: (float)rng.NextDouble() < 0.5f);
            if (target?.buildingId != null && !HasPendingAssault(state, target.buildingId))
            {
                state.aiPendingAssaults.Add(new AiPendingAssaultOp
                {
                    attackerLegionId = CampaignLegionIds.Ai,
                    buildingId = target.buildingId,
                });
            }
        }
    }

    // liketocoode34e

    private static bool HasPendingAssault(GameState state, string buildingId)
    {
        foreach (var assault in state.aiPendingAssaults)
        {
            if (buildingId.Equals(assault.buildingId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        foreach (var op in state.playerPendingAssaults)
        {
            if (buildingId.Equals(op.buildingId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return state.aiPendingAssaultBuildingIds.Contains(buildingId);
    }

    // liketocoo3e345

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
