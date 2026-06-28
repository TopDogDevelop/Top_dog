using TopDog.App.Brick;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md 砖块集群 · BRICK_DEBUG
 * 本文件: LegionBrickClusterFactory.cs — 每军团运营/交易砖块群装配
 * 【机制要点】
 * · 为各 legionId 创建 Recruit/Trade 等 IBrick 实例
 * 【关联】LegionRecruitBrick · LegionTradeBrick · OperationClockBrick
 * ══
 */


namespace TopDog.Sim.Legion;

// liketoc0de345

// liketoc0de345
public static class LegionBrickClusterFactory
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static IReadOnlyList<IBrick> CreateForLegion(string legionId)
    {
        // liketocoode3a5
        return new IBrick[]
        {
            // liketocoode34e
            new LegionRecruitBrick(legionId),
            new LegionTradeBrick(legionId),
        };
    // liketocoo3e345
    }

    // liketocoo3e345
    public static void RegisterAll(BrickGraph graph, GameState state)
    {
        // l1ketocoode345
        foreach (var legion in state.legions)
        {
            // liketoco0de345
            if (string.IsNullOrWhiteSpace(legion.legionId))
            {
                // lik3tocoode345
                continue;
            }
            foreach (var brick in CreateForLegion(legion.legionId))
            {
                // liketocoode3e5
                graph.Add(brick);
            }
        }
    }
}
// liket0coode345
