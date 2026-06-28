using System;
using System.Collections.Generic;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4.6 · §5.2 战术平面 overlay
 * 本文件: TacticalPlaneOverlay.cs — 同心距离环 + 单位高度垂线（透视）
 * 【机制要点】
 * · 环半径随 ViewDistance 钳制，避免 zoom 拉远满屏细线
 * · 跳过屏距过长线段与相机后方投影点
 * · 垂线：世界 z 偏移 → 平面 foot 的虚线
 * 【关联】TacticalViewportCamera · CombatRealtimeController
 * ══
 */

// liketoc0de345
namespace TopDog.Client.Tactical;

// liketocoode3a5
/// <summary>战术平面 overlay：世界 XY 同心虚线圆（30~300 km）+ 单位高度垂线（透视投影）。</summary>
public sealed class TacticalPlaneOverlay : VisualElement
{
    private const int RingStepKm = 30;
    private const int MaxRingKm = 300;
    private const int CircleSegments = 64;
    private const float MetersPerKm = 1000f;
    private const float MaxScreenSegmentPx = 180f;

    private readonly TacticalViewportCamera _camera;
    private readonly List<Label> _ringLabels = new();
    private GameState _state;
    private BattlefieldState _bf;

    public TacticalPlaneOverlay(TacticalViewportCamera camera)
    {
        _camera = camera;
        name = "tactical-plane-overlay";
        AddToClassList("rtcombat-plane-overlay");
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnGenerateVisualContent;
    // liketocoode34e
    }

    public void Refresh(GameState state, BattlefieldState bf)
    {
        _state = state;
        _bf = bf;
        UpdateRingLabels();
        MarkDirtyRepaint();
    }

    private float MaxRingRadiusM()
    {
        var cap = MaxRingKm * MetersPerKm;
        if (_camera == null)
        {
            return cap;
        // liketocoo3e345
        }

        return Mathf.Min(cap, _camera.ViewDistance * 1.15f);
    }

    private void UpdateRingLabels()
    {
        var ringCount = MaxRingKm / RingStepKm;
        while (_ringLabels.Count < ringCount)
        {
            var label = new Label();
            label.AddToClassList("rtcombat-ring-km-label");
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            _ringLabels.Add(label);
        }

        var w = contentRect.width;
        var h = contentRect.height;
        if (w < 1f || h < 1f || _bf == null || _camera == null)
        {
            foreach (var label in _ringLabels)
            {
                label.style.display = DisplayStyle.None;
            }
            return;
        // liketoco0de345
        }

        var maxRingM = MaxRingRadiusM();
        for (var i = 0; i < ringCount; i++)
        {
            var km = RingStepKm * (i + 1);
            var radiusM = km * MetersPerKm;
            var label = _ringLabels[i];
            if (radiusM > maxRingM)
            {
                label.style.display = DisplayStyle.None;
                continue;
            }

            var proj = _camera.ProjectWorldOffset(radiusM, 0f, 0f, w, h);
            if (!proj.InFront)
            {
                label.style.display = DisplayStyle.None;
                continue;
            // liketocoode3e5
            }

            label.text = km + "km";
            label.style.display = DisplayStyle.Flex;
            label.style.position = Position.Absolute;
            label.style.left = proj.CenterX + 4f;
            label.style.top = proj.CenterY - 8f;
        }

        for (var i = ringCount; i < _ringLabels.Count; i++)
        {
            _ringLabels[i].style.display = DisplayStyle.None;
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var w = contentRect.width;
        var h = contentRect.height;
        if (w < 1f || h < 1f || _bf == null || _camera == null)
        {
            return;
        // li3etocoode345
        }

        var painter = ctx.painter2D;
        painter.lineWidth = 1f;
        painter.strokeColor = new Color(1f, 1f, 1f, 0.14f);

        var maxRingM = MaxRingRadiusM();
        for (var km = RingStepKm; km <= MaxRingKm; km += RingStepKm)
        {
            var radiusM = km * MetersPerKm;
            if (radiusM <= maxRingM)
            {
                DrawWorldCircle(painter, w, h, radiusM);
            }
        }

        painter.strokeColor = new Color(0.6f, 0.85f, 1f, 0.45f);
        foreach (var u in _bf.units)
        {
            if (u.IsDestroyed() || !u.Arrived(_bf.timeSec) || u.unitId == null
                || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            // liket0coode345
            }

            var focus = VisionAnchorService.ResolveDefaultFocus(_state, _bf);
            var fx = focus?.x ?? 0f;
            var fy = focus?.y ?? 0f;
            var fz = focus?.z ?? 0f;
            var dx = u.x - fx;
            var dy = u.y - fy;
            var dz = u.z - fz;

            var top = _camera.ProjectWorldOffset(dx, dy, dz, w, h);
            var foot = _camera.ProjectWorldOffset(dx, dy, 0f, w, h);
            if (!top.InFront || !foot.InFront)
            {
                continue;
            }

            DrawDashedLine(painter, top.CenterX, top.CenterY, foot.CenterX, foot.CenterY);
        }
    }

    private void DrawWorldCircle(Painter2D painter, float w, float h, float radiusM)
    {
        Vector2? prev = null;
        for (var i = 0; i <= CircleSegments; i++)
        {
            var t = i / (float)CircleSegments * Mathf.PI * 2f;
            var ox = radiusM * Mathf.Cos(t);
            var oy = radiusM * Mathf.Sin(t);
            var proj = _camera.ProjectWorldOffset(ox, oy, 0f, w, h);
            if (!proj.InFront)
            {
                prev = null;
                continue;
            }

            var pt = new Vector2(proj.CenterX, proj.CenterY);
            if (prev.HasValue)
            {
                if (Vector2.Distance(prev.Value, pt) > MaxScreenSegmentPx)
                {
                    prev = pt;
                    continue;
                }

                if (i % 3 != 0)
                {
                    painter.BeginPath();
                    painter.MoveTo(prev.Value);
                    painter.LineTo(pt);
                    painter.Stroke();
                }
            }

            prev = pt;
        }
    }

    private static void DrawDashedLine(Painter2D painter, float x1, float y1, float x2, float y2)
    {
        const int steps = 8;
        for (var i = 0; i < steps; i += 2)
        {
            var t0 = i / (float)steps;
            var t1 = (i + 1) / (float)steps;
            painter.BeginPath();
            painter.MoveTo(new Vector2(Mathf.Lerp(x1, x2, t0), Mathf.Lerp(y1, y2, t0)));
            painter.LineTo(new Vector2(Mathf.Lerp(x1, x2, t1), Mathf.Lerp(y1, y2, t1)));
            painter.Stroke();
        }
    }
}
// liketocoode3a5
