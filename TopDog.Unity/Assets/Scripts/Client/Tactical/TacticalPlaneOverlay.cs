using System;

using TopDog.Sim.Realtime;

using TopDog.Sim.State;

using TopDog.Sim.Vision;

using UnityEngine;

using UnityEngine.UIElements;



namespace TopDog.Client.Tactical;



/// <summary>战术平面 overlay：世界 XY 同心虚线圆 + 单位高度垂线（TACTICAL_VIEW.md §5.1）。</summary>

public sealed class TacticalPlaneOverlay : VisualElement

{

    private const int CircleCount = 5;

    private const float CircleStepM = 2_000f;

    private const int CircleSegments = 64;



    private readonly TacticalViewportCamera _camera;

    private GameState _state;

    private BattlefieldState _bf;



    public TacticalPlaneOverlay(TacticalViewportCamera camera)

    {

        _camera = camera;

        name = "tactical-plane-overlay";

        AddToClassList("rtcombat-plane-overlay");

        pickingMode = PickingMode.Ignore;

        generateVisualContent += OnGenerateVisualContent;

    }



    public void Refresh(GameState state, BattlefieldState bf)

    {

        _state = state;

        _bf = bf;

        MarkDirtyRepaint();

    }



    private void OnGenerateVisualContent(MeshGenerationContext ctx)

    {

        var w = contentRect.width;

        var h = contentRect.height;

        if (w < 1f || h < 1f || _bf == null || _camera == null)

        {

            return;

        }



        var painter = ctx.painter2D;

        var cx = w * 0.5f;

        var cy = h * 0.5f;

        var scale = _camera.WorldScale;



        var focus = VisionAnchorService.ResolveDefaultFocus(_state, _bf);

        var fx = focus?.x ?? 0f;

        var fy = focus?.y ?? 0f;

        var fz = focus?.z ?? 0f;



        painter.lineWidth = 1f;

        painter.strokeColor = new Color(1f, 1f, 1f, 0.14f);



        for (var ring = 1; ring <= CircleCount; ring++)

        {

            var radiusM = CircleStepM * ring;

            DrawWorldCircle(painter, cx, cy, scale, fx, fy, fz, radiusM);

        }



        painter.strokeColor = new Color(0.6f, 0.85f, 1f, 0.45f);

        foreach (var u in _bf.units)

        {

            if (u.IsDestroyed() || !u.Arrived(_bf.timeSec) || u.unitId == null)

            {

                continue;

            }



            var dx = u.x - fx;

            var dy = u.y - fy;

            var dz = u.z - fz;

            if (Mathf.Abs(dz) < 1f)

            {

                continue;

            }



            _camera.TransformOffset(dx, dy, dz, out var sx, out var sy);

            _camera.TransformOffset(dx, dy, 0f, out var px, out var py);

            var x1 = cx + sx * scale;

            var y1 = cy - sy * scale;

            var x2 = cx + px * scale;

            var y2 = cy - py * scale;

            DrawDashedLine(painter, x1, y1, x2, y2);

        }

    }



    private void DrawWorldCircle(

        Painter2D painter,

        float cx,

        float cy,

        float scale,

        float fx,

        float fy,

        float fz,

        float radiusM)

    {

        Vector2? prev = null;

        for (var i = 0; i <= CircleSegments; i++)

        {

            var t = i / (float)CircleSegments * Mathf.PI * 2f;

            var ox = radiusM * Mathf.Cos(t);

            var oy = radiusM * Mathf.Sin(t);

            _camera.TransformOffset(ox, oy, 0f, out var sx, out var sy);

            var pt = new Vector2(cx + sx * scale, cy - sy * scale);

            if (prev.HasValue && i % 3 != 0)

            {

                painter.BeginPath();

                painter.MoveTo(prev.Value);

                painter.LineTo(pt);

                painter.Stroke();

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


