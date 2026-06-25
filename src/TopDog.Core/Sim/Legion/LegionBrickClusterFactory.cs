using TopDog.App.Brick;
using TopDog.Sim.State;

namespace TopDog.Sim.Legion;

public static class LegionBrickClusterFactory
{
    public static IReadOnlyList<IBrick> CreateForLegion(string legionId)
    {
        return new IBrick[]
        {
            new LegionRecruitBrick(legionId),
            new LegionTradeBrick(legionId),
        };
    }

    public static void RegisterAll(BrickGraph graph, GameState state)
    {
        foreach (var legion in state.legions)
        {
            if (string.IsNullOrWhiteSpace(legion.legionId))
            {
                continue;
            }
            foreach (var brick in CreateForLegion(legion.legionId))
            {
                graph.Add(brick);
            }
        }
    }
}
