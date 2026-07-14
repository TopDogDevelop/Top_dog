using System.Collections.Generic;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md · docs/MATCH_FLOW.md
 * 本文件: ICombatAttendancePolicy.cs — 管理代码块/词条影响上场的预留钩子
 * 【机制要点】
 * · ForceExclude > 玩家排除 > ForceInclude > 默认全员
 * · 本轮仅骨架；具体管理词条日后注册
 * 【关联】CombatAttendanceRules · CombatRosterRefresh
 * ══
 */

namespace TopDog.Sim.Combat;

public enum CombatAttendanceDecision
{
    Unchanged,
    ForceInclude,
    ForceExclude,
}

public interface ICombatAttendancePolicy
{
    CombatAttendanceDecision Evaluate(GameState state, CombatQueueEntry entry, string memberId);
}

/// <summary>上场策略注册表；默认空列表。</summary>
public static class CombatAttendancePolicies
{
    private static readonly List<ICombatAttendancePolicy> Policies = new();

    public static void Register(ICombatAttendancePolicy policy)
    {
        if (policy != null && !Policies.Contains(policy))
        {
            Policies.Add(policy);
        }
    }

    public static void Clear() => Policies.Clear();

    public static CombatAttendanceDecision EvaluateAll(
        GameState state,
        CombatQueueEntry entry,
        string memberId)
    {
        var forceExclude = false;
        var forceInclude = false;
        foreach (var p in Policies)
        {
            var d = p.Evaluate(state, entry, memberId);
            if (d == CombatAttendanceDecision.ForceExclude)
            {
                forceExclude = true;
            }
            else if (d == CombatAttendanceDecision.ForceInclude)
            {
                forceInclude = true;
            }
        }
        if (forceExclude)
        {
            return CombatAttendanceDecision.ForceExclude;
        }
        if (forceInclude)
        {
            return CombatAttendanceDecision.ForceInclude;
        }
        return CombatAttendanceDecision.Unchanged;
    }

    /// <summary>
    /// 综合 policy + 玩家排除：是否应排除上场。
    /// ForceExclude &gt; 玩家排除 &gt; ForceInclude。
    /// </summary>
    public static bool ShouldExcludeFromFight(
        GameState state,
        CombatQueueEntry entry,
        string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return true;
        }
        var decision = EvaluateAll(state, entry, memberId);
        if (decision == CombatAttendanceDecision.ForceExclude)
        {
            return true;
        }
        if (decision == CombatAttendanceDecision.ForceInclude)
        {
            return false;
        }
        return entry.excludedMemberIds.Contains(memberId);
    }
}
