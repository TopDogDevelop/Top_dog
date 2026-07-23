/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §2 · docs/VISUAL_ASSETS.md §3
 * 本文件: TacticalIconBatchElement.cs — UITK 面板批画战术图标
 * 【机制要点】
 * · 舰体：吨位 PNG（白 tint），**不是**敌我色块
 * · 声望：右下角 −/+ 字形 PNG（蓝/红），禁止色块回退
 * · SetIcons；PickNearest
 * 【关联】TacticalViewportPresenter · TacticalIconCatalog
 * ══
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

public sealed class TacticalIconBatchElement : VisualElement
{
    public const float IconHalfPx = 8f;
    public const float BadgeHalfPx = 5f;
    public const int PickExploreCap = 512;
    public const float MotionArrowMinSpeedMps = 500f;
    public const float MotionArrowMinLengthPx = 14f;
    public const float MotionArrowMaxLengthPx = 72f;

    private float[] _x = Array.Empty<float>();
    private float[] _y = Array.Empty<float>();
    private bool[] _hostile = Array.Empty<bool>();
    private string[] _ids = Array.Empty<string>();
    private string[] _tonnage = Array.Empty<string>();
    private float[] _motionDirX = Array.Empty<float>();
    private float[] _motionDirY = Array.Empty<float>();
    private float[] _speedMps = Array.Empty<float>();
    private int _count;

    private readonly List<(Texture2D tex, List<int> indices)> _texGroups = new();
    private readonly List<int> _noTex = new();

