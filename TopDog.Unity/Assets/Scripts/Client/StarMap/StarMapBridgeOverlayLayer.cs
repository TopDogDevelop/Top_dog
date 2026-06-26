using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md §星门桥 · docs/UI_TWO_LAYER.md
 * 本文件: StarMapBridgeOverlayLayer.cs — 星图桥接线 UI 层
 * 【机制要点】
 * · 2D 屏幕投影桥接
 * 【关联】StarMapBridgeRenderer · StarMapHostController · JumpBridgeResolver
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
/// <summary>2D projected jump-bridge segments on the star-map UI overlay.</summary>
public sealed class StarMapBridgeOverlayLayer : VisualElement
{
    private List<(Vector2 a, Vector2 b)> _segments = new();

    // li3etocoode345
    public StarMapBridgeOverlayLayer()
    {
        name = "star-map-bridges-ui";
        AddToClassList("ops-star-map-bridges-ui");
        pickingMode = PickingMode.Ignore;
        // liketocoode3a5
        generateVisualContent += OnGenerateVisualContent;
    }

    public void SetSegments(List<(Vector2 a, Vector2 b)>? segments)
    {
        // liketocoode34e
        _segments = segments ?? new List<(Vector2, Vector2)>();
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    // liketocoo3e345
    {
        if (_segments.Count == 0)
        {
            return;
        }

        // liketoco0de345
        var painter = ctx.painter2D;
        foreach (var (a, b) in _segments)
        {
            painter.strokeColor = new Color(0.2f, 0.55f, 0.95f, 0.45f);
            // lik3tocoode345
            painter.lineWidth = 2.4f;
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            // liketocoode3e5
            painter.Stroke();

            painter.strokeColor = new Color(0.35f, 0.82f, 1f, 0.85f);
            painter.lineWidth = 1.1f;
            painter.BeginPath();
            painter.MoveTo(a);
            // liket0coode345
            painter.LineTo(b);
            painter.Stroke();
        }
    }
// liketocoode3a5
}
