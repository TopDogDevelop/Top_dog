using System;
using TopDog.App;
using TopDog.App.Brick;
using TopDog.Client.StarMap;
using TopDog.Client.Tactical;
using TopDog.Content;
using TopDog.Sim.Combat;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ⚠️ 背景链（CombatSpaceBackground* 接线 / OnCombatBackgroundSetChanged 等）：勿动，除非用户明确要求。
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md · docs/TACTICAL_VIEW.md · docs/TACTICAL_WARP_AND_ORDERS.md · docs/MATCH_FLOW.md · docs/BATTLE_REPORT.md
 * 本文件: CombatRealtimeController.cs — 实时战术 UI 主控（视口/星图切换/战报/舰队底栏）
 * 【机制要点】
 * · 战术视口：TacticalViewportPresenter + TacticalPlaneOverlay（30~300km 环）
 * · 星图切换：StarMapHostController 屏外标记边缘钳制
 * · combatAwaitingContinue 时底栏「继续」→ CombatContinue
 * · 战报浮层 BattleReportWindow 按吨位分组；选中摘要含舰载机/导弹归属
 * · 默认 autoFireEnabled=false 进入战场
 * 【实现逻辑】
 * · OnCommandIssued：SetStatus + InvalidateCaches + RefreshAll；不 RefreshSceneProxies/sync proxy
 * · TacticalRightRail 构造时注入 RefreshAll 供 ActivateDescentEntry 回调
 * 【关联】FleetCommandBar · GameSceneRouter · CombatPhaseService · TacticalRightRail
 * ══
 */

namespace TopDog.Client;

