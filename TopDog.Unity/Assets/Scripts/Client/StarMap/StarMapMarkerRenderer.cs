using System;
using System.Collections.Generic;
using TopDog.Content.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.StarMap;

/// <summary>
/// Star-map marker screen layout only (project ly → panel pixels). Overlap is allowed and expected.
/// Chrome controls (mode bar, 归零/全图) live in UXML; this class does not touch them.
/// </summary>
internal static class StarMapMarkerRenderer
{
    public const float MarkerHitWidthPx = 80f;
    /// <summary>Half of .ops-star-system-icon width (18px) — local point is the 3D circle center.</summary>
    public const float SystemIconHalfPx = 9f;

    public delegate bool ProjectWorldToPanel(Vector3 world, out Vector2 local);

    /// <summary>Place markers at projected ly positions; overlap is intentional.</summary>
    public static void LayoutStrategicMarkers(
        IReadOnlyList<SolarSystemDef> systems,
        IReadOnlyDictionary<string, (Button btn, VisualElement icon, Label label)> markers,
        Func<SolarSystemDef, Vector3> worldForSystem,
        ProjectWorldToPanel projectToPanel)
    {
        if (systems == null)
        {
            return;
        }

        foreach (var sys in systems)
        {
            if (sys.solarSystemId == null || !markers.TryGetValue(sys.solarSystemId, out var pair))
            {
                continue;
            }

            if (!projectToPanel(worldForSystem(sys), out var local))
            {
                pair.btn.style.display = DisplayStyle.None;
                continue;
            }

            pair.btn.style.display = DisplayStyle.Flex;
            pair.btn.style.left = local.x - MarkerHitWidthPx * 0.5f;
            pair.btn.style.top = local.y - SystemIconHalfPx;
            pair.icon.style.alignSelf = Align.Center;
        }
    }
}
