using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.StarMap;

/// <summary>2D projected jump-bridge segments on the star-map UI overlay.</summary>
public sealed class StarMapBridgeOverlayLayer : VisualElement
{
    private List<(Vector2 a, Vector2 b)> _segments = new();

    public StarMapBridgeOverlayLayer()
    {
        name = "star-map-bridges-ui";
        AddToClassList("ops-star-map-bridges-ui");
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnGenerateVisualContent;
    }

    public void SetSegments(List<(Vector2 a, Vector2 b)>? segments)
    {
        _segments = segments ?? new List<(Vector2, Vector2)>();
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (_segments.Count == 0)
        {
            return;
        }

        var painter = ctx.painter2D;
        foreach (var (a, b) in _segments)
        {
            painter.strokeColor = new Color(0.2f, 0.55f, 0.95f, 0.45f);
            painter.lineWidth = 2.4f;
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.Stroke();

            painter.strokeColor = new Color(0.35f, 0.82f, 1f, 0.85f);
            painter.lineWidth = 1.1f;
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.Stroke();
        }
    }
}
