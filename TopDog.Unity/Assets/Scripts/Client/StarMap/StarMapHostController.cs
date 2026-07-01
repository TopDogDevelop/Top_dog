using System;
using System.Collections.Generic;
using TopDog.Client.Tactical;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md §着色解钩 · §星系 interior · §派遣/战场高亮
 * 本文件: StarMapHostController.cs — 战略星图 3D 轨道相机 + UI marker 层
 * 【机制要点】
 * · 战略/星系内景双模式；marker 投影 worldBound → ClampMarkerToViewport 屏外钳制
 * · StarMapMath.SystemFillColor + security 外环；派遣/选中高亮
 * · 合并 GameState.buildings 为内景 marker；活跃战场 systemId 联动
 * 【关联】StarMapMath · StarMapBadgeSync · TacticalIconCatalog · GameSceneRouter
 * ══
 */


// liketoc0de345
// liketocoode3a5
// liketocoode34e
namespace TopDog.Client.StarMap;

/// <summary>
/// Strategic star map compute layer: 3D orbit camera.
/// UI layer: transparent input overlay, projected bridge lines, system Buttons (see UI_TWO_LAYER.md).
/// </summary>
public sealed class StarMapHostController : MonoBehaviour, IViewportCameraCommands
{
    private enum ViewMode
    {
        Strategic,
        SystemInterior,
    }

    private const int StarMapLayer = 31;
    private const float OrbitStepPx = 28f;
    private const float PanStepPx = 24f;
    private const float ZoomStep = 0.35f;
    private const float AuToWorldScale = 2.5f;

    private VisualElement? _host;
    private VisualElement? _markerLayer;
    private VisualElement? _mapViewport;
    private StarMapViewportInputOverlay? _inputOverlay;
    private StarMapBridgeOverlayLayer? _bridgeOverlay;
    private Action<string>? _onSystemPicked;
    private Action<string>? _onEventRegionPicked;

    private Camera? _mapCamera;
    private StarMapOrbitCamera? _orbit;
    private StarMapBridgeRenderer? _bridges;

    private LoadedMap? _map;
    private bool _needsFrame;
    private bool _pendingFrameAll;
    private Dictionary<string, StarMapSystemBadge> _badges = new();
    private string? _dispatchTargetId;
    private string? _highlightedId;
    private GameState? _syncState;

    private ViewMode _viewMode = ViewMode.Strategic;
    private string? _interiorSystemId;
    private string? _selectedEventRegionId;

    private bool _active;
    private float _nextSync;
    private EventCallback<GeometryChangedEvent>? _geometryHandler;

    private readonly List<(Button btn, Action handler)> _markerClickHandlers = new();
    private readonly Dictionary<string, (Button btn, VisualElement icon, Label label)> _markers = new();

    private bool _mousePanActive;
    private bool _mouseOrbitActive;
    private Vector3 _lastMouseInput;

    private float _viewportPixelWidth;
    private float _viewportPixelHeight;

    public bool IsSystemInterior => _viewMode == ViewMode.SystemInterior;
    public string? InteriorSystemId => _interiorSystemId;

    // liketoc0de345

    public void Attach(VisualElement host, Action<string> onSystemPicked, Action<string>? onEventRegionPicked = null)
    {
        Detach();
        _host = host;
        _onSystemPicked = onSystemPicked;
        _onEventRegionPicked = onEventRegionPicked;
        _host.pickingMode = PickingMode.Ignore;
        if (!_host.ClassListContains("art-viewport-host"))
        {
            _host.AddToClassList("art-viewport-host");
        }

        _markerLayer = _host.Q<VisualElement>("star-map-markers");
        if (_markerLayer == null)
        {
            _markerLayer = new VisualElement { name = "star-map-markers" };
            _markerLayer.AddToClassList("ops-star-map-markers");
            _host.Add(_markerLayer);
        }
        _mapViewport = _markerLayer;
        _markerLayer.pickingMode = PickingMode.Position;

        EnsureInputOverlay();
        EnsureBridgeOverlay();

        _geometryHandler = _ => SyncCameraRect();
        _host.RegisterCallback<WheelEvent>(OnHostWheel);
        _host.RegisterCallback(_geometryHandler);

        EnsureWorldObjects();
        _inputOverlay?.SetOrbitCamera(_orbit);
        if (_map != null)
        {
            _bridges?.SetMap(null);
            RebuildMarkers();
        }
        _active = true;
        _pendingFrameAll = true;
        ArrangeOverlayZOrder();
    }

