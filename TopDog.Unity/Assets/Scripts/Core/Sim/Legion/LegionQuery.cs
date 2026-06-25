using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Legion;

public static class LegionQuery
{
    public static string? OfMember(MemberState? member)
    {
        if (member == null)
        {
            return null;
        }
        if (!string.IsNullOrWhiteSpace(member.legionId))
        {
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

    public static bool IsLocalLegion(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return false;
        }
        var localId = LegionRegistry.Local(state)?.legionId;
        if (localId != null && localId.Equals(legionId, StringComparison.Ordinal))
        {
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
