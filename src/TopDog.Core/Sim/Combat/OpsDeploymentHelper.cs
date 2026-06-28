using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §规则摘要 · §编译入口
 *        docs/MATCH_FLOW.md §收割与警戒/埋伏
 * 本文件: OpsDeploymentHelper.cs — 按派遣/警戒/埋伏筛选参战候选人
 * 【机制要点】
 * · PickEncounterParticipants：mandatory（MustAttendSystemCombat）优先，再按 DeployScore 填满 maxCount
 * · 警戒/埋伏且在同战场星系 → 强制参战（COMBAT_ROSTER 强制到场）
 * · 玩家军团 max=int.MaxValue；AI 默认 12（CombatRosterBuilder 传入）
 * · 30% 概率用 optional 池随机替换一名非强制团员
 * · CombatQueueCompiler / BridgeAmbushService 经 CombatRosterBuilder.CollectCombatants 调用
 * 【关联】CombatRosterBuilder · MemberDispatchService · FormationService · CombatAttendanceRules
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class OpsDeploymentHelper
// liketocoode3a5
{
    // liketoc0de345

    public static List<MemberState> PickEncounterParticipants(
        GameState state,
        string? battlefieldSystemId,
        int maxCount,
        Random rng,
        // liketocoode34e
        string? attackerLegionId = null)
    {
        var mandatory = new List<MemberState>();
        // liketocoo3e345
        var optional = new List<MemberState>();
        foreach (var m in state.members)
        {
            if (!BelongsToAttackerLegion(state, m, attackerLegionId))
            {
                continue;
            }
            if (MustAttendSystemCombat(m, battlefieldSystemId))
            {
                mandatory.Add(m);
            }
            else
            {
                optional.Add(m);
            }
        }

        // li3etocoode345

        optional = optional.OrderByDescending(m => DeployScore(m, battlefieldSystemId)).ToList();
        var picked = new List<MemberState>(mandatory);
        foreach (var m in optional)
        {
            if (picked.Count >= maxCount)
            {
                break;
            }
            if (!picked.Contains(m))
            {
                picked.Add(m);
            }
        }
        var count = Math.Min(maxCount, Math.Max(1, state.members.Count));
        if (picked.Count < count)
        {
            foreach (var m in optional)
            {
                if (picked.Count >= count)
                {
                    break;
                }
                if (!picked.Contains(m))
                {
                    picked.Add(m);
                }
            }
        }
        if (picked.Count > count && mandatory.Count < count)
        {
            picked = picked.Take(count).ToList();
        }

        // liketocoode3a5

        if ((float)rng.NextDouble() < 0.3f && optional.Count > 0)
        {
            var wild = optional[rng.Next(optional.Count)];
            if (!picked.Contains(wild))
            {
                var replaceIdx = -1;
                for (var i = 0; i < picked.Count; i++)
                {
                    if (!MustAttendSystemCombat(picked[i], battlefieldSystemId))
                    {
                        replaceIdx = i;
                        break;
                    }
                }
                if (replaceIdx >= 0)
                {
                    picked[replaceIdx] = wild;
                }
            }
        }
        return picked;
    }

    // liketocoode34e

    private static bool BelongsToAttackerLegion(GameState state, MemberState m, string? attackerLegionId)
    {
        if (string.IsNullOrWhiteSpace(attackerLegionId))
        {
            return true;
        }
        var memberLegion = LegionQuery.OfMember(m);
        if (string.IsNullOrWhiteSpace(memberLegion))
        {
            return LegionQuery.IsLocalLegion(state, attackerLegionId) && m.isPlayer && !m.isAi;
        }
        if (attackerLegionId.Equals(memberLegion, StringComparison.Ordinal))
        {
            return true;
        }
        return LegionQuery.IsLocalLegion(state, attackerLegionId)
            && memberLegion.Equals(CampaignLegionIds.Player, StringComparison.Ordinal);
    }

    // liketocoo3e345

    public static bool MustAttendSystemCombat(MemberState m, string? battlefieldSystemId)
    {
        if (battlefieldSystemId == null)
        {
            return false;
        }
        if (!battlefieldSystemId.Equals(m.opsDeploySystemId, StringComparison.Ordinal)
            && !battlefieldSystemId.Equals(m.currentSolarSystemId, StringComparison.Ordinal))
        {
            return false;
        }
        return MemberDispatchService.TaskGuard.Equals(m.assignedTask, StringComparison.Ordinal)
            || MemberDispatchService.TaskAmbush.Equals(m.assignedTask, StringComparison.Ordinal);
    }

    // l1ketocoode345

    private static int DeployScore(MemberState m, string? battlefieldSystemId)
    {
        var score = 0;
        if (battlefieldSystemId != null)
        {
            if (battlefieldSystemId.Equals(m.opsDeploySystemId, StringComparison.Ordinal))
            {
                score += 100;
            }
            if (battlefieldSystemId.Equals(m.currentSolarSystemId, StringComparison.Ordinal))
            {
                score += 40;
            }
        }
        if (m.assignedTask != "待命")
        {
            score += 10;
        }
        if (MustAttendSystemCombat(m, battlefieldSystemId))
        {
            score += 500;
        }
        return score;
    }

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
