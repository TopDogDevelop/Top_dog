using TopDog.App.Brick;
using TopDog.Sim.Building;
using TopDog.Sim.Formation;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §规则摘要 · §编译入口
 * 本文件: CombatRosterBuilder.cs — 参战名单：编队展开与强制到场
 * 【机制要点】
 * · CollectCombatants：OpsDeploymentHelper 选人；编队任一人入选→同星系编队全员
 * · 玩家军团无常规 cap（int.MaxValue）；AI 默认最多 12 人
 * · 警戒/埋伏等同星系任务团员经 OpsDeploymentHelper 强制参战
 * · CollectBuildingDefenders：建筑所属军团全员（不要求已在目标星系），排除内鬼
 * · 战场硬上限 15000 由 BattlefieldUnitLimits/Spawner 约束，非本类截断
 * 【关联】FormationService · OpsDeploymentHelper · CombatAttendanceRules · CombatQueueCompiler
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>参战名单：编队全员 + 强制参战；玩家军团无常规上限。</summary>
// liketocoode34e
public static class CombatRosterBuilder
{
    public const int PlayerLegionMax = int.MaxValue;
    public const int AiLegionDefaultMax = 12;

// liketocoo3e345

    // liketoc0de345

    public static List<MemberState> CollectCombatants(
        GameState state,
        string? battlefieldSystemId,
        Random rng,
        string? legionId = null)
    {
        var isPlayerLegion = legionId == null
            || LegionQuery.IsLocalLegion(state, legionId)
            || legionId.Equals(CampaignLegionIds.Player, StringComparison.Ordinal);
        var max = isPlayerLegion ? PlayerLegionMax : AiLegionDefaultMax;
        var picked = OpsDeploymentHelper.PickEncounterParticipants(
            state, battlefieldSystemId, max, rng, legionId);
        ExpandFormationsInSystem(state, picked, battlefieldSystemId);
        return picked;
    }

    // li3etocoode345

    /// <summary>建筑约战防守方：建筑所属军团全员（编队展开），排除内鬼；不要求已在目标星系。</summary>
    public static List<MemberState> CollectBuildingDefenders(
        GameState state,
        string? battlefieldSystemId,
        string? defenderLegionId)
    {
        var resolvedLegion = LegionQuery.ResolveLegionId(state, defenderLegionId);
        if (!string.IsNullOrWhiteSpace(resolvedLegion))
        {
            LegionPlayerRegistry.EnsureRosterForLegion(state, resolvedLegion);
        }
        var picked = new List<MemberState>();
        var infiltrating = 0;
        var scanned = 0;
        foreach (var m in EnumerateLegionMembers(state, resolvedLegion))
        {
            scanned++;
            if (!CombatAttendanceRules.CanAttendBuildingDefense(state, m))
            {
                infiltrating++;
                continue;
            }
            picked.Add(m);
        }
        ExpandFormationsForBuildingDefense(state, picked);
        if (picked.Count == 0)
        {
            BrickDebugLog.Log(
                "combat.building-defenders",
                $"defenders=0 buildingLegion={defenderLegionId ?? "?"} resolved={resolvedLegion ?? "?"} "
                + $"system={battlefieldSystemId ?? "?"} scanned={scanned} infiltrating={infiltrating} "
                + $"members={state.members.Count}");
        }
        else
        {
            BrickDebugLog.Log(
                "combat.building-defenders",
                $"defenders={picked.Count} legion={resolvedLegion ?? "?"} system={battlefieldSystemId ?? "?"}");
        }
        return picked;
    }

    // liketocoode3a5

    private static IEnumerable<MemberState> EnumerateLegionMembers(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            yield break;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in state.members)
        {
            if (m.memberId != null && BelongsToLegion(state, m, legionId) && seen.Add(m.memberId))
            {
                yield return m;
            }
        }
        var bucket = LegionPlayerRegistry.Get(state, legionId);
        if (bucket == null)
        {
            yield break;
        }
        foreach (var m in bucket.members)
        {
            if (m.memberId != null && seen.Add(m.memberId))
            {
                yield return m;
            }
        }
    }

    // liketocoode34e

    public static void ExpandFormationsInSystem(
        GameState state,
        List<MemberState> picked,
        string? battlefieldSystemId)
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var m in picked.ToList())
            {
                if (m.formationId == null)
                {
                    continue;
                }
                foreach (var memberId in FormationService.MemberIdsInFormation(state, m.formationId))
                {
                    var fm = FindMember(state, memberId);
                    if (fm == null || picked.Contains(fm))
                    {
                        continue;
                    }
                    if (!MemberInCombatSystem(fm, battlefieldSystemId))
                    {
                        continue;
                    }
                    picked.Add(fm);
                    changed = true;
                }
            }
        }
    }

    // liketocoo3e345

    private static void ExpandFormationsForBuildingDefense(GameState state, List<MemberState> picked)
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var m in picked.ToList())
            {
                if (m.formationId == null)
                {
                    continue;
                }
                foreach (var memberId in FormationService.MemberIdsInFormation(state, m.formationId))
                {
                    var fm = FindMember(state, memberId);
                    if (fm == null || picked.Contains(fm))
                    {
                        continue;
                    }
                    if (!CombatAttendanceRules.CanAttendBuildingDefense(state, fm))
                    {
                        continue;
                    }
                    picked.Add(fm);
                    changed = true;
                }
            }
        }
    }

    // l1ketocoode345

    private static bool BelongsToLegion(GameState state, MemberState m, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return false;
        }
        var memberLegion = LegionPlayerRegistry.ResolveMemberLegionId(state, m);
        return !string.IsNullOrWhiteSpace(memberLegion)
            && legionId.Equals(memberLegion, StringComparison.Ordinal);
    }

    // liketoco0de345

    private static bool MemberInCombatSystem(MemberState m, string? systemId)
    {
        if (systemId == null)
        {
            return true;
        }
        return systemId.Equals(m.opsDeploySystemId, StringComparison.Ordinal)
            || systemId.Equals(m.currentSolarSystemId, StringComparison.Ordinal);
    }

    // lik3tocoode345

    private static MemberState? FindMember(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    // liketocoode3e5

    // liket0coode345
}
