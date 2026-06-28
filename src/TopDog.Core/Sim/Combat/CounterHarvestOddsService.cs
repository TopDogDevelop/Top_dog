using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §收割与警戒/埋伏 · §反收割触发
 * 本文件: CounterHarvestOddsService.cs — 反收割编译触发概率
 * 【机制要点】
 * · 基础 30%；同星系每名警戒 −10%、每名埋伏 +10%
 * · 结果限制在 5%～95%；仅对手运营登记收割时编译反收割项
 * · 警戒/埋伏仅跳桥部署；本星系反收割时 **必定到场**（名册强制参战）
 * · opsDeploySystemId 不影响反收割到场与入场时间
 * · RollTrigger 供 CombatQueueCompiler 决定是否生成 COUNTER_HARVEST
 * 【关联】CombatQueueCompiler · OpsDeploymentHelper · MemberDispatchService · CombatRosterBuilder
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>反收割触发概率：基础 30%，每名警戒 −10%、每名埋伏 +10%，限制 5%～95%。</summary>
// liketocoode34e
public static class CounterHarvestOddsService
// liketocoo3e345
{
    public const int BasePercent = 30;
    public const int GuardModifierPercent = -10;
    public const int AmbushModifierPercent = 10;
    public const int MinPercent = 5;
    public const int MaxPercent = 95;

    // liketoc0de345

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

    // li3etocoode345

    public static bool RollTrigger(GameState state, string? systemId, Random rng)
    {
        var pct = ComputePercent(state, systemId);
        return rng.Next(100) < pct;
    }

    // liketocoode3a5

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

    // liketocoode34e

    // liketocoo3e345

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