/// <summary>战斗视野（阶段 4 实时战术 UI）。</summary>
public sealed class CombatRealtimeController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.CombatRealtime;

    protected override bool UseSafeAreaInsets => false;

    private Label _timerLabel;
    private Label _statusLabel;
    private Label _overviewLabel;
    private Label _broadcastLabel;
    private Label _possessionLabel;
    private TacticalObjectCommandMenu _objectCommandMenu;
    private VisualElement _uiRoot;
    private TacticalRightRail _rightRail;
    private TacticalViewportPresenter _viewportPresenter;
    private TacticalViewportCamera _viewportCamera;
    private TacticalPlaneOverlay _planeOverlay;
    private TacticalNavMarkerPresenter _navMarker;
    private TacticalUnitHoverOverlay _unitHover;
    private TacticalViewportInputOverlay _inputOverlay;
    private FleetCommandBar _fleetBar;
    private VisualElement _viewportHost;
    private VisualElement _starMapHost;
    private VisualElement _fleetCommandBar;
    private Button _viewToggleBtn;
    private StarMapHostController _starMap;
    private readonly ITacticalInputSource _inputSource = new KeyboardTacticalInputSource();
    private PossessionInputBridge _inputBridge;
    private CombatFloatingTextPresenter _floatingText;
    private VisualElement _fieldAuraHost;
    private FieldAuraVfxCameraHost _fieldAuraCamera;
    private FieldAuraVfxPresenter _fieldAuraVfx;
    private Transform? _fieldAuraWorldRoot;
    private BattleReportWindow _battleReportWindow;
    private CombatSpaceBackgroundPresenter? _spaceBackground;
    private bool _openSystemInteriorOnStarMap;
    private ScrollView _combatDebugScroll;
    private Label _combatDebugLabel;
    private float _nextRefresh;
    private string? _lastFollowedBattlefieldId;
    private string? _starMapDispatchSystemId;
    private string? _starMapSelectedRegionId;
    private EventCallback<KeyDownEvent>? _keyHandler;

    protected override void Bind(VisualElement root)
    {
        _uiRoot = root;
        _timerLabel = root.Q<Label>("lbl-timer");
        _statusLabel = root.Q<Label>("lbl-status");
        _overviewLabel = root.Q<Label>("lbl-overview");
        _broadcastLabel = root.Q<Label>("lbl-broadcast");
        _possessionLabel = root.Q<Label>("lbl-possession");

        _viewportCamera = GetComponent<TacticalViewportCamera>()
                          ?? gameObject.AddComponent<TacticalViewportCamera>();
        _viewportCamera.ActiveBattlefieldProvider = () => ActiveBf(GameAppHost.Instance?.Core?.State);
        _viewportHost = root.Q<VisualElement>("tactical-viewport-host");
        _starMapHost = root.Q<VisualElement>("star-map-host");
        _fleetCommandBar = root.Q<VisualElement>("fleet-command-bar");
        _viewToggleBtn = root.Q<Button>("btn-view-toggle");
        var markersHost = root.Q<VisualElement>("tactical-markers");
        if (markersHost != null)
        {
            markersHost.pickingMode = PickingMode.Ignore;
        }

        _fieldAuraHost = new VisualElement { name = "field-aura-overlay" };
        _fieldAuraHost.AddToClassList("rtcombat-field-aura-overlay");
        _fieldAuraHost.pickingMode = PickingMode.Ignore;
        _fieldAuraCamera = GetComponent<FieldAuraVfxCameraHost>()
                           ?? gameObject.AddComponent<FieldAuraVfxCameraHost>();
        var fieldWorldGo = new GameObject("FieldAuraWorld");
        fieldWorldGo.transform.SetParent(transform, false);
        _fieldAuraWorldRoot = fieldWorldGo.transform;

        var artBg = root.Q<VisualElement>("art-viewport-bg");
        if (artBg != null && _viewportHost != null)
        {
            _spaceBackground = new CombatSpaceBackgroundPresenter(_viewportHost, artBg, _viewportCamera, this);
        }

        _planeOverlay = new TacticalPlaneOverlay(_viewportCamera);
        _navMarker = new TacticalNavMarkerPresenter(_viewportCamera);
        var grid = root.Q<VisualElement>("tactical-grid");
        if (_viewportHost != null)
        {
            if (grid != null)
            {
                _viewportHost.Insert(_viewportHost.IndexOf(grid) + 1, _planeOverlay);
                _viewportHost.Insert(_viewportHost.IndexOf(grid) + 2, _navMarker);
            }
            else
            {
                _viewportHost.Insert(1, _planeOverlay);
                _viewportHost.Insert(2, _navMarker);
            }
        }

        var edgeMarkersHost = new VisualElement { name = "tactical-edge-markers" };
        edgeMarkersHost.AddToClassList("rtcombat-markers");
        edgeMarkersHost.AddToClassList("rtcombat-edge-markers");
        edgeMarkersHost.pickingMode = PickingMode.Ignore;
        if (_viewportHost != null)
        {
            _viewportHost.Add(edgeMarkersHost);
        }
        _viewportPresenter = new TacticalViewportPresenter(markersHost, _viewportCamera, edgeMarkersHost);
        _unitHover = new TacticalUnitHoverOverlay(_viewportPresenter);
        if (_viewportHost != null && _navMarker != null)
        {
            var navIdx = _viewportHost.IndexOf(_navMarker);
            if (navIdx >= 0)
            {
                _viewportHost.Insert(navIdx + 1, _unitHover);
            }
            else
            {
                _viewportHost.Add(_unitHover);
            }
        }
        _floatingText = new CombatFloatingTextPresenter(markersHost, _viewportCamera);
        _objectCommandMenu = new TacticalObjectCommandMenu(
            root,
            () => GameAppHost.Instance != null ? GameAppHost.Instance.Core : null,
            OnCommandIssued,
            RefreshAll);
        _rightRail = new TacticalRightRail(
            root.Q<VisualElement>("right-rail") ?? root,
            _objectCommandMenu,
            RefreshAll);
        _fleetBar = new FleetCommandBar(
            root,
            () => GameAppHost.Instance != null ? GameAppHost.Instance.Core : null,
            SetStatus,
            OnCommandIssued);
        _inputBridge = new PossessionInputBridge(() => GameAppHost.Instance?.Session);

        _inputOverlay = new TacticalViewportInputOverlay();
        if (_viewportHost != null)
        {
            var markersIdx = markersHost != null ? _viewportHost.IndexOf(markersHost) : _viewportHost.childCount;
            _viewportHost.Insert(Mathf.Max(0, markersIdx), _inputOverlay);
            _inputOverlay.Bind(_viewportCamera, _viewportPresenter, RefreshAll, OnViewportUnitPicked, OnViewportContextCommand);
            RegisterTacticalWheel(_viewportHost);
            var viewportControls = root.Q<VisualElement>("viewport-controls");
            RegisterTacticalWheel(viewportControls);
            edgeMarkersHost?.BringToFront();
            _inputOverlay.BringToFront();
            root.Q<VisualElement>("viewport-controls")?.BringToFront();
            if (_viewportHost != null && markersHost != null)
            {
                var fieldAuraInsertIdx = _viewportHost.IndexOf(markersHost);
                if (fieldAuraInsertIdx >= 0)
                {
                    _viewportHost.Insert(Mathf.Max(0, fieldAuraInsertIdx), _fieldAuraHost);
                }
                else
                {
                    _viewportHost.Add(_fieldAuraHost);
                }

                _fieldAuraCamera.Bind(_viewportHost, _fieldAuraHost, _viewportCamera, _fieldAuraWorldRoot);
                _fieldAuraCamera.SetRenderMode(FieldAuraVfxCameraHost.FieldAuraRenderMode.CompositeOnSkybox);
                _spaceBackground?.SetFieldAuraPass(_fieldAuraCamera);
            }

            ArrangeCombatViewportLayers();
        }

        UiViewportControlBar.BindWithin(_viewportHost ?? root, root, _viewportCamera, RefreshAll);
        if (_starMapHost != null)
        {
            _starMap = GetComponent<StarMapHostController>() ?? gameObject.AddComponent<StarMapHostController>();
            _starMap.Attach(_starMapHost, OnCombatStarMapSystemPicked, OnCombatStarMapRegionPicked);
            UiViewportControlBar.BindWithin(_starMapHost, root, _starMap, RefreshAll);
            OnClick(root, "btn-enter-system", EnterStarMapSystemInterior);
            OnClick(root, "btn-exit-system", ExitStarMapSystemInterior);
            OnClick(root, "btn-rally-starmap", RallyAtStarMapSelection);
            OnClick(root, "btn-topbar-enter-system", EnterStarMapSystemInterior);
        }
        if (grid != null)
        {
            grid.pickingMode = PickingMode.Ignore;
        }
        UiViewportControlBar.EnsureRaised(root);

        TacticalSelectionState.SelectionChanged += OnSelectionChanged;
        TacticalSelectionState.RailModeChanged += () => _rightRail?.Refresh(GameAppHost.Instance?.Core?.State);
        CombatViewModeState.ModeChanged += ApplyViewMode;

        if (_viewToggleBtn != null)
        {
            _viewToggleBtn.clicked += () =>
            {
                if (CombatViewModeState.Mode == CombatViewMode.Tactical)
                {
                    _openSystemInteriorOnStarMap = false;
                }

                CombatViewModeState.Toggle();
            };
        }

        OnClick(root, "btn-system-interior", OpenActiveSystemInteriorMap);

        OnClick(root, "btn-battle-reports", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core == null)
            {
                return;
            }
            _battleReportWindow ??= new BattleReportWindow(root);
            _battleReportWindow.Show(core.State);
            SetStatus("战报");
        });

        OnClick(root, "btn-abort", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core == null)
            {
                return;
            }

            if (SkirmishBuildingRules.IsSkirmish(core.State))
            {
                GameAppHost.Instance?.EndCampaign();
                GameSceneRouter.Instance?.GoOutOfMatch();
                GetComponent<UiNavigator>()?.ShowWorldline();
                return;
            }

            core.State.combatRealtimeActive = false;
            core.SetPhase(GamePhase.COMBAT_PREP);
        });
        EnsureCombatDebugPanel(root);
        BindKeyboard(root);
        ApplyViewMode();
        var core = GameAppHost.Instance?.Core?.State;
        if (core?.combatRealtimeActive == true)
        {
            CombatSpaceBackgroundState.EnsureForBattlefield(core.activeBattlefieldId);
        }
        RefreshAll();
        ClientGameSettings.CombatViewFovChanged += OnCombatViewSettingsChanged;
        ClientGameSettings.CombatBackgroundResolutionChanged += OnCombatViewSettingsChanged;
        ClientGameSettings.CombatBackgroundSetChanged += OnCombatBackgroundSetChanged;
    }

    public void RefreshViewportNow() => RefreshAll();

    private void OnCombatViewSettingsChanged()
    {
        if (isActiveAndEnabled)
        {
            RefreshAll();
        }
    }

    private void OnCombatBackgroundSetChanged()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        CombatSpaceBackgroundState.ApplyClientPreference();
        _spaceBackground?.InvalidateAppliedSet();
        RefreshAll();
    }

    private void OnCombatStarMapSystemPicked(string systemId)
    {
        // liketoc0de345
        _starMapDispatchSystemId = systemId;
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }

        if (_starMap != null && _starMap.IsSystemInterior)
        {
            return;
        }

        var s = core.State;
        BattlefieldState? pick = null;
        foreach (var bf in s.battlefields)
        {
            if (bf.finished || bf.battlefieldId == null)
            {
                continue;
            }

            if (systemId == null || !systemId.Equals(bf.systemId, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (VisionGate.HasDirectBattlefieldView(s, bf.battlefieldId))
            {
                pick = bf;
                break;
            }
        }

        if (pick?.battlefieldId == null)
        {
            BrickDebugLog.Log("combat.starmap", "pick denied system=" + systemId);
            SetStatus("需情报员或附身方可切换该战场");
            return;
        }

        var msg = PossessionService.SwitchBattlefield(s, pick.battlefieldId);
        BrickDebugLog.Log("combat.starmap", "pick system=" + systemId + " → " + pick.battlefieldId);
        SetStatus(msg);
        CombatViewModeState.Set(CombatViewMode.Tactical);
    }

    private void OnCombatStarMapRegionPicked(string regionId)
    {
        _starMapSelectedRegionId = regionId;
    }

    private void EnterStarMapSystemInterior()
    {
        var bf = ActiveBf(GameAppHost.Instance?.Core?.State);
        var systemId = _starMapDispatchSystemId ?? bf?.systemId;
        if (string.IsNullOrEmpty(systemId))
        {
            SetStatus("请先点击星图选择星系");
            return;
        }

        var highlightRegion = _starMapSelectedRegionId ?? bf?.eventRegionId;
        _openSystemInteriorOnStarMap = true;
        if (CombatViewModeState.Mode != CombatViewMode.StarMap)
        {
            CombatViewModeState.Set(CombatViewMode.StarMap);
        }
        else
        {
            _starMap?.EnterSystemInterior(systemId, highlightRegion);
            SetStatus("星系内景 · " + systemId);
        }
    }

    private void ExitStarMapSystemInterior()
    {
        _starMap?.ExitSystemInterior();
        SetStatus("返回战略星图");
    }

    private void RallyAtStarMapSelection()
    {
        var core = GameAppHost.Instance?.Core;
        var state = core?.State;
        var bf = state != null ? ActiveBf(state) : null;
        if (core == null || state == null || bf == null)
        {
            SetStatus("无活动战场");
            return;
        }

        RallyAnchor anchor;
        if (_starMap != null && _starMap.IsSystemInterior && _starMap.InteriorSystemId != null)
        {
            var regionId = _starMap.SelectedEventRegionId ?? bf.eventRegionId;
            if (string.IsNullOrEmpty(regionId))
            {
                SetStatus("请先选择星系内场景");
                return;
            }

            var landingM = TacticalWarpLandingService.ResolveLandingDistM(state);
            anchor = RallyNavigationPlanner.ResolveSceneAnchor(
                state,
                _starMap.InteriorSystemId,
                regionId,
                landingM);
        }
        else if (!string.IsNullOrEmpty(_starMapDispatchSystemId))
        {
            anchor = RallyNavigationPlanner.ResolveSystemAnchor(_starMapDispatchSystemId);
        }
        else
        {
            SetStatus("请先点击星图选择集结目标");
            return;
        }

        var msg = FleetOrderService.RallyToAnchor(state, bf, anchor);
        SetStatus(msg);
        OnCommandIssued(msg, msg.StartsWith("已", System.StringComparison.Ordinal));
    }

    private void OpenActiveSystemInteriorMap()
    {
        var core = GameAppHost.Instance?.Core;
        var s = core?.State;
        if (s == null)
        {
            return;
        }

        var bf = ActiveBf(s);
        if (bf?.systemId == null)
        {
            SetStatus("无活动星系");
            return;
        }

        _openSystemInteriorOnStarMap = true;
        if (CombatViewModeState.Mode != CombatViewMode.StarMap)
        {
            CombatViewModeState.Set(CombatViewMode.StarMap);
        }
        else
        {
            ApplyStarMapSubview(s);
        }

        SetStatus("星系内景 · " + (bf.eventRegionId ?? bf.systemId));
    }

    private void ApplyStarMapSubview(GameState s)
    {
        if (_starMap == null)
        {
            return;
        }

        _starMap.SyncFromState(s);
        if (_openSystemInteriorOnStarMap)
        {
            var bf = ActiveBf(s);
            if (bf?.systemId != null)
            {
                if (_starMap.IsSystemInterior
                    && string.Equals(_starMap.InteriorSystemId, bf.systemId, StringComparison.Ordinal))
                {
                    return;
                }

                _starMap.EnterSystemInterior(
                    bf.systemId,
                    _starMapSelectedRegionId ?? bf.eventRegionId);
            }
        }
        else
        {
            _starMap.ExitSystemInterior();
            HighlightActiveBattlefieldSystem(s);
            _starMap.FrameAll();
        }
    }

    private void ArrangeCombatViewportLayers()
    {
        if (_viewportHost == null)
        {
            return;
        }

        var artBg = _viewportHost.Q<VisualElement>("art-viewport-bg");
        var grid = _viewportHost.Q<VisualElement>("tactical-grid");
        var markers = _viewportHost.Q<VisualElement>("tactical-markers");
        var edge = _viewportHost.Q<VisualElement>("tactical-edge-markers");
        var joystick = _viewportHost.Q<VisualElement>("virtual-joystick-host");
        var controls = _viewportHost.Q<VisualElement>("viewport-controls");

        VisualElement?[] backToFront =
        {
            artBg,
            grid,
            _planeOverlay,
            _navMarker,
            _fieldAuraHost,
            markers,
            edge,
            _unitHover,
            _inputOverlay,
            joystick,
            controls,
        };
        foreach (var layer in backToFront)
        {
            if (layer?.hierarchy.parent == _viewportHost)
            {
                layer.BringToFront();
            }
        }
    }

    private void ApplyViewMode()
    {
        // liketocoode3a5
        var starMap = CombatViewModeState.Mode == CombatViewMode.StarMap;
        if (_viewportHost != null)
        {
            _viewportHost.style.display = starMap ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_starMapHost != null)
        {
            _starMapHost.style.display = starMap ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_fleetCommandBar != null)
        {
            _fleetCommandBar.style.opacity = starMap ? 0.85f : 1f;
        }
        if (_viewToggleBtn != null)
        {
            _viewToggleBtn.text = starMap ? "战斗视野" : "星图";
        }
        if (starMap)
        {
            _spaceBackground?.SetActive(false);
            var core = GameAppHost.Instance?.Core?.State;
            if (core != null && _starMap != null)
            {
                ApplyStarMapSubview(core);
            }

            var modeBar = _uiRoot?.Q<VisualElement>("star-map-mode-bar");
            modeBar?.BringToFront();
            var topEnter = _uiRoot?.Q<Button>("btn-topbar-enter-system");
            if (topEnter != null)
            {
                topEnter.style.display = _openSystemInteriorOnStarMap ? DisplayStyle.None : DisplayStyle.Flex;
            }

            _fieldAuraCamera?.SetActive(true);
        }
        else
        {
            _openSystemInteriorOnStarMap = false;
            _spaceBackground?.SetActive(true);
            _fieldAuraCamera?.SetActive(false);
            var topEnter = _uiRoot?.Q<Button>("btn-topbar-enter-system");
            if (topEnter != null)
            {
                topEnter.style.display = DisplayStyle.None;
            }
        }
    }

    private void HighlightActiveBattlefieldSystem(GameState s)
    {
        // liketocoode34e
        if (_starMap == null || s.activeBattlefieldId == null)
        {
            _starMap?.SetHighlightedSystem(null);
            return;
        }

        foreach (var bf in s.battlefields)
        {
            if (s.activeBattlefieldId.Equals(bf.battlefieldId, System.StringComparison.Ordinal))
            {
                _starMap.SetHighlightedSystem(bf.systemId);
                return;
            }
        }

        _starMap.SetHighlightedSystem(null);
    }

    private void OnCommandIssued(string msg, bool success)
    {
        // liketoco0de3e5
        SetStatus(msg);
        _rightRail?.InvalidateCaches();
        RefreshAll();
        if (success)
        {
            var ids = FleetOrderService.LastAcknowledgedUnitIds;
            if (ids.Count > 0)
            {
                _viewportPresenter?.FlashCommandAck(ids);
            }
            else
            {
                _viewportPresenter?.FlashCommandAck(TacticalSelectionState.GetSelectedFriendlyUnitIds());
            }
        }
    }

    private void IssueFleetOrder(string msg) =>
        OnCommandIssued(msg, msg.StartsWith("已下令", System.StringComparison.Ordinal));

    private void BindKeyboard(VisualElement root)
    {
        root.focusable = true;
        _keyHandler = evt =>
        {
            if (evt.keyCode == KeyCode.Space)
            {
                var core = GameAppHost.Instance?.Core;
                TacticalSelectionState.ClearTargetAndBoxSelection();
                if (core != null)
                {
                    core.State.fleetCommandScope = FleetCommandScope.AllInScene;
                    core.State.tacticalNavVisible = false;
                }

                RefreshAll();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }
            MatchPauseOverlay.TryHandleEscape(root);
            evt.StopPropagation();
        };
        root.RegisterCallback(_keyHandler);
    }

    protected override void OnDisable()
    {
        if (Root != null && _keyHandler != null)
        {
            Root.UnregisterCallback(_keyHandler);
        }
        _keyHandler = null;
        TacticalSelectionState.SelectionChanged -= OnSelectionChanged;
        CombatViewModeState.ModeChanged -= ApplyViewMode;
        CombatSpaceBackgroundState.Reset();
        ClientGameSettings.CombatViewFovChanged -= OnCombatViewSettingsChanged;
        ClientGameSettings.CombatBackgroundResolutionChanged -= OnCombatViewSettingsChanged;
        ClientGameSettings.CombatBackgroundSetChanged -= OnCombatBackgroundSetChanged;
        // UI 重建/切屏会 OnDisable，不能结束会话，否则整场齐射/场域日志丢失
        if (CombatTelemetrySessionExport.IsActive)
        {
            CombatTelemetrySessionExport.Flush();
            var stillFighting = GameAppHost.Instance?.Core?.State.combatRealtimeActive == true;
            if (!stillFighting)
            {
                var path = CombatTelemetrySessionExport.End("ui.disable");
                if (!string.IsNullOrEmpty(path))
                {
                    Debug.Log("TopDog combat export → " + path);
                }
            }
        }
        base.OnDisable();
    }

    private void OnSelectionChanged()
    {
        _rightRail?.RefreshSelectionHighlight();
    }

    private void RegisterTacticalWheel(VisualElement? element)
    {
        if (element == null || _viewportCamera == null)
        {
            return;
        }

        element.RegisterCallback<WheelEvent>(evt =>
        {
            if (_viewportCamera == null)
            {
                return;
            }
            if (evt.delta.y < 0)
            {
                _viewportCamera.ZoomIn();
            }
            else if (evt.delta.y > 0)
            {
                _viewportCamera.ZoomOut();
            }
            RefreshAll();
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);
    }

    private void Update()
    {
        // liketocoo3e345
        if (!isActiveAndEnabled)
        {
            return;
        }

        var core = GameAppHost.Instance?.Core;
        if (core != null && core.State.combatRealtimeActive)
        {
            if (_inputSource.TryPoll(out var sample))
            {
                _inputBridge?.Send(sample);
            }
        }

        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.08f;
            RefreshAll();
        }
    }

    private void RefreshAll()
    {
        // l1ketocoode345
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            SetStatus("模拟未启动");
            return;
        }
        var s = core.State;
        var bf = ActiveBf(s);
        if (s.combatRealtimeActive && string.IsNullOrEmpty(CombatSpaceBackgroundState.ActiveSetId))
        {
            CombatSpaceBackgroundState.EnsureForBattlefield(s.activeBattlefieldId);
        }

        if (s.activeBattlefieldId != _lastFollowedBattlefieldId)
        {
            _lastFollowedBattlefieldId = s.activeBattlefieldId;
            CombatSpaceBackgroundState.EnsureForBattlefield(s.activeBattlefieldId);
            if (bf != null && CombatViewModeState.Mode != CombatViewMode.StarMap)
            {
                _viewportCamera?.EnterBattlefieldTopDown();
            }
        }

        if (CombatViewModeState.Mode != CombatViewMode.StarMap)
        {
            _spaceBackground?.Refresh(CombatSpaceBackgroundState.ActiveSetId);
        }

        _rightRail?.Refresh(s);
        _planeOverlay?.Refresh(s, bf);
        _navMarker?.Refresh(s, bf);
        _unitHover?.Refresh(s, bf);
        _viewportPresenter?.Refresh(s, bf);
        _floatingText?.Refresh(s, bf);
        if (_fieldAuraVfx == null && core != null && _fieldAuraWorldRoot != null)
        {
            _fieldAuraVfx = new FieldAuraVfxPresenter(_fieldAuraWorldRoot, core.Ships, core.Modules);
        }
        _fieldAuraCamera?.PrepareWorldRootForFrame();
        _fieldAuraVfx?.Refresh(s, bf, _fieldAuraCamera?.CurrentFocusWorld ?? Vector3.zero);
        _fieldAuraCamera?.SetActive(CombatViewModeState.Mode != CombatViewMode.StarMap && bf != null);
        _fleetBar?.RefreshGate(s);
        if (CombatViewModeState.Mode == CombatViewMode.StarMap && _starMap != null)
        {
            _starMap.SyncFromState(s);
            if (!_openSystemInteriorOnStarMap)
            {
                HighlightActiveBattlefieldSystem(s);
            }
        }

        if (_timerLabel != null)
        {
            var modeLabel = CombatViewModeState.Mode == CombatViewMode.StarMap
                ? (_openSystemInteriorOnStarMap ? "星系内景" : "战略星图")
                : "战斗视野";
            var vision = s.spectatorMode || s.spectatorFullVision
                ? "观战·全场景"
                : "附身 " + (s.possessingMemberId ?? "无");
            var t = bf != null ? $"T={bf.timeSec:0}s" : "";
            var dist = _viewportCamera != null ? $" dist={_viewportCamera.ViewDistance:0}m" : "";
            var guest = GameAppHost.Instance?.NetworkGuest == true ? "联机客 · " : "";
            var warpHint = FormatWarpEtaHint(s, bf);
            var linkHint = CombatRealtimeLinkService.IsHandshakeFrozen(s)
                ? " · 建立战场连接…"
                : "";
            _timerLabel.text = guest + modeLabel + " · " + vision + " " + t + dist + warpHint + linkHint;
        }
        if (_possessionLabel != null)
        {
            var possessed = FindPossessedUnit(bf, s.possessingMemberId);
            var throttle = possessed?.throttleOn == true ? "开" : "关";
            var selCount = TacticalSelectionState.GetSelectedFriendlyUnitIds().Count;
            _possessionLabel.text = "附身: " + (s.possessingMemberId ?? "无")
                + " · 框选=" + selCount
                + " · 默认距=" + (TacticalSelectionState.DefaultCommandRangeKm.HasValue
                    ? TacticalSelectionState.DefaultCommandRangeKm.Value.ToString("0") + "km"
                    : "无")
                + " · 油门=" + throttle
                + " · 自开火=" + (s.autoFireEnabled ? "开" : "关");
        }
        if (_overviewLabel != null)
        {
            _overviewLabel.text = FormatSelectionSummary(bf);
        }
        if (_broadcastLabel != null)
        {
            _broadcastLabel.text = s.alertLog.Count > 0
                ? s.alertLog[^1]
                : "播报";
        }
        if (string.IsNullOrEmpty(_statusLabel?.text))
        {
            SetStatus(s.combatRealtimeActive
                ? (s.combatAwaitingContinue ? "战果待确认 · 点继续" : "实时 sim · WASD艏向 空格油门 · 左键框选")
                : "非实时状态");
        }
        RefreshCombatDebug(core);
    }

    private void EnsureCombatDebugPanel(VisualElement root)
    {
        var status = root.Q<Label>("lbl-status");
        if (status == null)
        {
            return;
        }

        var panel = new VisualElement();
        panel.AddToClassList("rtcombat-combat-debug");
        var header = new Label("战斗诊断（CombatTelemetryLog）");
        header.AddToClassList("rtcombat-subtitle");
        panel.Add(header);
        _combatDebugScroll = new ScrollView();
        _combatDebugScroll.style.maxHeight = 100;
        _combatDebugLabel = new Label("（无日志）");
        _combatDebugLabel.AddToClassList("rtcombat-combat-debug-body");
        _combatDebugScroll.Add(_combatDebugLabel);
        panel.Add(_combatDebugScroll);
        var parent = status.parent;
        if (parent != null)
        {
            parent.Insert(parent.IndexOf(status) + 1, panel);
        }
    }

    private void RefreshCombatDebug(SimulationCore? core)
    {
        if (_combatDebugLabel == null)
        {
            return;
        }
        var dump = core?.DumpCombatDebug();
        _combatDebugLabel.text = string.IsNullOrWhiteSpace(dump) ? "（无日志）" : dump;
    }

    private static string FormatWarpEtaHint(GameState s, BattlefieldState? bf)
    {
        if (bf == null)
        {
            return "";
        }

        var u = FindPossessedUnit(bf, s.possessingMemberId);
        if (u == null || (!u.inTacticalWarp && u.warpPhase == TacticalWarpPhase.None))
        {
            foreach (var candidate in bf.units)
            {
                if (!candidate.alive || candidate.memberId == null)
                {
                    continue;
                }

                if (!candidate.inTacticalWarp && candidate.warpPhase == TacticalWarpPhase.None)
                {
                    continue;
                }

                if (candidate.side != UnitSide.FRIENDLY)
                {
                    continue;
                }

                u = candidate;
                break;
            }
        }

        return u == null ? "" : TacticalWarpEtaEstimator.FormatRemainingLabel(u);
    }

    private static BattlefieldUnit? FindPossessedUnit(BattlefieldState? bf, string? memberId)
    {
        // liketocoode3e5
        if (bf == null || memberId == null)
        {
            return null;
        }
        foreach (var u in bf.units)
        {
            if (memberId.Equals(u.memberId, System.StringComparison.Ordinal))
            {
                return u;
            }
        }
        return null;
    }

    private static BattlefieldState? ActiveBf(GameState s)
    {
        if (s.activeBattlefieldId == null)
        {
            return null;
        }
        foreach (var bf in s.battlefields)
        {
            if (s.activeBattlefieldId.Equals(bf.battlefieldId, System.StringComparison.Ordinal))
            {
                return bf;
            }
        }
        return null;
    }

    private static BattlefieldUnit? FindBfUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, System.StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    private void OnViewportContextCommand(Vector2 overlayLocalPos, int button)
    {
        if (button != 1)
        {
            return;
        }

        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }

        var s = core.State;
        var bf = ActiveBf(s);
        if (bf == null)
        {
            return;
        }

        s.fleetCommandScope = TacticalSelectionState.CommandScope;
        var picked = _viewportPresenter?.PickUnitAt(overlayLocalPos);
        if (picked != null)
        {
            var unit = FindBfUnit(bf, picked);
            if (unit == null)
            {
                return;
            }

            if (unit.side == UnitSide.ENEMY && !unit.isBuilding)
            {
                IssueFleetOrder(FleetOrderService.OrderFocus(
                    s, bf, picked, TacticalSelectionState.GetSelectedFriendlyUnitIds(),
                    GameAppHost.Instance?.Core?.Modules));
                return;
            }

            if (unit.side == UnitSide.FRIENDLY && !unit.isBuilding)
            {
                IssueFleetOrder(FleetOrderService.OrderRepairTarget(
                    s, bf, picked, TacticalSelectionState.GetSelectedFriendlyUnitIds()));
                return;
            }

            if (unit.isBuilding || BattlefieldSceneProxyService.IsSceneProxy(unit))
            {
                TacticalSelectionState.SetSelectedTarget(picked);
                IssueFleetOrder(FleetOrderService.OrderEnterBuilding(
                    s, bf, picked, TacticalSelectionState.GetSelectedFriendlyUnitIds()));
                return;
            }
        }

        var w = _inputOverlay?.contentRect.width ?? 0f;
        var h = _inputOverlay?.contentRect.height ?? 0f;
        if (_viewportCamera != null
            && TacticalScreenRaycast.TryRaycastFocusPlane(_viewportCamera, s, bf, overlayLocalPos, w, h, out var x, out var y, out var z))
        {
            IssueFleetOrder(FleetOrderService.OrderNavigateToPoint(
                s, bf, x, y, z, TacticalSelectionState.GetSelectedFriendlyUnitIds()));
        }
    }

    private void OnViewportUnitPicked(Vector2 overlayLocalPos, string unitId)
    {
        if (_uiRoot == null || _inputOverlay == null)
        {
            return;
        }

        var anchorLocal = _viewportPresenter != null
            && _viewportPresenter.TryGetUnitScreenCenter(unitId, out var center)
            ? center
            : overlayLocalPos;
        var world = _inputOverlay.LocalToWorld(anchorLocal);
        _objectCommandMenu.ShowAtWorld(world, unitId);
    }

    private static string FormatSelectionSummary(BattlefieldState? bf)
    {
        // liketoco0de345
        var id = TacticalSelectionState.SelectedTargetUnitId;
        if (id == null || bf == null)
        {
            return "未选中目标";
        }
        foreach (var u in bf.units)
        {
            if (id.Equals(u.unitId, System.StringComparison.Ordinal))
            {
                var side = u.side == UnitSide.ENEMY ? "敌对" : "友方";
                var tonnage = DisplayLabels.TonnageBilingual(u.tonnageClass);
                var owner = "";
                if (u.parentUnitId != null)
                {
                    foreach (var p in bf.units)
                    {
                        if (u.parentUnitId.Equals(p.unitId, System.StringComparison.Ordinal))
                        {
                            owner = " · 归属 " + (p.displayName ?? p.unitId);
                            break;
                        }
                    }
                    if (owner.Length == 0)
                    {
                        owner = " · 归属 " + u.parentUnitId;
                    }
                }
                return $"选中: {u.displayName} · {tonnage} · {side}{owner} · {u.SpeedMps():0} m/s";
            }
        }
        return "未选中目标";
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }
}
