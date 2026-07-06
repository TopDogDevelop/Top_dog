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
 * 本文件: TacticalPlaneOverlay.cs — 焦距同心距离环 + 高度垂线
 * 【机制要点】
 * · 圆心 = ResolveDefaultFocus 世界坐标（战术注视点）
 * · 环半径 30 km 步进，最外圈铺满当前视口（随 ViewDistance/FOV）
 * 【关联】TacticalViewportCamera · TacticalViewportPresenter · VisionAnchorService
 * ══
 */

/// <summary>战术平面 overlay：焦距同心虚线圆 + 单位高度垂线（透视投影）。</summary>
public sealed class TacticalPlaneOverlay : VisualElement
{
    private const int RingStepKm = 30;
    private const int MinRingKm = 30;
    private const int MaxRingKmCap = 600;
    private const int CircleSegments = 64;
    private const float MetersPerKm = 1000f;
    private const float MaxScreenSegmentPx = 180f;
    private const float ViewportFillInset = 0.92f;

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

    private float MaxRingRadiusM(float viewportW, float viewportH)
    {
        if (_camera == null || viewportW < 1f || viewportH < 1f)
        {
            return MinRingKm * MetersPerKm;
        }

        var tanHalf = Mathf.Tan(_camera.VerticalFovDeg * Mathf.Deg2Rad * 0.5f);
        var aspect = viewportW / Mathf.Max(viewportH, 1f);
        var horizontalM = _camera.ViewDistance * tanHalf * aspect;
        var verticalM = _camera.ViewDistance * tanHalf;
        var reachM = Mathf.Max(horizontalM, verticalM) * ViewportFillInset;
        var stepM = RingStepKm * MetersPerKm;
        var ringCount = Mathf.Max(1, Mathf.CeilToInt(reachM / stepM));
        var maxM = ringCount * stepM;
        return Mathf.Min(maxM, MaxRingKmCap * MetersPerKm);
    }

    private int RingCount(float viewportW, float viewportH) =>
        Mathf.Max(1, Mathf.RoundToInt(MaxRingRadiusM(viewportW, viewportH) / (RingStepKm * MetersPerKm)));

    private void UpdateRingLabels()
    {
        var w = contentRect.width;
        var h = contentRect.height;
        var ringCount = RingCount(w, h);
        while (_ringLabels.Count < ringCount)
        {
            var label = new Label();
            label.AddToClassList("rtcombat-ring-km-label");
            label.pickingMode = PickingMode.Ignore;
            Add(label);
            _ringLabels.Add(label);
        }

        if (w < 1f || h < 1f || _bf == null || _camera == null || _state == null)
        {
            foreach (var label in _ringLabels)
            {
                label.style.display = DisplayStyle.None;
            }

            return;
        }

        var maxRingM = MaxRingRadiusM(w, h);

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

        var painter = ctx.painter2D;
        painter.lineWidth = 1f;
        painter.strokeColor = new Color(1f, 1f, 1f, 0.14f);

        var maxRingM = MaxRingRadiusM(w, h);
        for (var km = RingStepKm; km * MetersPerKm <= maxRingM; km += RingStepKm)
        {
            DrawWorldCircle(painter, w, h, fx, fy, fz, km * MetersPerKm);
        }

        painter.strokeColor = new Color(0.6f, 0.85f, 1f, 0.45f);
        foreach (var u in _bf.units)
        {
            if (u.IsDestroyed() || !u.Arrived(_bf.timeSec) || u.unitId == null
                || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            var top = _camera.ProjectWorldOffset(u.x - fx, u.y - fy, u.z - fz, w, h);
            var foot = _camera.ProjectWorldOffset(u.x - fx, u.y - fy, fz - fz, w, h);
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
        float cx,
        float cy,
        float cz,
        float radiusM)
    {
        Vector2? prev = null;
        for (var i = 0; i <= CircleSegments; i++)
        {
            var t = i / (float)CircleSegments * Mathf.PI * 2f;
            var dx = radiusM * Mathf.Cos(t);
            var dy = radiusM * Mathf.Sin(t);
            var proj = _camera.ProjectWorldOffset(dx, dy, 0f, w, h);
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
