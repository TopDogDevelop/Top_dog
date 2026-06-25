using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

public static class CombatRosterRefresh
{
    public static void RefreshCurrent(GameState state, ShipRegistry ships, ModuleRegistry? modules = null)
    {
        var entry = CombatPhaseService.CurrentEntry(state);
        if (entry != null)
        {
            RefreshFriendly(state, entry, ships, modules);
        }
    }

    public static void RefreshFriendly(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules = null)
    {
        if (entry.combatSubtype != CombatSubtype.COUNTER_HARVEST)
        {
            BuildDefaultFriendlyRoster(state, entry, ships, modules);
            return;
        }
        entry.friendlyRosterLines.Clear();
        foreach (var memberId in entry.friendlyMemberIds)
        {
            var m = FindMember(state, memberId);
            if (m == null)
            {
                continue;
            }
            var arrival = entry.arrivalSecByMember.GetValueOrDefault(memberId, -1);
            var mandatory = entry.mandatoryAttendeeByMember.GetValueOrDefault(memberId, false);
            var captured = entry.capturedTargetByMember.GetValueOrDefault(memberId, false);
            var hasShip = !string.IsNullOrEmpty(m.equippedHullId);
            var hullId = hasShip ? m.equippedHullId : "(无舰)";
            var tonnage = "(无)";
            if (hasShip)
            {
                var h = ships.FindHull(m.equippedHullId);
                if (h?.tonnageClass != null)
                {
                    tonnage = h.tonnageClass;
                }
            }
            entry.friendlyRosterLines.Add(new CombatRosterLine
            {
                memberId = memberId,
                displayName = DisplayName(m),
                hullId = hullId,
                tonnageClass = tonnage,
                combatPower = hasShip ? AutoCombatValuation.MemberValue(state, m, ships, modules) : 0f,
                arrivalSec = arrival,
                mandatoryAttendee = mandatory,
                capturedTarget = captured,
                canParticipate = hasShip,
            });
        }
    }

    private static void BuildDefaultFriendlyRoster(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules)
    {
        entry.friendlyRosterLines.Clear();
        foreach (var memberId in entry.friendlyMemberIds)
        {
            var m = FindMember(state, memberId);
            if (m == null)
            {
                continue;
            }
            var hasShip = !string.IsNullOrEmpty(m.equippedHullId);
            var hull = hasShip ? ships.FindHull(m.equippedHullId) : null;
            entry.friendlyRosterLines.Add(new CombatRosterLine
            {
                memberId = memberId,
                displayName = DisplayName(m),
                hullId = hasShip ? m.equippedHullId : "(无舰)",
                tonnageClass = hull?.tonnageClass ?? "(无)",
                combatPower = hasShip ? AutoCombatValuation.MemberValue(state, m, ships, modules) : 0f,
                canParticipate = hasShip,
            });
        }
    }

    private static MemberState? FindMember(GameState state, string id)
    {
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static string DisplayName(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "?";
}
