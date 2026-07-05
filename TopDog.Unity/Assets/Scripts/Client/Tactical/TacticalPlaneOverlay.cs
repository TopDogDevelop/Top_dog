using System;
using System.Collections.Generic;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4.6 · §5.2
 * 本文件: TacticalPlaneOverlay.cs — 场景原点同心距离环 + 高度垂线
 * 【机制要点】
 * · 圆心 = BattlefieldSceneOriginService.Resolve 世界坐标透视投影
 * · 环半径步进/上限随 ViewDistance 动态钳制；GeometryChangedEvent 重绘
 * 【关联】TacticalViewportCamera · TacticalViewportPresenter · BattlefieldSceneOriginService
 * ══
 */

/// <summary>战术平面 overlay：场景原点同心虚线圆 + 单位高度垂线（透视投影）。</summary>
public sealed class TacticalPlaneOverlay : VisualElement
{
    private const int RingStepKm = 30;
    private const int MinRingKm = 30;
    private const int MaxRingKmCap = 600;
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
        RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
    }

    public void Refresh(GameState state, BattlefieldState bf)
    {
        _state = state;
        _bf = bf;
        UpdateRingLabels();
        MarkDirtyRepaint();
    }

    private int MaxRingKm()
    {
        if (_camera == null)
        {
            return MaxRingKmCap;
        }

        var viewKm = Mathf.CeilToInt(_camera.ViewDistance / MetersPerKm);
        return Mathf.Clamp(Mathf.Max(MinRingKm, viewKm / 2), MinRingKm, MaxRingKmCap);
    }

    private float MaxRingRadiusM() => MaxRingKm() * MetersPerKm;

    private void UpdateRingLabels()
    {
        var ringCount = MaxRingKm() / RingStepKm;
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
        if (w < 1f || h < 1f || _bf == null || _camera == null || _state == null)
        {
            foreach (var label in _ringLabels)
            {
                label.style.display = DisplayStyle.None;
            }

            return;
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(_state, _bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        BattlefieldSceneOriginService.Resolve(_state, _bf, out var ox, out var oy, out var oz);

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

            var proj = _camera.ProjectWorldOffset(ox + radiusM - fx, oy - fy, oz - fz, w, h);
            if (!proj.InFront)
            {
                label.style.display = DisplayStyle.None;
                continue;
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
        if (w < 1f || h < 1f || _bf == null || _camera == null || _state == null)
        {
            return;
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(_state, _bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        BattlefieldSceneOriginService.Resolve(_state, _bf, out var ox, out var oy, out var oz);

        var painter = ctx.painter2D;
        painter.lineWidth = 1f;
        painter.strokeColor = new Color(1f, 1f, 1f, 0.14f);

        var maxRingM = MaxRingRadiusM();
        for (var km = RingStepKm; km <= MaxRingKm(); km += RingStepKm)
        {
            var radiusM = km * MetersPerKm;
            if (radiusM <= maxRingM)
            {
                DrawWorldCircle(painter, w, h, ox, oy, oz, fx, fy, fz, radiusM);
            }
        }

        painter.strokeColor = new Color(0.6f, 0.85f, 1f, 0.45f);
        foreach (var u in _bf.units)
        {
            if (u.IsDestroyed() || !u.Arrived(_bf.timeSec) || u.unitId == null
                || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            var dx = u.x - fx;
            var dy = u.y - fy;
            var dz = u.z - fz;

            var top = _camera.ProjectWorldOffset(dx, dy, dz, w, h);
        var foot = _camera.ProjectWorldOffset(u.x - fx, u.y - fy, oz - fz, w, h);
            if (!top.InFront || !foot.InFront)
            {
                continue;
            }

            DrawDashedLine(painter, top.CenterX, top.CenterY, foot.CenterX, foot.CenterY);
        }
    }

    private void DrawWorldCircle(
        Painter2D painter,
        float w,
        float h,
        float ox,
        float oy,
        float oz,
        float fx,
        float fy,
        float fz,
        float radiusM)
    {
        Vector2? prev = null;
        for (var i = 0; i <= CircleSegments; i++)
        {
            var t = i / (float)CircleSegments * Mathf.PI * 2f;
            var wx = ox + radiusM * Mathf.Cos(t);
            var wy = oy + radiusM * Mathf.Sin(t);
            var wz = oz;
            var proj = _camera.ProjectWorldOffset(wx - fx, wy - fy, wz - fz, w, h);
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
