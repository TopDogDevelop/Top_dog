using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

/// <summary>反收割触发概率：基础 30%，每名警戒 −10%、每名埋伏 +10%，限制 5%～95%。</summary>
public static class CounterHarvestOddsService
{
    public const int BasePercent = 30;
    public const int GuardModifierPercent = -10;
    public const int AmbushModifierPercent = 10;
    public const int MinPercent = 5;
    public const int MaxPercent = 95;

    public static int ComputePercent(GameState state, string? systemId)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            return BasePercent;
        }
        var guards = CountTaskInSystem(state, systemId, MemberDispatchService.TaskGuard);
        var ambushes = CountTaskInSystem(state, systemId, MemberDispatchService.TaskAmbush);
        var pct = BasePercent + guards * GuardModifierPercent + ambushes * AmbushModifierPercent;
        return Math.Clamp(pct, MinPercent, MaxPercent);
    }

    public static bool RollTrigger(GameState state, string? systemId, Random rng)
    {
        var pct = ComputePercent(state, systemId);
        return rng.Next(100) < pct;
    }

    private static int CountTaskInSystem(GameState state, string systemId, string task)
    {
        var n = 0;
        foreach (var m in state.members)
        {
            if (task.Equals(m.assignedTask, StringComparison.Ordinal)
                && systemId.Equals(m.opsDeploySystemId ?? m.currentSolarSystemId, StringComparison.Ordinal))
            {
                n++;
            }
        }
        return n;
    }
}
