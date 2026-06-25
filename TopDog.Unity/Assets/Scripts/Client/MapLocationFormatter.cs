using System;
using System.Collections.Generic;
using TopDog.Content.Map;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Client;

/// <summary>星域-星座-星系-地点 层级显示（MAP_SPEC / TACTICAL_VIEW）。</summary>
public static class MapLocationFormatter
{
    public static string FormatSystemPath(GameState state, string? systemId)
    {
        if (string.IsNullOrWhiteSpace(systemId) || state.map?.Project == null)
        {
            return systemId ?? "?";
        }
        var project = state.map.Project;
        SolarSystemDef? sys = null;
        foreach (var s in project.systems)
        {
            if (systemId.Equals(s.solarSystemId, StringComparison.Ordinal))
            {
                sys = s;
                break;
            }
        }
        if (sys == null)
        {
            return systemId;
        }
        var region = FindName(project.regions, sys.regionId, r => r.regionId, r => r.name);
        var con = FindName(project.constellations, sys.constellationId, c => c.constellationId, c => c.name);
        var sysName = sys.name ?? sys.solarSystemId ?? "?";
        return JoinParts(region, con, sysName);
    }

    public static string FormatBattlefield(GameState state, BattlefieldState bf)
    {
        var systemPath = FormatSystemPath(state, bf.systemId);
        var place = ResolvePlaceName(state, bf);
        return string.IsNullOrWhiteSpace(place) ? systemPath : systemPath + " · " + place;
    }

    private static string ResolvePlaceName(GameState state, BattlefieldState bf)
    {
        if (bf.systemId != null && bf.eventRegionId != null)
        {
            var sys = FindSystem(state.map?.Project, bf.systemId);
            if (sys?.eventRegions != null)
            {
                foreach (var er in sys.eventRegions)
                {
                    if (bf.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal))
                    {
                        return er.name ?? er.eventRegionId;
                    }
                }
            }
        }
        return bf.subLocation ?? bf.eventRegionId ?? bf.battlefieldId ?? "?";
    }

    private static SolarSystemDef? FindSystem(MapProject? project, string systemId)
    {
        if (project == null)
        {
            return null;
        }
        foreach (var s in project.systems)
        {
            if (systemId.Equals(s.solarSystemId, StringComparison.Ordinal))
            {
                return s;
            }
        }
        return null;
    }

    private static string? FindName<T>(
        List<T> list,
        string? id,
        Func<T, string?> idFn,
        Func<T, string?> nameFn)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        foreach (var item in list)
        {
            var itemId = idFn(item);
            if (id.Equals(itemId, StringComparison.Ordinal))
            {
                return nameFn(item) ?? itemId;
            }
        }
        return id;
    }

    private static string JoinParts(params string?[] parts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var p in parts)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }
            if (sb.Length > 0)
            {
                sb.Append(" · ");
            }
            sb.Append(p);
        }
        return sb.Length > 0 ? sb.ToString() : "?";
    }
}
