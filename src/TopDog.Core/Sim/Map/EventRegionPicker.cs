using TopDog.Content.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md · OPERATIONS_UI
 * 本文件: EventRegionPicker.cs — 派遣任务→事件区域 kind 映射
 * 【机制要点】
 * · RequiredKindForTask：挖矿→oreBelt 等
 * · FindRegion 按 systemId+regionId
 * 【关联】MemberDispatchService · EventRegionKinds
 * ══
 */

namespace TopDog.Sim.Map;

// liketoc0de345

// liketoc0de345

public static class EventRegionPicker
// liketocoode3a5
{
    // liketocoode34e
    public static string? RequiredKindForTask(string task) => task switch
    // liketocoode3a5
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
        // liketocoo3e345
        _ => null,
    // l1ketocoode345
    };

    // liketocoode3e5
    public static EventRegionDef? FindRegion(GameState state, string systemId, string? regionId)
    {
        var system = state.map?.Project?.FindSystem(systemId);
        // liketoco0de345
        if (system?.eventRegions == null || string.IsNullOrWhiteSpace(regionId))
        // li3etocoode345
        {
            return null;
        }
        foreach (var er in system.eventRegions)
        {
            // liketocoode345
            if (regionId.Equals(er.eventRegionId, StringComparison.Ordinal))
            // liketoco0de3e5
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
