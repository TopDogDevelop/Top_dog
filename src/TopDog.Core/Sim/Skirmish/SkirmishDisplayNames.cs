using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

/// <summary>约战星系地点/建筑显示名：按本机军团标注「己方 / 敌方」前缀。</summary>
public static class SkirmishDisplayNames
{
    public static string SidePrefix(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return "";
        }

        foreach (var legion in state.legions)
        {
            if (legion.isLocal && legionId.Equals(legion.legionId, StringComparison.Ordinal))
            {
                return "己方";
            }
        }

        return "敌方";
    }

    public static string FormatBuildingDisplayName(GameState state, BuildingState building)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state))
        {
            return building.displayName ?? building.buildingId ?? "建筑";
        }

        var prefix = SidePrefix(state, building.legionId);
        if (string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
        {
            return prefix + "军堡";
        }

        if (string.Equals(building.buildingType, BuildingService.PersonalFortress, StringComparison.Ordinal))
        {
            var tail = StripSideMarker(ResolveRegionBaseName(state, building.eventRegionId));
            if (tail.StartsWith("个堡", StringComparison.Ordinal))
            {
                return prefix + tail;
            }

            return prefix + "个堡";
        }

        return prefix + (building.displayName ?? "建筑");
    }

    public static string FormatEventRegionPlace(GameState state, string? systemId, EventRegionDef er)
    {
        var fallback = er.name ?? er.eventRegionId ?? "?";
        if (!SkirmishBuildingRules.IsSkirmish(state) || er.eventRegionId == null)
        {
            return fallback;
        }

        var building = FindBuildingAtRegion(state, er.eventRegionId);
        if (building != null)
        {
            return FormatBuildingDisplayName(state, building);
        }

        if (EventRegionKinds.IsPlanet(er.kind))
        {
            return StripSideMarker(fallback);
        }

        return StripSideMarker(fallback);
    }

    public static void SyncSkirmishLabels(GameState state)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state))
        {
            return;
        }

        var sys = state.map?.Project?.systems.Count > 0 ? state.map.Project.systems[0] : null;
        if (sys?.eventRegions == null)
        {
            return;
        }

        foreach (var er in sys.eventRegions)
        {
            if (er.eventRegionId == null)
            {
                continue;
            }

            er.name = FormatEventRegionPlace(state, sys.solarSystemId, er);
        }

        foreach (var building in state.buildings)
        {
            building.displayName = FormatBuildingDisplayName(state, building);
        }

        foreach (var bf in state.battlefields)
        {
            if (bf.eventRegionId == null)
            {
                continue;
            }

            foreach (var er in sys.eventRegions)
            {
                if (bf.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal))
                {
                    bf.subLocation = er.name;
                    break;
                }
            }
        }
    }

    private static BuildingState? FindBuildingAtRegion(GameState state, string eventRegionId)
    {
        foreach (var building in state.buildings)
        {
            if (eventRegionId.Equals(building.eventRegionId, StringComparison.Ordinal))
            {
                return building;
            }
        }

        return null;
    }

    private static string ResolveRegionBaseName(GameState state, string? eventRegionId)
    {
        if (eventRegionId == null || state.map?.Project?.systems.Count == 0)
        {
            return "个堡";
        }

        foreach (var er in state.map.Project.systems[0].eventRegions)
        {
            if (eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal))
            {
                return StripSideMarker(er.name ?? eventRegionId);
            }
        }

        return "个堡";
    }

    private static string StripSideMarker(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        if (name.Length > 2 && (name.StartsWith("A ", StringComparison.Ordinal)
                                || name.StartsWith("B ", StringComparison.Ordinal)))
        {
            return name[2..];
        }

        return name;
    }
}
