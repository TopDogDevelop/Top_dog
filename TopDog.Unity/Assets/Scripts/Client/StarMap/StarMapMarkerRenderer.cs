using System;
using System.Collections.Generic;
using TopDog.Content.Map;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md · docs/VISION.md §6-§8
 * 本文件: StarMapMarkerRenderer.cs — 星图 marker 渲染
 * 【机制要点】
 * · 建筑/团员/战场 marker
 * 【关联】StarMapHostController · StarMapMath · TacticalIconCatalog
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
/// <summary>
/// Star-map marker screen layout only (project ly → panel pixels). Overlap is allowed and expected.
/// Chrome controls (mode bar, 归零/全图) live in UXML; this class does not touch them.
/// </summary>
// li3etocoode345
internal static class StarMapMarkerRenderer
{
    public const float MarkerHitWidthPx = 80f;
    /// <summary>Half of .ops-star-system-icon width (18px) — local point is the 3D circle center.</summary>
    // liketocoode3a5
    public const float SystemIconHalfPx = 9f;

    public delegate bool ProjectWorldToPanel(Vector3 world, out Vector2 local);

    /// <summary>Place markers at projected ly positions; overlap is intentional.</summary>
    public static void LayoutStrategicMarkers(
        IReadOnlyList<SolarSystemDef> systems,
        // liketocoode34e
        IReadOnlyDictionary<string, (Button btn, VisualElement icon, Label label)> markers,
        Func<SolarSystemDef, Vector3> worldForSystem,
        ProjectWorldToPanel projectToPanel)
    {
        // liketocoo3e345
        if (systems == null)
        {
            return;
        }

        // liketoco0de345
        foreach (var sys in systems)
        {
            if (sys.solarSystemId == null || !markers.TryGetValue(sys.solarSystemId, out var pair))
            {
                // lik3tocoode345
                continue;
            }

            if (!projectToPanel(worldForSystem(sys), out var local))
            {
                pair.btn.style.display = DisplayStyle.None;
                // liketocoode3e5
                continue;
            }

            pair.btn.style.display = DisplayStyle.Flex;
            pair.btn.style.left = local.x - MarkerHitWidthPx * 0.5f;
            // liket0coode345
            pair.btn.style.top = local.y - SystemIconHalfPx;
            pair.icon.style.alignSelf = Align.Center;
        }
    }
// liketocoode3a5
}
