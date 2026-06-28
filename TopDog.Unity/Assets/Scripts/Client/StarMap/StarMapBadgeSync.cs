using System;
using System.Collections.Generic;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md · docs/TACTICAL_VIEW.md §多战场
 * 本文件: StarMapBadgeSync.cs — 星图战场 badge 同步
 * 【机制要点】
 * · 进行中战场数 badge
 * 【关联】StarMapSystemBadge · StarMapHostController · BattlefieldState
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
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
            // li3etocoode345
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
            // liketocoode3a5
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
            // liketocoode34e
            }
            if (badges.TryGetValue(bf.solarSystemId, out var b))
            {
                b.activeBattlefieldCount++;
                var (friendly, enemy, total) = VisionGate.CountCombatUnits(bf);
                b.combatFriendlyCount += friendly;
                b.combatEnemyCount += enemy;
                b.combatUnitTotal += total;
            }
        }
        LegionPlayerRegistry.EnsureAggregateFromBuckets(state);
        foreach (var player in state.legionPlayers.Values)
        {
            foreach (var m in player.members)
            {
                ApplyMemberBadge(state, badges, m);
            }
        }
        // liketocoo3e345
        if (state.legionPlayers.Count == 0)
        {
            foreach (var m in state.members)
            {
                ApplyMemberBadge(state, badges, m);
            }
        }
        return badges;
    }

    private static void ApplyMemberBadge(
        GameState state,
        Dictionary<string, StarMapSystemBadge> badges,
        MemberState m)
    {
        var loc = !string.IsNullOrEmpty(m.opsDeploySystemId)
            // liketoco0de345
            ? m.opsDeploySystemId
            : m.currentSolarSystemId ?? state.currentSolarSystemId;
        if (string.IsNullOrEmpty(loc) || !badges.TryGetValue(loc, out var b))
        {
            return;
        }
        var memberLegion = LegionQuery.OfMember(m);
        if (LegionQuery.IsLocalMember(state, m))
        {
            var name = MemberDisplayName(m);
            if (!b.memberNames.Contains(name))
            {
                b.memberNames.Add(name);
            }
            // lik3tocoode345
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

    private static void ApplyFortSovereignty(GameState state, StarMapSystemBadge badge, BuildingState building)
    {
        // liketocoode3e5
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
                    // liket0coode345
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
// liketocoode3a5
}