    public void Detach()
    {
        _active = false;
        if (_host != null && _geometryHandler != null)
        {
            _host.UnregisterCallback(_geometryHandler);
            _host.UnregisterCallback<WheelEvent>(OnHostWheel);
        }
        _geometryHandler = null;
        _host = null;
        _markerLayer = null;
        _mapViewport = null;
        _inputOverlay = null;
        _bridgeOverlay = null;
        ClearMarkerHandlers();
    }

    // li3etocoode345

    public void SetDispatchTarget(string? systemId)
    {
        _dispatchTargetId = systemId;
        UpdateMarkerLabels();
    }

    public void SetHighlightedSystem(string? systemId)
    {
        _highlightedId = systemId;
        UpdateMarkerLabels();
    }

    public void LoadPreviewMap(LoadedMap? map)
    {
        _map = map;
        _dispatchTargetId = null;
        _highlightedId = null;
        _badges = new Dictionary<string, StarMapSystemBadge>();
        ExitSystemInterior();
        _bridges?.SetMap(null);
        _needsFrame = true;
        if (_active && _host != null)
        {
            RebuildMarkers();
            _pendingFrameAll = true;
            _needsFrame = false;
        }
    }

    public void SyncFromState(GameState state)
    {
        _syncState = state;
        if (state.map != null && !ReferenceEquals(state.map, _map))
        {
            _map = state.map;
            ExitSystemInterior();
            _bridges?.SetMap(null);
            _needsFrame = true;
            RebuildMarkers();
        }
        _badges = StarMapBadgeSync.Build(state);
        var dispatch = ResolveDispatchSystemId(state);
        if (!string.IsNullOrEmpty(dispatch))
        {
            _dispatchTargetId = dispatch;
        }
        UpdateMarkerLabels();
        if (_needsFrame)
        {
            _pendingFrameAll = true;
            _needsFrame = false;
        }
    }

    private static string? ResolveDispatchSystemId(GameState state)
    {
        if (!string.IsNullOrEmpty(state.activeBattlefieldId))
        {
            foreach (var bf in state.battlefields)
            {
                if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(bf.systemId))
                {
                    return bf.systemId;
                }
            }
        }

