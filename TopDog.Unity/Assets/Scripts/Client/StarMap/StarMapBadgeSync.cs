using System;
using System.Collections.Generic;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Client.StarMap;

internal static class StarMapBadgeSync
{
    public static Dictionary<string, StarMapSystemBadge> Build(GameState state)
    {
        var badges = new Dictionary<string, StarMapSystemBadge>();
        if (state.map?.Project.systems == null)
        {
            return badges;
        }
        foreach (var sys in state.map.Project.systems)
        {
            if (sys.solarSystemId == null)
            {
                continue;
            }
            badges[sys.solarSystemId] = new StarMapSystemBadge
            {
                systemId = sys.solarSystemId,
                displayName = !string.IsNullOrEmpty(sys.name) ? sys.name : sys.solarSystemId,
            };
        }
        foreach (var building in state.buildings)
        {
            if (string.IsNullOrEmpty(building.solarSystemId))
            {
                continue;
            }
            if (!badges.TryGetValue(building.solarSystemId, out var b))
            {
                continue;
            }
            if (LegionQuery.IsLocalBuilding(state, building))
            {
                b.playerBuildingCount++;
            }
            ApplyFortSovereignty(state, b, building);
        }
        foreach (var bf in state.battlefields)
        {
            if (string.IsNullOrEmpty(bf.solarSystemId))
            {
                continue;
            }
            if (badges.TryGetValue(bf.solarSystemId, out var b))
            {
                b.activeBattlefieldCount++;
            }
        }
        foreach (var m in state.members)
        {
            var loc = !string.IsNullOrEmpty(m.opsDeploySystemId)
                ? m.opsDeploySystemId
                : m.currentSolarSystemId ?? state.currentSolarSystemId;
            if (string.IsNullOrEmpty(loc) || !badges.TryGetValue(loc, out var b))
            {
                continue;
            }
            var memberLegion = LegionQuery.OfMember(m);
            if (LegionQuery.IsLocalMember(state, m))
            {
                var name = MemberDisplayName(m);
                if (!b.memberNames.Contains(name))
                {
                    b.memberNames.Add(name);
                }
                var onTask = !string.IsNullOrEmpty(m.assignedTask) && m.assignedTask != "待命";
                if (onTask)
                {
                    b.taskMemberCount++;
                }
                b.playerPresence = true;
            }
            else if (LegionQuery.IsHostileLegion(state, memberLegion))
            {
                b.hostilePresence = true;
            }
        }
        return badges;
    }

    private static void ApplyFortSovereignty(GameState state, StarMapSystemBadge badge, BuildingState building)
    {
        var next = ClassifyFort(state, building);
        if (next > badge.fortSovereignty)
        {
            badge.fortSovereignty = next;
        }
    }

    private static FortSovereignty ClassifyFort(GameState state, BuildingState building)
    {
        if (string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
        {
            if (LegionQuery.IsLocalBuilding(state, building))
            {
                return string.IsNullOrWhiteSpace(building.eventRegionId)
                    ? FortSovereignty.FriendlyUnanchored
                    : FortSovereignty.FriendlyAnchored;
            }
            return FortSovereignty.Enemy;
        }

        if (!building.playerOwned && LegionQuery.IsHostileLegion(state, LegionQuery.OfBuilding(building)))
        {
            return FortSovereignty.Enemy;
        }

        return FortSovereignty.None;
    }

    private static string MemberDisplayName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "团员";
}
