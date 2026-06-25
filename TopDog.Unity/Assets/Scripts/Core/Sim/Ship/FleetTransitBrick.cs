namespace TopDog.Sim.Ship;

using TopDog.App.Brick;
using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.State;

public sealed class FleetTransitBrick : IBrick
{
    private const float BaseTransitSec = 8f;

    public string Id() => "ship.fleet_transit";

    public void Tick(BrickContext ctx, float dtSec)
    {
        foreach (var f in ctx.State.fleets)
        {
            if (!f.inTransit)
            {
                continue;
            }
            f.transitRemainingSec -= dtSec;
            if (f.transitRemainingSec <= 0f)
            {
                f.inTransit = false;
                f.transitRemainingSec = 0f;
                if (f.transitTargetSystemId != null)
                {
                    f.solarSystemId = f.transitTargetSystemId;
                    var leader = FindMember(ctx, f.leaderMemberId);
                    if (leader != null)
                    {
                        leader.currentSolarSystemId = f.transitTargetSystemId;
                    }
                }
                f.transitTargetSystemId = null;
            }
        }
    }

    public bool RequestTransit(BrickContext ctx, MemberState member, string targetSystemId)
    {
        if (member?.equippedHullId == null || ctx.State.map == null)
        {
            return false;
        }
        if (ctx.Ships.FindHull(member.equippedHullId) == null)
        {
            return false;
        }
        var from = member.currentSolarSystemId;
        if (from == null || from.Equals(targetSystemId, StringComparison.Ordinal))
        {
            return false;
        }
        if (!HasBridge(ctx, from, targetSystemId))
        {
            return false;
        }
        var fleet = FindOrCreateFleet(ctx, member.memberId!);
        fleet.leaderMemberId = member.memberId;
        fleet.solarSystemId = from;
        fleet.inTransit = true;
        fleet.transitTargetSystemId = targetSystemId;
        fleet.transitRemainingSec = BaseTransitSec;
        return true;
    }

    private static bool HasBridge(BrickContext ctx, string a, string b)
    {
        foreach (var jb in ctx.State.map!.Project.bridges)
        {
            if ((jb.fromSystemId == a && jb.toSystemId == b)
                || (jb.fromSystemId == b && jb.toSystemId == a))
            {
                return true;
            }
        }
        return false;
    }

    private static FleetState FindOrCreateFleet(BrickContext ctx, string memberId)
    {
        foreach (var f in ctx.State.fleets)
        {
            if (memberId.Equals(f.leaderMemberId, StringComparison.Ordinal))
            {
                return f;
            }
        }
        var fleet = new FleetState
        {
            fleetId = "fleet_" + memberId.ToLowerInvariant(),
            leaderMemberId = memberId,
        };
        ctx.State.fleets.Add(fleet);
        return fleet;
    }

    private static MemberState? FindMember(BrickContext ctx, string? id)
    {
        if (id == null)
        {
            return null;
        }
        foreach (var m in ctx.State.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    public SolarSystemDef? FindSystemByName(BrickContext ctx, string? name)
    {
        if (ctx.State.map == null || name == null)
        {
            return null;
        }
        var needle = name.Trim().ToLowerInvariant();
        foreach (var s in ctx.State.map.Project.systems)
        {
            if (s.name != null && s.name.ToLowerInvariant() == needle)
            {
                return s;
            }
            if (s.solarSystemId != null && s.solarSystemId.ToLowerInvariant() == needle)
            {
                return s;
            }
        }
        return null;
    }
}
