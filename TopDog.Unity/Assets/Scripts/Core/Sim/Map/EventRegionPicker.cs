using TopDog.Content.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Map;

public static class EventRegionPicker
{
    public static string? RequiredKindForTask(string task) => task switch
    {
        var t when MemberDispatchService.TaskMining.Equals(t, StringComparison.Ordinal) || "挖矿".Equals(t, StringComparison.Ordinal)
            => EventRegionKinds.OreBelt,
        var t when MemberDispatchService.TaskBounty.Equals(t, StringComparison.Ordinal) || "刷赏".Equals(t, StringComparison.Ordinal)
            => EventRegionKinds.PirateRally,
        var t when MemberDispatchService.TaskGuard.Equals(t, StringComparison.Ordinal) || "警戒".Equals(t, StringComparison.Ordinal)
            => EventRegionKinds.JumpBridge,
        var t when MemberDispatchService.TaskAmbush.Equals(t, StringComparison.Ordinal) || "埋伏".Equals(t, StringComparison.Ordinal)
            => EventRegionKinds.JumpBridge,
        var t when MemberDispatchService.TaskAnchor.Equals(t, StringComparison.Ordinal) || "锚定".Equals(t, StringComparison.Ordinal)
            => EventRegionKinds.Planet,
        _ => null,
    };

    public static EventRegionDef? FindRegion(GameState state, string systemId, string? regionId)
    {
        var system = state.map?.Project?.FindSystem(systemId);
        if (system?.eventRegions == null || string.IsNullOrWhiteSpace(regionId))
        {
            return null;
        }
        foreach (var er in system.eventRegions)
        {
            if (regionId.Equals(er.eventRegionId, StringComparison.Ordinal))
            {
                return er;
            }
        }
        return null;
    }

    public static EventRegionDef? PickRandomOfKind(GameState state, string systemId, string kind, Random? rng = null)
    {
        var system = state.map?.Project?.FindSystem(systemId);
        if (system?.eventRegions == null)
        {
            return null;
        }
        var list = new List<EventRegionDef>();
        foreach (var er in system.eventRegions)
        {
            if (kind.Equals(er.kind, StringComparison.Ordinal))
            {
                list.Add(er);
            }
        }
        if (list.Count == 0)
        {
            return null;
        }
        var r = rng ?? new Random();
        return list[r.Next(list.Count)];
    }

    public static List<EventRegionDef> ListOfKind(GameState state, string systemId, string kind)
    {
        var list = new List<EventRegionDef>();
        var system = state.map?.Project?.FindSystem(systemId);
        if (system?.eventRegions == null)
        {
            return list;
        }
        foreach (var er in system.eventRegions)
        {
            if (kind.Equals(er.kind, StringComparison.Ordinal))
            {
                list.Add(er);
            }
        }
        return list;
    }
}
