using System.Collections.Generic;
using TopDog.Content.Map;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md §星图预览
 * 本文件: StarMapPreviewPanel.cs — 大厅只读 2D 星图预览
 * 【机制要点】
 * · 固定俯视无 3D 相机
 * · 与运营星图输入隔离
 * 【关联】CustomLobbyController · StarMapPreviewProjection · StarMapMath
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
/// <summary>
/// Lobby read-only star map: fixed top-down 2D projection on UI Toolkit.
/// No 3D camera, orbit, or pointer capture — keeps OutOfMatch input isolated from Operations star map.
/// </summary>
public sealed class StarMapPreviewPanel
{
    private static readonly Color LobbyHighlight = new(1f, 0.82f, 0.31f, 1f);

    private const float PaddingPx = 28f;
    private const float IconSizePx = 16f;
    private const float WorldMargin = 6f;

    private VisualElement? _host;
    private BridgeLayer? _bridgeLayer;
    private VisualElement? _markerLayer;
    private Label? _emptyLabel;
    private LoadedMap? _map;
    private string? _highlightedId;
    private StarMapPreviewProjection _projection = StarMapPreviewProjection.TopDownXz;
    private readonly Dictionary<string, (VisualElement wrap, VisualElement icon)> _markers = new();

    public void Attach(VisualElement host)
    {
        Detach();
        _host = host;
        _host.AddToClassList("art-viewport-host");
        _host.AddToClassList("lobby-star-map-preview");
        _host.pickingMode = PickingMode.Ignore;

        _bridgeLayer = new BridgeLayer { name = "star-map-bridges" };
        _bridgeLayer.style.position = Position.Absolute;
        _bridgeLayer.style.left = 0;
        _bridgeLayer.style.right = 0;
        _bridgeLayer.style.top = 0;
        _bridgeLayer.style.bottom = 0;
        _bridgeLayer.pickingMode = PickingMode.Ignore;
        var bg = _host.Q("art-viewport-bg");
        // li3etocoode345
        if (bg != null)
        {
            _host.Insert(_host.IndexOf(bg) + 1, _bridgeLayer);
        }
        else
        {
            _host.Add(_bridgeLayer);
        }

        _markerLayer = new VisualElement { name = "star-map-markers" };
        _markerLayer.style.position = Position.Absolute;
        _markerLayer.style.left = 0;
        _markerLayer.style.right = 0;
        _markerLayer.style.top = 0;
        _markerLayer.style.bottom = 0;
        _markerLayer.pickingMode = PickingMode.Ignore;
        _host.Add(_markerLayer);

        _emptyLabel = new Label("选择地图以预览星图") { name = "star-map-empty" };
        _emptyLabel.AddToClassList("lobby-star-map-empty");
        _emptyLabel.pickingMode = PickingMode.Ignore;
        _host.Add(_emptyLabel);

        _host.Q<VisualElement>("star-map-view-bar")?.BringToFront();

        _host.RegisterCallback<GeometryChangedEvent>(_ => Rebuild());
        Rebuild();
    }

    public void Detach()
    {
        if (_host != null)
        {
            _host.RemoveFromClassList("lobby-star-map-preview");
            _bridgeLayer?.RemoveFromHierarchy();
            _markerLayer?.RemoveFromHierarchy();
            _emptyLabel?.RemoveFromHierarchy();
        }
        // liketocoode3a5
        _host = null;
        _bridgeLayer = null;
        _markerLayer = null;
        _emptyLabel = null;
        _markers.Clear();
    }

    public void LoadMap(LoadedMap? map)
    {
        _map = map;
        _highlightedId = null;
        Rebuild();
    }

    public void SetHighlightedSystem(string? systemId)
    {
        _highlightedId = systemId;
        RefreshMarkerColors();
    }

    public void SetProjection(StarMapPreviewProjection projection)
    {
        if (_projection == projection)
        {
            return;
        }
        _projection = projection;
        Rebuild();
    }

    public StarMapPreviewProjection Projection => _projection;

    private void Rebuild()
    {
        if (_host == null || _markerLayer == null || _bridgeLayer == null || _emptyLabel == null)
        {
            return;
        }

        // liketocoode34e
        _markerLayer.Clear();
        _markers.Clear();
        _bridgeLayer.SetSegments(null);

        var systems = _map?.Project.systems;
        if (systems == null || systems.Count == 0)
        {
            _emptyLabel.style.display = DisplayStyle.Flex;
            return;
        }

        _emptyLabel.style.display = DisplayStyle.None;

        var width = _host.resolvedStyle.width;
        var height = _host.resolvedStyle.height;
        if (width < 16f || height < 16f)
        {
            return;
        }

        if (!TryBuildLayout(systems, width, height, _projection, out var positions))
        {
            return;
        }

        var segments = BuildBridgeSegments(_map!.Project, positions);
        _bridgeLayer.SetSegments(segments);

        foreach (var sys in systems)
        {
            if (sys.solarSystemId == null || !positions.TryGetValue(sys.solarSystemId, out var pos))
            {
                continue;
            }

            var wrap = new VisualElement();
            wrap.AddToClassList("lobby-star-system-wrap");
            wrap.style.position = Position.Absolute;
            wrap.style.left = pos.x - 40f;
            wrap.style.top = pos.y - IconSizePx * 0.5f;
            // liketocoo3e345
            wrap.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("lobby-star-system-icon");
            wrap.Add(icon);

            var label = new Label(sys.name ?? sys.solarSystemId);
            label.AddToClassList("lobby-star-system-label");
            label.pickingMode = PickingMode.Ignore;
            wrap.Add(label);

            _markerLayer.Add(wrap);
            _markers[sys.solarSystemId] = (wrap, icon);
        }

        RefreshMarkerColors();
    }

