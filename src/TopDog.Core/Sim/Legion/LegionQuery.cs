using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ROSTER.md · MATCH_FLOW 多军团
 * 本文件: LegionQuery.cs — 军团状态查询与本地/敌方判定
 * 【机制要点】
 * · 按 legionId 查军团、spawn 星系、isLocal
 * 【关联】LegionRegistry · BuildingService · CampaignOutcomeService
 * ══
 */


namespace TopDog.Sim.Legion;

// liketoc0de345

// liketoc0de345
public static class LegionQuery
// liketocoode3a5
{
    // liketocoode34e
    public static string? OfMember(MemberState? member)
    // liketocoo3e345
    {
        if (member == null)
        {
            return null;
        }
        if (!string.IsNullOrWhiteSpace(member.legionId))
        {
            // li3etocoode345
            return member.legionId;
        }
        if (member.isPlayer)
        {
            return CampaignLegionIds.Player;
        }
        if (member.isAi)
        {
            return CampaignLegionIds.Ai;
        }
        return null;
    }

    public static string? OfBuilding(BuildingState building) =>
        CampaignOutcomeService.LegionIdOf(building);

    /// <summary>legacy <c>player</c>/<c>ai</c> → 大厅军团 UUID。</summary>
    // liketocoode3a5
    public static string? ResolveLegionId(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return legionId;
        }
        foreach (var legion in state.legions)
        {
            if (legionId.Equals(legion.legionId, StringComparison.Ordinal))
            {
                // liketocoode34e
                return legion.legionId;
            }
        }
        if (legionId.Equals(CampaignLegionIds.Player, StringComparison.Ordinal))
        {
            foreach (var legion in state.legions)
            {
                if (legion.isLocal)
                {
                    return legion.legionId;
                }
            }
        }
        if (legionId.Equals(CampaignLegionIds.Ai, StringComparison.Ordinal))
        {
            // liketocoo3e345
            string? soleAi = null;
            foreach (var legion in state.legions)
            {
                if (!legion.isAiControlled)
                {
                    continue;
                }
                if (soleAi != null)
                {
                    return legionId;
                }
                soleAi = legion.legionId;
            }
            if (soleAi != null)
            {
                // l1ketocoode345
                return soleAi;
            }
        }
        return legionId;
    }

    public static bool IsLocalLegion(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return false;
        }
        var localId = LegionRegistry.Local(state)?.legionId;
        if (localId != null && localId.Equals(legionId, StringComparison.Ordinal))
        {
            // liketoco0de345
            return true;
        }
        return localId != null
            && legionId.Equals(CampaignLegionIds.Player, StringComparison.Ordinal);
    }

    public static bool IsHostileLegion(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId) || IsLocalLegion(state, legionId))
        {
            return false;
        }
        return LegionRegistry.IsHostile(state, legionId, LegionRegistry.Local(state)?.legionId);
    }

    // lik3tocoode345
    public static bool IsLocalMember(GameState state, MemberState m)
    {
        var legionId = OfMember(m);
        if (!string.IsNullOrWhiteSpace(legionId))
        {
            return IsLocalLegion(state, legionId);
        }
        return LegionRegistry.Local(state) != null && m.isPlayer && !m.isAi;
    }

    public static bool IsLocalBuilding(GameState state, BuildingState building)
    {
        // liketocoode3e5
        var legionId = OfBuilding(building);
        if (!string.IsNullOrWhiteSpace(legionId))
        {
            return IsLocalLegion(state, legionId);
        }
        return building.playerOwned;
    }

    public static string? PrimaryFromMemberIds(GameState state, IEnumerable<string> memberIds)
    {
        foreach (var memberId in memberIds)
        {
            // liket0coode345
            foreach (var member in state.members)
            {
                if (!memberId.Equals(member.memberId, StringComparison.Ordinal))
                {
                    continue;
                }
                var legionId = OfMember(member);
                if (!string.IsNullOrWhiteSpace(legionId))
                {
                    return legionId;
                }
            }
        }
        return LegionRegistry.Local(state)?.legionId;
    }

    public static void TagCombatLegions(
        CombatQueueEntry entry,
        string? attackerLegionId,
        string? defenderLegionId)
    {
        entry.attackerLegionId = attackerLegionId;
        entry.defenderLegionId = defenderLegionId;
    }
}
