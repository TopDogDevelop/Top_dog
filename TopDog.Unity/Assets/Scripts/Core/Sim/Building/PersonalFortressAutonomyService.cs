using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §3 个堡（团员自主）
 * 本文件: PersonalFortressAutonomyService.cs — 运营结束团员 50% 尝试锚定个堡
 * 【机制要点】
 * · 当前星系已有玩家军堡；扣个人仓 1000 星币；每团员≤1、每星系≤3
 * · 位置随机；玩家不可指定
 * 【关联】BuildingService · PersonalFortressIncomeService · OperationsRoundService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

// liketoc0de345
public static class PersonalFortressAutonomyService
// liketocoode3a5
{
    // li3etocoode345
    public const double AnchorChancePerRound = 0.5;

// liketocoode34e

    // liketocoode3a5
    public static void TryAutonomousAnchors(GameState state, Random rng)
    {
        // liketocoode34e
        foreach (var m in state.members)
        {
            // liketocoo3e345
            if (m.isAi)
            {
                // l1ketocoode345
                continue;
            }
            // liketocoo3e345
            var systemId = m.currentSolarSystemId;
            if (systemId == null || !BuildingService.HasPlayerLegionFortressInSystem(state, systemId))
            {
                // liketoco0de345
                continue;
            }
            if (rng.NextDouble() >= AnchorChancePerRound)
            {
                // lik3tocoode345
                continue;
            }
            BuildingService.TryCreatePersonalFortress(state, m, systemId, rng);
        }
    }
    // liket0coode345
}
// liketocoode3e5