        if (!string.IsNullOrEmpty(state.possessingMemberId))
        {
            foreach (var m in state.members)
            {
                if (!state.possessingMemberId.Equals(m.memberId, StringComparison.Ordinal))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(m.opsDeploySystemId))
                {
                    return m.opsDeploySystemId;
                }
                break;
            }
        }

        return state.currentSolarSystemId;
    }

    // liketocoode3a5

    public void FrameAll()
    {
        if (_orbit == null)
        {
            return;
        }
        if (_host != null && (_host.worldBound.width < 8f || _host.worldBound.height < 8f))
        {
            _pendingFrameAll = true;
            return;
        }
        _pendingFrameAll = false;
        DoFrameAll();
    }

    // liketocoode34e

    private void DoFrameAll()
    {
        if (_orbit == null)
        {
            return;
        }
        if (_viewMode == ViewMode.SystemInterior && _interiorSystemId != null)
        {
            ResetView();
            return;
        }
        var systems = _map?.Project.systems;
        if (systems != null && systems.Count > 0)
        {
            _orbit.FrameSystems(systems, _viewportPixelWidth, _viewportPixelHeight);
        }
        else
        {
            _orbit.FrameTopDownAt(Vector3.zero);
        }
    }

    public void EnterSystemInterior(string systemId)
    {
        if (_map?.Project.FindSystem(systemId) == null)
        {
            return;
        }
        var sys = _map!.Project.FindSystem(systemId)!;
        SystemInteriorPopulator.EnsureSystem(sys, _map.Project, systemId.GetHashCode());
        _viewMode = ViewMode.SystemInterior;
        _interiorSystemId = systemId;
        _selectedEventRegionId = null;
        _bridges?.SetMap(null);
        RebuildMarkers();
        ResetView();
    }

    public void ExitSystemInterior()
    {
        if (_viewMode == ViewMode.Strategic)
        {
            return;
        }
        _viewMode = ViewMode.Strategic;
        _interiorSystemId = null;
        _selectedEventRegionId = null;
        _bridges?.SetMap(null);
        RebuildMarkers();
    }

    public void ZoomIn() => _orbit?.ZoomBy(-ZoomStep);
    public void ZoomOut() => _orbit?.ZoomBy(ZoomStep);
    public void OrbitLeft() => _orbit?.OrbitBy(-OrbitStepPx, 0f);
    public void OrbitRight() => _orbit?.OrbitBy(OrbitStepPx, 0f);
    public void OrbitUp() => _orbit?.OrbitBy(0f, -OrbitStepPx);
    public void OrbitDown() => _orbit?.OrbitBy(0f, OrbitStepPx);
    public void PanLeft() => _orbit?.PanBy(-PanStepPx, 0f);
    public void PanRight() => _orbit?.PanBy(PanStepPx, 0f);
    public void PanUp() => _orbit?.PanBy(0f, PanStepPx);
    public void PanDown() => _orbit?.PanBy(0f, -PanStepPx);

    // liketocoo3e345

    public void ResetView()
    {
        if (_orbit == null)
        {
            return;
        }
        if (_viewMode == ViewMode.SystemInterior && _interiorSystemId != null)
        {
            var sys = _map?.Project.FindSystem(_interiorSystemId);
            if (sys == null)
            {
                return;
            }
            var starPos = SystemWorldPosition(sys);
            var starRegion = FindStarRegion(sys);
            if (starRegion?.anchorAu != null && starRegion.anchorAu.Length >= 3)
            {
                starPos += AuToWorld(starRegion.anchorAu);
            }
            _orbit.FrameTopDownAt(starPos, InteriorFrameRadius(sys));
            return;
        }

        DoFrameAll();
    }

    // liketoco0de345

    private void Update()
    {
        if (!_active || _host == null || _mapCamera == null)
        {
            return;
        }
        SyncCameraRect();
        UpdateMarkerPositions();
        UpdateBridgeOverlay();
        UiViewportControlBar.EnsureRaised(_host);
        PollViewportMouseInput();
        if (Time.unscaledTime >= _nextSync)
        {
            _nextSync = Time.unscaledTime + 1f;
            var core = GameAppHost.Instance?.Core?.State;
            if (core != null)
            {
                SyncFromState(core);
            }
        }
    }

    private void EnsureWorldObjects()
    {
        if (_mapCamera != null)
        {
            return;
        }
        var root = new GameObject("StarMapWorld");
        root.transform.SetParent(transform, false);
        root.layer = StarMapLayer;

        var camGo = new GameObject("StarMapCamera");
        camGo.transform.SetParent(root.transform, false);
        camGo.layer = StarMapLayer;
        _mapCamera = camGo.AddComponent<Camera>();
        _mapCamera.clearFlags = CameraClearFlags.SolidColor;
        _mapCamera.backgroundColor = new Color(0.06f, 0.07f, 0.11f, 1f);
        _mapCamera.cullingMask = 1 << StarMapLayer;
        _mapCamera.depth = 1;
        _mapCamera.fieldOfView = 67f;
        _orbit = new StarMapOrbitCamera(_mapCamera);

        var bridgeGo = new GameObject("StarMapBridges");
        bridgeGo.transform.SetParent(root.transform, false);
        bridgeGo.layer = StarMapLayer;
        _bridges = bridgeGo.AddComponent<StarMapBridgeRenderer>();
    }

    private void SyncCameraRect()
    {
        if (_mapCamera == null || _host == null)
        {
            return;
        }

        var viewport = _mapViewport ?? _host;
        if (!UiPanelCoords.TryWorldBoundToCameraPixelRect(viewport, out var pixelRect))
        {
            _mapCamera.enabled = false;
            return;
        }

        _mapCamera.enabled = true;
        _mapCamera.pixelRect = pixelRect;
        _mapCamera.aspect = pixelRect.width / Mathf.Max(1f, pixelRect.height);
        _viewportPixelWidth = pixelRect.width;
        _viewportPixelHeight = pixelRect.height;
        if (_pendingFrameAll)
        {
            DoFrameAll();
            _pendingFrameAll = false;
        }
    }

    // lik3tocoode345

    private void RebuildMarkers()
    {
        if (_markerLayer == null)
        {
            return;
        }
        ClearMarkerHandlers();
        _markerLayer.Clear();
        _markers.Clear();
        if (_viewMode == ViewMode.SystemInterior)
        {
            RebuildInteriorMarkers();
            return;
        }
        if (_map?.Project.systems == null)
        {
            return;
        }
        foreach (var sys in _map.Project.systems)
        {
            if (sys.solarSystemId == null)
            {
                continue;
            }
            AddSystemMarker(sys);
        }
    }

    private void RebuildInteriorMarkers()
    {
        if (_interiorSystemId == null || _map == null)
        {
            return;
        }
        var sys = _map.Project.FindSystem(_interiorSystemId);
        if (sys?.eventRegions == null)
        {
            return;
        }
        var basePos = SystemWorldPosition(sys);
        foreach (var er in sys.eventRegions)
        {
            if (er.eventRegionId == null)
            {
                continue;
            }
            var world = basePos + AuToWorld(er.anchorAu);
            var kindLabel = EventRegionKindLabel(er.kind);
            var presence = SkirmishPresenceOverlay.RegionTooltipSuffix(_syncState, er.eventRegionId);
            if (presence != null)
            {
                kindLabel += " · " + presence;
            }
            var title = !string.IsNullOrEmpty(er.name) ? er.name : er.eventRegionId;
            var selected = er.eventRegionId == _selectedEventRegionId;
            AddInteriorMarker(er.eventRegionId, world, title, kindLabel, er.kind, selected);
        }

        if (_syncState?.buildings != null)
        {
            foreach (var building in _syncState.buildings)
            {
                if (building.buildingId == null
                    || !string.Equals(building.solarSystemId, _interiorSystemId, StringComparison.Ordinal))
                {
                    continue;
                }
                if (!TryResolveBuildingWorld(sys, basePos, building, out var world))
                {
                    continue;
                }
                var title = !string.IsNullOrEmpty(building.displayName) ? building.displayName : building.buildingId;
                var kindLabel = BuildingKindLabel(building);
                var markerId = "bld:" + building.buildingId;
                var selected = markerId == _selectedEventRegionId
                    || building.eventRegionId == _selectedEventRegionId;
                AddInteriorMarker(markerId, world, title, kindLabel, regionKind: null, selected);
            }
        }
    }

    private void AddSystemMarker(SolarSystemDef sys)
    {
        if (_markerLayer == null || sys.solarSystemId == null)
        {
            return;
        }
        _badges.TryGetValue(sys.solarSystemId, out var badge);

        var btn = new Button();
        btn.AddToClassList("ops-star-system-btn");
        btn.pickingMode = PickingMode.Position;
        btn.style.position = Position.Absolute;
        btn.style.backgroundColor = StyleKeyword.Null;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;

        var wrap = new VisualElement();
        wrap.AddToClassList("ops-star-system-wrap");
        wrap.pickingMode = PickingMode.Ignore;

        var icon = new VisualElement();
        icon.AddToClassList("ops-star-system-icon");
        icon.pickingMode = PickingMode.Ignore;
        var color = StarMapMath.SystemFillColor(sys, badge, _dispatchTargetId, _highlightedId);
        icon.style.backgroundColor = color;
        ApplySecurityRing(icon, sys);
        if (sys.solarSystemId == _dispatchTargetId)
        {
            icon.AddToClassList("ops-star-system-dispatch");
        }
        wrap.Add(icon);

        var name = badge?.displayName ?? sys.name ?? sys.solarSystemId;
        var label = new Label(name);
        label.AddToClassList("ops-star-system-label");
        label.pickingMode = PickingMode.Ignore;
        wrap.Add(label);

        if (badge is { activeBattlefieldCount: > 0 })
        {
            var battle = new Label($"交战×{badge.activeBattlefieldCount}");
            battle.AddToClassList("ops-star-system-badge-battle");
            battle.pickingMode = PickingMode.Ignore;
            wrap.Add(battle);
        }
        if (badge is { playerBuildingCount: > 0 })
        {
            var sov = new Label($"己方×{badge.playerBuildingCount}");
            sov.AddToClassList("ops-star-system-badge-sov");
            sov.pickingMode = PickingMode.Ignore;
            wrap.Add(sov);
        }

        btn.Add(wrap);

        var id = sys.solarSystemId;
        Action handler = () => _onSystemPicked?.Invoke(id);
        btn.clicked += handler;
        _markerClickHandlers.Add((btn, handler));

        _markerLayer.Add(btn);
        _markers[id] = (btn, icon, label);
    }

    private void AddInteriorMarker(
        string regionId,
        Vector3 world,
        string title,
        string kindLabel,
        string? regionKind,
        bool selected)
    {
        if (_markerLayer == null)
        {
            return;
        }

        var isStar = EventRegionKinds.IsStar(regionKind);
        var btn = new Button();
        btn.AddToClassList("ops-star-system-btn");
        btn.userData = world;
        btn.pickingMode = PickingMode.Position;
        btn.style.position = Position.Absolute;
        btn.style.backgroundColor = StyleKeyword.Null;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;

        var wrap = new VisualElement();
        wrap.AddToClassList("ops-star-system-wrap");
        wrap.pickingMode = PickingMode.Ignore;

        var icon = new VisualElement();
        icon.AddToClassList("ops-star-system-icon");
        if (isStar)
        {
            icon.AddToClassList("ops-interior-star-icon");
        }
        icon.pickingMode = PickingMode.Ignore;
        var regionTex = TacticalIconCatalog.ResolveEventRegionIcon(regionKind);
        if (regionTex != null)
        {
            icon.style.backgroundImage = new StyleBackground(regionTex);
            icon.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            icon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
        }
        else
        {
            icon.style.backgroundImage = StyleKeyword.Null;
            icon.style.backgroundColor = isStar
                ? new Color(1f, 0.85f, 0.35f, 1f)
                : new Color(0.45f, 0.75f, 1f, 1f);
        }
        if (selected)
        {
            icon.AddToClassList("ops-star-system-dispatch");
        }
        wrap.Add(icon);

        var kind = new Label(kindLabel);
        kind.AddToClassList("ops-interior-kind-label");
        kind.pickingMode = PickingMode.Ignore;
        wrap.Add(kind);

        var label = new Label(title);
        label.AddToClassList("ops-star-system-label");
        label.pickingMode = PickingMode.Ignore;
        wrap.Add(label);

        btn.Add(wrap);

        Action handler = () =>
        {
            _selectedEventRegionId = regionId;
            RebuildInteriorMarkers();
            if (regionId.StartsWith("bld:", StringComparison.Ordinal))
            {
                _dispatchTargetId = _interiorSystemId;
                UpdateMarkerLabels();
            }
            _onEventRegionPicked?.Invoke(regionId);
        };
        btn.clicked += handler;
        _markerClickHandlers.Add((btn, handler));

        _markerLayer.Add(btn);
        _markers[regionId] = (btn, icon, label);
    }

    private void UpdateMarkerPositions()
    {
        if (_markerLayer == null)
        {
            return;
        }
        if (_viewMode == ViewMode.SystemInterior)
        {
            foreach (var kv in _markers)
            {
                var btn = kv.Value.btn;
                if (btn.userData is not Vector3 world)
                {
                    continue;
                }
                if (!TryWorldToLocal(world, out var local))
                {
                    btn.style.display = DisplayStyle.None;
                    continue;
                }
                btn.style.display = DisplayStyle.Flex;
                var r = StarMapMath.IconRadiusPx;
                btn.style.left = local.x - 40f;
                btn.style.top = local.y - r;
            }
            return;
        }
        if (_map?.Project.systems == null)
        {
            return;
        }

        StarMapMarkerRenderer.LayoutStrategicMarkers(
            _map.Project.systems,
            _markers,
            StrategicSystemPosition,
            TryWorldToLocal);

        foreach (var sys in _map.Project.systems)
        {
            if (sys.solarSystemId == null || !_markers.TryGetValue(sys.solarSystemId, out var pair))
            {
                continue;
            }
            _badges.TryGetValue(sys.solarSystemId, out var badge);
            ApplyStrategicMarkerStyle(pair.icon, sys, badge);
        }
    }

    private void ApplyStrategicMarkerStyle(VisualElement icon, SolarSystemDef sys, StarMapSystemBadge? badge)
    {
        icon.style.backgroundColor = StarMapMath.SystemFillColor(sys, badge, _dispatchTargetId, _highlightedId);
        ApplySecurityRing(icon, sys);
    }

    private void ApplySecurityRing(VisualElement icon, SolarSystemDef sys)
    {
        var ring = StarMapMath.SystemSecurityRingColor(sys.securityLevel, _map?.SecurityBands);
        icon.style.borderTopColor = ring;
        icon.style.borderBottomColor = ring;
        icon.style.borderLeftColor = ring;
        icon.style.borderRightColor = ring;
        icon.style.borderTopWidth = 2;
        icon.style.borderBottomWidth = 2;
        icon.style.borderLeftWidth = 2;
        icon.style.borderRightWidth = 2;
    }

    private void UpdateMarkerLabels()
    {
        if (_viewMode == ViewMode.SystemInterior || _map?.Project.systems == null)
        {
            return;
        }
        foreach (var sys in _map.Project.systems)
        {
            if (sys.solarSystemId == null || !_markers.TryGetValue(sys.solarSystemId, out var pair))
            {
                continue;
            }
            _badges.TryGetValue(sys.solarSystemId, out var badge);
            var name = badge?.displayName ?? sys.name ?? sys.solarSystemId;
            if (badge != null && badge.combatUnitTotal > 0)
            {
                name += $"\n友{badge.combatFriendlyCount}/敌{badge.combatEnemyCount} 共{badge.combatUnitTotal}";
            }

            pair.label.text = name;
            ApplyStrategicMarkerStyle(pair.icon, sys, badge);
            if (sys.solarSystemId == _dispatchTargetId)
            {
                pair.icon.AddToClassList("ops-star-system-dispatch");
            }
            else
            {
                pair.icon.RemoveFromClassList("ops-star-system-dispatch");
            }
        }
    }

    private bool TryWorldToLocal(Vector3 world, out Vector2 local)
    {
        local = default;
        if (_mapCamera == null || _host == null || _host.panel == null)
        {
            return false;
        }

        var layer = _mapViewport ?? _host;
        var layerBounds = layer.worldBound;
        var screen = _mapCamera.WorldToScreenPoint(world);
        if (screen.z < 0f)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }

        var panelPoint = UiPanelCoords.WorldScreenPointToPanel(_host.panel, screen);
        local = new Vector2(panelPoint.x - layerBounds.x, panelPoint.y - layerBounds.y);
        local = ClampMarkerToViewport(local, layerBounds.width, layerBounds.height);
        return true;
    }

    // liketocoode3e5

    private static Vector2 ClampMarkerToViewport(Vector2 p, float w, float h)
    {
        const float pad = 14f;
        if (w < pad * 2f || h < pad * 2f)
        {
            return p;
        }
        if (p.x >= pad && p.y >= pad && p.x <= w - pad && p.y <= h - pad)
        {
            return p;
        }

        var cx = w * 0.5f;
        var cy = h * 0.5f;
        var dx = p.x - cx;
        var dy = p.y - cy;
        if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f)
        {
            return new Vector2(cx, pad);
        }

        var t = float.MaxValue;
        if (dx > 0.01f)
        {
            t = Math.Min(t, (w - pad - cx) / dx);
        }
        if (dx < -0.01f)
        {
            t = Math.Min(t, (pad - cx) / dx);
        }
        if (dy > 0.01f)
        {
            t = Math.Min(t, (h - pad - cy) / dy);
        }
        if (dy < -0.01f)
        {
            t = Math.Min(t, (pad - cy) / dy);
        }
        t = Math.Max(0f, t);
        return new Vector2(cx + dx * t, cy + dy * t);
    }

    private bool TryProjectToLocal(Vector3 world, out Vector2 local)
    {
        local = default;
        if (_mapCamera == null || _host == null || _host.panel == null)
        {
            return false;
        }

        var layer = _mapViewport ?? _host;
        var layerBounds = layer.worldBound;
        var screen = _mapCamera.WorldToScreenPoint(world);
        if (screen.z < 0f)
        {
            return false;
        }

        var panelPoint = UiPanelCoords.WorldScreenPointToPanel(_host.panel, screen);
        local = new Vector2(panelPoint.x - layerBounds.x, panelPoint.y - layerBounds.y);
        return true;
    }

    private static bool ClipSegmentToRect(
        Vector2 a,
        Vector2 b,
        Rect rect,
        out Vector2 ca,
        out Vector2 cb)
    {
        ca = a;
        cb = b;
        var dx = b.x - a.x;
        var dy = b.y - a.y;
        var t0 = 0f;
        var t1 = 1f;
        bool Clip(float p, float q)
        {
            if (Mathf.Approximately(p, 0f))
            {
                return q >= 0f;
            }

            var r = q / p;
            if (p < 0f)
            {
                if (r > t1)
                {
                    return false;
                }
                if (r > t0)
                {
                    t0 = r;
                }
            }
            else
            {
                if (r < t0)
                {
                    return false;
                }
                if (r < t1)
                {
                    t1 = r;
                }
            }

            return true;
        }

        if (Clip(-dx, a.x - rect.xMin)
            && Clip(dx, rect.xMax - a.x)
            && Clip(-dy, a.y - rect.yMin)
            && Clip(dy, rect.yMax - a.y))
        {
            ca = new Vector2(a.x + t0 * dx, a.y + t0 * dy);
            cb = new Vector2(a.x + t1 * dx, a.y + t1 * dy);
            return t1 > t0;
        }

        return false;
    }

    private static Vector3 StrategicSystemPosition(SolarSystemDef sys) =>
        StarMapMath.LyToWorldStrategic(sys.starMapPositionLy);

    private Vector3 SystemWorldPosition(SolarSystemDef sys) =>
        _viewMode == ViewMode.Strategic
            ? StrategicSystemPosition(sys)
            : StarMapMath.LyToWorld(sys.starMapPositionLy);

    private static Vector3 AuToWorld(float[]? au)
    {
        if (au == null || au.Length < 3)
        {
            return Vector3.zero;
        }
        return new Vector3(au[0] * AuToWorldScale, au[1] * AuToWorldScale, au[2] * AuToWorldScale);
    }

    private static EventRegionDef? FindStarRegion(SolarSystemDef sys)
    {
        foreach (var er in sys.eventRegions)
        {
            if (EventRegionKinds.IsStar(er.kind))
            {
                return er;
            }
        }
        return sys.eventRegions.Count > 0 ? sys.eventRegions[0] : null;
    }

    private float InteriorFrameRadius(SolarSystemDef sys)
    {
        var basePos = SystemWorldPosition(sys);
        var maxR = 12f;
        foreach (var er in sys.eventRegions)
        {
            var w = basePos + AuToWorld(er.anchorAu);
            maxR = MathF.Max(maxR, Vector3.Distance(basePos, w) / AuToWorldScale + 4f);
        }
        return maxR;
    }

    private static string BuildingKindLabel(BuildingState building) =>
        BuildingService.LegionFortress.Equals(building.buildingType, StringComparison.Ordinal) ? "军团堡垒"
        : BuildingService.PersonalFortress.Equals(building.buildingType, StringComparison.Ordinal) ? "个人堡垒"
        : "建筑";

    private static bool TryResolveBuildingWorld(
        SolarSystemDef sys,
        Vector3 basePos,
        BuildingState building,
        out Vector3 world)
    {
        world = default;
        if (!string.IsNullOrEmpty(building.eventRegionId))
        {
            foreach (var er in sys.eventRegions)
            {
                if (building.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal))
                {
                    world = basePos + AuToWorld(er.anchorAu);
                    return true;
                }
            }
        }

        foreach (var er in sys.eventRegions)
        {
            if (EventRegionKinds.IsPlanet(er.kind))
            {
                world = basePos + AuToWorld(er.anchorAu);
                return true;
            }
        }

        world = basePos;
        return true;
    }

    private static string EventRegionKindLabel(string? kind) => kind switch
    {
        EventRegionKinds.Star => "恒星",
        EventRegionKinds.Planet => "行星",
        EventRegionKinds.OreBelt => "矿带",
        EventRegionKinds.PirateRally => "海盗集结",
        EventRegionKinds.LegionStructure => "军团建筑",
        EventRegionKinds.JumpBridge => "跳桥位",
        EventRegionKinds.DeployedStructure => "部署结构",
        _ => kind ?? "地点",
    };

    private void PollViewportMouseInput()
    {
        if (_orbit == null || _host?.panel == null)
        {
            return;
        }

        if (!TryPointerInViewport(out var panelPos))
        {
            _mousePanActive = false;
            _mouseOrbitActive = false;
            return;
        }

        if (IsPointerOverStarMapChrome(panelPos))
        {
            _mousePanActive = false;
            _mouseOrbitActive = false;
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            _mousePanActive = true;
            _lastMouseInput = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1))
        {
            _mousePanActive = false;
        }
        if (Input.GetMouseButtonDown(2))
        {
            _mouseOrbitActive = true;
            _lastMouseInput = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2))
        {
            _mouseOrbitActive = false;
        }

        if (_mousePanActive && Input.GetMouseButton(1))
        {
            var delta = (Vector2)Input.mousePosition - (Vector2)_lastMouseInput;
            _lastMouseInput = Input.mousePosition;
            if (delta.sqrMagnitude > 0.01f)
            {
                _orbit.PanBy(delta.x, -delta.y);
            }
        }
        else if (_mouseOrbitActive && Input.GetMouseButton(2))
        {
            var delta = (Vector2)Input.mousePosition - (Vector2)_lastMouseInput;
            _lastMouseInput = Input.mousePosition;
            if (delta.sqrMagnitude > 0.01f)
            {
                _orbit.OrbitBy(delta.x, delta.y);
            }
        }
    }

    private bool TryPointerInViewport(out Vector2 panelPos)
    {
        panelPos = default;
        if (_host == null || _host.panel == null)
        {
            return false;
        }

        panelPos = UiPanelCoords.ScreenBottomLeftToPanel(_host.panel, Input.mousePosition);
        var viewport = _mapViewport ?? _host;
        return viewport.worldBound.Contains(panelPos);
    }

    private bool IsPointerOverStarMapChrome(Vector2 panelPos)
    {
        if (_host?.panel == null)
        {
            return false;
        }

        var picked = _host.panel.Pick(panelPos);
        while (picked != null)
        {
            if (picked is Button)
            {
                return true;
            }
            if (picked == _host)
            {
                break;
            }
            picked = picked.parent;
        }

        return false;
    }

    private void OnHostWheel(WheelEvent evt)
    {
        if (_orbit == null)
        {
            return;
        }

        _orbit.ZoomByWheel(evt.delta.y, evt.delta.x);
        evt.StopPropagation();
    }

    private void EnsureInputOverlay()
    {
        if (_host == null)
        {
            return;
        }

        var slot = _host.Q<VisualElement>("star-map-input-overlay");
        if (_inputOverlay == null)
        {
            _inputOverlay = slot as StarMapViewportInputOverlay ?? new StarMapViewportInputOverlay();
            if (slot != null && slot != _inputOverlay)
            {
                var parent = slot.parent;
                var index = parent.IndexOf(slot);
                slot.RemoveFromHierarchy();
                parent.Insert(index, _inputOverlay);
            }
            else if (slot == null)
            {
                var markers = _markerLayer;
                var insertIndex = markers != null ? _host.IndexOf(markers) : _host.childCount;
                _host.Insert(insertIndex, _inputOverlay);
            }
        }

        _inputOverlay.SetOrbitCamera(_orbit);
        ArrangeOverlayZOrder();
    }

    private void ArrangeOverlayZOrder()
    {
        if (_host == null)
        {
            return;
        }

        _host.Q<VisualElement>("art-viewport-bg")?.SendToBack();

        if (_inputOverlay != null && _bridgeOverlay != null)
        {
            _inputOverlay.PlaceBehind(_bridgeOverlay);
        }
        else
        {
            _inputOverlay?.SendToBack();
        }

        if (_bridgeOverlay != null && _markerLayer != null)
        {
            _bridgeOverlay.PlaceBehind(_markerLayer);
        }

        _markerLayer?.BringToFront();
        _host.Q<VisualElement>("star-map-mode-bar")?.BringToFront();
        _host.Q<Label>("lbl-dispatch-target")?.BringToFront();

        var controls = _host.Q<VisualElement>("viewport-controls");
        if (controls == null)
        {
            foreach (var bar in _host.Query(className: "ops-viewport-controls").ToList())
            {
                controls = bar;
                break;
            }
        }
        if (controls != null)
        {
            controls.pickingMode = PickingMode.Position;
            controls.BringToFront();
            controls.Query<Button>().ForEach(btn => btn.pickingMode = PickingMode.Position);
        }
    }

    private void EnsureBridgeOverlay()
    {
        if (_host == null)
        {
            return;
        }

        var slot = _host.Q<VisualElement>("star-map-bridges-ui");
        if (_bridgeOverlay == null)
        {
            _bridgeOverlay = slot as StarMapBridgeOverlayLayer ?? new StarMapBridgeOverlayLayer();
            if (slot != null && slot != _bridgeOverlay)
            {
                var parent = slot.parent;
                var index = parent.IndexOf(slot);
                slot.RemoveFromHierarchy();
                parent.Insert(index, _bridgeOverlay);
            }
            else if (slot == null)
            {
                var insertIndex = _markerLayer != null ? _host.IndexOf(_markerLayer) : _host.childCount;
                _host.Insert(insertIndex, _bridgeOverlay);
            }
        }

        _bridgeOverlay.pickingMode = PickingMode.Ignore;
        if (_markerLayer != null)
        {
            _bridgeOverlay.PlaceBehind(_markerLayer);
        }
        ArrangeOverlayZOrder();
    }

    private void UpdateBridgeOverlay()
    {
        if (_bridgeOverlay == null || _viewMode != ViewMode.Strategic || _map?.Project == null)
        {
            _bridgeOverlay?.SetSegments(null);
            return;
        }

        var segments = new List<(Vector2 a, Vector2 b)>();
        if (_map.Project.bridges != null && _map.Project.bridges.Count > 0)
        {
            var drawn = new HashSet<string>();
            foreach (var jb in _map.Project.bridges)
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
                var aSys = _map.Project.FindSystem(jb.fromSystemId);
                var bSys = _map.Project.FindSystem(jb.toSystemId);
                if (aSys == null || bSys == null)
                {
                    continue;
                }
                if (!TryProjectToLocal(StrategicSystemPosition(aSys), out var a)
                    || !TryProjectToLocal(StrategicSystemPosition(bSys), out var b))
                {
                    continue;
                }

                var layer = _mapViewport ?? _host;
                var rect = new Rect(0f, 0f, layer.worldBound.width, layer.worldBound.height);
                if (ClipSegmentToRect(a, b, rect, out var ca, out var cb))
                {
                    segments.Add((ca, cb));
                }
            }
        }

        _bridgeOverlay.SetSegments(segments);
    }

    private void ClearMarkerHandlers()
    {
        foreach (var (btn, handler) in _markerClickHandlers)
        {
            if (btn != null)
            {
                btn.clicked -= handler;
            }
        }
        _markerClickHandlers.Clear();
    }

    // liket0coode345

    private void OnDestroy()
    {
        Detach();
    }
}