    public TacticalIconBatchElement()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        generateVisualContent += OnGenerateVisualContent;
    }

    public int Count => _count;

    public void SetIcons(
        IReadOnlyList<(
            float cx,
            float cy,
            bool hostile,
            string id,
            string tonnage,
            float motionDirX,
            float motionDirY,
            float speedMps)> icons)
    {
        _count = icons.Count;
        Ensure(_count);
        for (var i = 0; i < _count; i++)
        {
            var it = icons[i];
            _x[i] = it.cx;
            _y[i] = it.cy;
            _hostile[i] = it.hostile;
            _ids[i] = it.id;
            _tonnage[i] = it.tonnage ?? "";
            _motionDirX[i] = it.motionDirX;
            _motionDirY[i] = it.motionDirY;
            _speedMps[i] = it.speedMps;
        }

        RebuildTexGroups();
        MarkDirtyRepaint();
    }

    public void ClearIcons()
    {
        _count = 0;
        _texGroups.Clear();
        _noTex.Clear();
        MarkDirtyRepaint();
    }

    public string? PickNearest(Vector2 localPos, float radiusPx = 22f)
    {
        string? best = null;
        var bestD = radiusPx * radiusPx;
        var limit = _count <= PickExploreCap ? _count : PickExploreCap;
        for (var i = 0; i < limit; i++)
        {
            var dx = _x[i] - localPos.x;
            var dy = _y[i] - localPos.y;
            var d = dx * dx + dy * dy;
            if (d <= bestD)
            {
                bestD = d;
                best = _ids[i];
            }
        }

        if (_count > PickExploreCap)
        {
            var stride = Math.Max(1, _count / PickExploreCap);
            for (var i = 0; i < _count; i += stride)
            {
                var dx = _x[i] - localPos.x;
                var dy = _y[i] - localPos.y;
                var d = dx * dx + dy * dy;
                if (d <= bestD)
                {
                    bestD = d;
                    best = _ids[i];
                }
            }
        }

        return best;
    }

    public bool TryGetScreenCenter(string unitId, out Vector2 center)
    {
        center = default;
        for (var i = 0; i < _count; i++)
        {
            if (_ids[i] == unitId)
            {
                center = new Vector2(_x[i], _y[i]);
                return true;
            }
        }

        return false;
    }

    private void RebuildTexGroups()
    {
        _texGroups.Clear();
        _noTex.Clear();
        var map = new Dictionary<Texture2D, List<int>>();
        for (var i = 0; i < _count; i++)
        {
            var tex = TacticalIconCatalog.ResolveShipIcon(
                string.IsNullOrEmpty(_tonnage[i]) ? null : _tonnage[i]);
            if (tex == null)
            {
                _noTex.Add(i);
                continue;
            }

            if (!map.TryGetValue(tex, out var list))
            {
                list = new List<int>();
                map[tex] = list;
                _texGroups.Add((tex, list));
            }

            list.Add(i);
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (_count <= 0)
        {
            return;
        }

        // 1) 高速运动矢量：中性色，避免替代敌我 +/− 角标语义。
        DrawMotionArrows(ctx.painter2D);

        // 2) 舰体图标：PNG × 白 tint（与稀疏 marker 相同），绝不用敌我色整图标上色
        for (var g = 0; g < _texGroups.Count; g++)
        {
            var (tex, indices) = _texGroups[g];
            DrawTexturedQuads(ctx, tex, indices, Color.white);
        }

        if (_noTex.Count > 0)
        {
            var painter = ctx.painter2D;
            // 缺图占位：中性深底，敌我只靠角标
            painter.fillColor = new Color(0.18f, 0.22f, 0.28f, 0.9f);
            for (var n = 0; n < _noTex.Count; n++)
            {
                DrawSolidRect(painter, _noTex[n], IconHalfPx);
            }
        }

        // 3) 声望角标：右下 −/+ 字形 PNG（禁止色块回退）
        DrawStandingBadges(ctx);
    }

    private void DrawMotionArrows(Painter2D painter)
    {
        painter.strokeColor = new Color(0.78f, 0.9f, 1f, 0.78f);
        painter.lineWidth = 1.5f;
        for (var i = 0; i < _count; i++)
        {
            var speed = _speedMps[i];
            var dx = _motionDirX[i];
            var dy = _motionDirY[i];
            var dirLen = Mathf.Sqrt(dx * dx + dy * dy);
            if (speed < MotionArrowMinSpeedMps || dirLen <= 0.0001f)
            {
                continue;
            }

            dx /= dirLen;
            dy /= dirLen;
            var arrowLength = Mathf.Clamp(
                MotionArrowMinLengthPx + (speed - MotionArrowMinSpeedMps) * 0.02f,
                MotionArrowMinLengthPx,
                MotionArrowMaxLengthPx);
            var start = new Vector2(
                _x[i] + dx * (IconHalfPx + 2f),
                _y[i] + dy * (IconHalfPx + 2f));
            var end = start + new Vector2(dx, dy) * arrowLength;
            var sideX = -dy;
            var sideY = dx;
            const float headLength = 7f;
            const float headHalfWidth = 4f;
            var headBase = end - new Vector2(dx, dy) * headLength;

            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(end);
            painter.MoveTo(end);
            painter.LineTo(headBase + new Vector2(sideX, sideY) * headHalfWidth);
            painter.MoveTo(end);
            painter.LineTo(headBase - new Vector2(sideX, sideY) * headHalfWidth);
            painter.Stroke();
        }
    }

    private void DrawTexturedQuads(
        MeshGenerationContext ctx,
        Texture2D tex,
        List<int> indices,
        Color tint)
    {
        var mesh = ctx.Allocate(indices.Count * 4, indices.Count * 6, tex);
        var hx = IconHalfPx;
        for (var n = 0; n < indices.Count; n++)
        {
            var i = indices[n];
            var x0 = _x[i] - hx;
            var y0 = _y[i] - hx;
            var x1 = _x[i] + hx;
            var y1 = _y[i] + hx;
            EmitQuad(mesh, n, x0, y0, x1, y1, tint);
        }
    }

    private void DrawStandingBadges(MeshGenerationContext ctx)
    {
        var friendlyTex = TacticalIconCatalog.BadgeFriendly;
        var hostileTex = TacticalIconCatalog.BadgeHostile;
        if (friendlyTex == null || hostileTex == null)
        {
            return;
        }

        DrawBadgeGroup(ctx, friendlyTex, hostile: false);
        DrawBadgeGroup(ctx, hostileTex, hostile: true);
    }

    private void DrawBadgeGroup(MeshGenerationContext ctx, Texture2D tex, bool hostile)
    {
        var count = 0;
        for (var i = 0; i < _count; i++)
        {
            if (_hostile[i] == hostile)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return;
        }

        var mesh = ctx.Allocate(count * 4, count * 6, tex);
        var bh = BadgeHalfPx;
        var n = 0;
        for (var i = 0; i < _count; i++)
        {
            if (_hostile[i] != hostile)
            {
                continue;
            }

            var cx = _x[i] + IconHalfPx - bh;
            var cy = _y[i] + IconHalfPx - bh;
            EmitQuad(mesh, n, cx - bh, cy - bh, cx + bh, cy + bh, Color.white);
            n++;
        }
    }

    private void DrawSolidRect(Painter2D painter, int i, float half)
    {
        var x0 = _x[i] - half;
        var y0 = _y[i] - half;
        painter.BeginPath();
        painter.MoveTo(new Vector2(x0, y0));
        painter.LineTo(new Vector2(x0 + half * 2f, y0));
        painter.LineTo(new Vector2(x0 + half * 2f, y0 + half * 2f));
        painter.LineTo(new Vector2(x0, y0 + half * 2f));
        painter.ClosePath();
        painter.Fill();
    }

    private static void EmitQuad(
        MeshWriteData mesh,
        int quadIndex,
        float x0,
        float y0,
        float x1,
        float y1,
        Color tint)
    {
        mesh.SetNextVertex(new Vertex
        {
            position = new Vector3(x0, y0, Vertex.nearZ),
            tint = tint,
            uv = new Vector2(0, 1),
        });
        mesh.SetNextVertex(new Vertex
        {
            position = new Vector3(x1, y0, Vertex.nearZ),
            tint = tint,
            uv = new Vector2(1, 1),
        });
        mesh.SetNextVertex(new Vertex
        {
            position = new Vector3(x1, y1, Vertex.nearZ),
            tint = tint,
            uv = new Vector2(1, 0),
        });
        mesh.SetNextVertex(new Vertex
        {
            position = new Vector3(x0, y1, Vertex.nearZ),
            tint = tint,
            uv = new Vector2(0, 0),
        });
        var b = (ushort)(quadIndex * 4);
        mesh.SetNextIndex(b);
        mesh.SetNextIndex((ushort)(b + 1));
        mesh.SetNextIndex((ushort)(b + 2));
        mesh.SetNextIndex(b);
        mesh.SetNextIndex((ushort)(b + 2));
        mesh.SetNextIndex((ushort)(b + 3));
    }

    /// <summary>仅角标色：VISUAL_ASSETS §3。</summary>
    public static Color StandingBadgeColor(bool hostile) =>
        hostile
            ? new Color(220f / 255f, 70f / 255f, 70f / 255f, 1f)
            : new Color(80f / 255f, 160f / 255f, 1f, 1f);

    private void Ensure(int n)
    {
        if (_x.Length >= n)
        {
            return;
        }

        _x = new float[n];
        _y = new float[n];
        _hostile = new bool[n];
        _ids = new string[n];
        _tonnage = new string[n];
        _motionDirX = new float[n];
        _motionDirY = new float[n];
        _speedMps = new float[n];
    }
}