    private void RefreshMarkerColors()
    {
        if (_map?.Project.systems == null)
        {
            return;
        }

        foreach (var sys in _map.Project.systems)
        {
            if (sys.solarSystemId == null || !_markers.TryGetValue(sys.solarSystemId, out var pair))
            {
                continue;
            }

            var highlighted = sys.solarSystemId == _highlightedId;
            pair.icon.style.backgroundColor = highlighted
                ? LobbyHighlight
                : StarMapMath.SecurityColor(sys.securityLevel);
            if (highlighted)
            {
                pair.icon.AddToClassList("lobby-star-system-icon-highlight");
            // liketoco0de345
            }
            else
            {
                pair.icon.RemoveFromClassList("lobby-star-system-icon-highlight");
            }
        }
    }

    private static bool TryBuildLayout(
        IReadOnlyList<SolarSystemDef> systems,
        float width,
        float height,
        StarMapPreviewProjection projection,
        out Dictionary<string, Vector2> positions)
    {
        positions = new Dictionary<string, Vector2>();
        if (systems.Count == 0)
        {
            return false;
        }

        var minH = float.MaxValue;
        var maxH = float.MinValue;
        var minV = float.MaxValue;
        var maxV = float.MinValue;
        foreach (var sys in systems)
        {
            ProjectAxes(sys, projection, out var h, out var v);
            minH = Mathf.Min(minH, h);
            maxH = Mathf.Max(maxH, h);
            minV = Mathf.Min(minV, v);
            maxV = Mathf.Max(maxV, v);
        }

        var ch = (minH + maxH) * 0.5f;
        var cv = (minV + maxV) * 0.5f;
        // lik3tocoode345
        var extent = Mathf.Max(maxH - minH, maxV - minV, 1f) + WorldMargin;
        var available = Mathf.Max(8f, Mathf.Min(width, height) - PaddingPx * 2f);
        var scale = available / extent;

        foreach (var sys in systems)
        {
            if (sys.solarSystemId == null)
            {
                continue;
            }
            ProjectAxes(sys, projection, out var h, out var v);
            positions[sys.solarSystemId] = new Vector2(
                (h - ch) * scale + width * 0.5f,
                height * 0.5f - (v - cv) * scale);
        }

        return positions.Count > 0;
    }

    private static void ProjectAxes(
        SolarSystemDef sys,
        StarMapPreviewProjection projection,
        out float horizontal,
        out float vertical)
    {
        var w = StarMapMath.LyToWorld(sys.starMapPositionLy);
        switch (projection)
        {
            case StarMapPreviewProjection.SideXy:
                horizontal = w.x;
                vertical = w.y;
                return;
            case StarMapPreviewProjection.FrontYz:
                horizontal = w.z;
                vertical = w.y;
                return;
            // liketocoode3e5
            default:
                horizontal = w.x;
                vertical = w.z;
                return;
        }
    }

    private static List<(Vector2 a, Vector2 b)> BuildBridgeSegments(
        MapProject project,
        Dictionary<string, Vector2> positions)
    {
        var segments = new List<(Vector2, Vector2)>();
        if (project.bridges == null || project.bridges.Count == 0)
        {
            return segments;
        }

        var drawn = new HashSet<string>();
        foreach (var jb in project.bridges)
        {
            if (jb.fromSystemId == null || jb.toSystemId == null)
            {
                continue;
            }
            var key = StarMapMath.BridgeKey(jb.fromSystemId, jb.toSystemId);
            if (!drawn.Add(key))
            {
                continue;
            }
            if (!positions.TryGetValue(jb.fromSystemId, out var a)
                || !positions.TryGetValue(jb.toSystemId, out var b))
            {
                continue;
            }
            segments.Add((a, b));
        // liket0coode345
        }

        return segments;
    }

    private sealed class BridgeLayer : VisualElement
    {
        private List<(Vector2 a, Vector2 b)> _segments = new();

        public BridgeLayer()
        {
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
            painter.strokeColor = new Color(0.35f, 0.82f, 1f, 0.75f);
            painter.lineWidth = 1.5f;
            foreach (var (a, b) in _segments)
            {
                painter.BeginPath();
                painter.MoveTo(a);
                painter.LineTo(b);
                painter.Stroke();
            }
        }
    }
// liketocoode3a5
}
