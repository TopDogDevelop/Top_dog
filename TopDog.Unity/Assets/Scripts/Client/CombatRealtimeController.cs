using System;
using TopDog.AgentDiag;
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
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md · docs/TACTICAL_VIEW.md · docs/TACTICAL_WARP_AND_ORDERS.md · docs/MATCH_FLOW.md · docs/BATTLE_REPORT.md · docs/COMBAT_FX.md
 * 本文件: CombatRealtimeController.cs — 实时战术 UI 主控（视口/星图切换/战报/舰队底栏）
 * 【机制要点】
 * · 战术视口：TacticalViewportPresenter + TacticalPlaneOverlay（30~300km 环）
 * · CombatFx：独立 RT 叠在舰标下；混伤弹道 Drain；场域 UITK 投影盘（主验收）
 * · 星图切换：StarMapHostController 屏外标记边缘钳制
 * · combatAwaitingContinue 时底栏「继续」→ CombatContinue
 * · 战报浮层 BattleReportWindow 按吨位分组；选中摘要含舰载机/导弹归属
 * · 默认 autoFireEnabled=false 进入战场
 * · **交互层每帧** / **表现层节流+墙钟预算**（REALTIME_COMBAT_UNIFORM §4）
 * 【实现逻辑】
 * · OnCommandIssued：交互层即时刷新；重表现标脏延后
 * · TacticalRightRail 构造时注入 RefreshAll 供 ActivateDescentEntry 回调
 * 【关联】FleetCommandBar · GameSceneRouter · CombatPhaseService · TacticalRightRail · RealtimeInteractionFramePolicy
 * ══
 */

namespace TopDog.Client;

/// <summary>战斗视野（阶段 4 实时战术 UI）。</summary>
[DefaultExecutionOrder(-200)]
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
    private VisualElement _combatFxHost;
    private CombatFxCameraHost _combatFxCamera;
    private FieldAuraVfxPresenter _fieldAuraVfx;
    private InterdictionVfxPresenter? _interdictionVfx;
    private FieldAuraUitkPresenter? _fieldAuraUitk;
    private CombatFxTracerPresenter? _combatFxTracers;
    private Transform? _combatFxWorldRoot;
    private VisualElement? _markersHost;
    private float _lastCombatFxWallTime;
    private BattleReportWindow _battleReportWindow;
    private CombatSpaceBackgroundPresenter? _spaceBackground;
    private bool _openSystemInteriorOnStarMap;
    private ScrollView _combatDebugScroll;
    private Label _combatDebugLabel;
    private float _nextHeavyRefresh;
    private bool _heavyDirty = true;
    private float _nextCombatDebugRefresh;
    private float _nextAgentInteractionPerfLog;
    private float _nextAgentHeavyPerfLog;
    private string? _lastCombatDebugText;
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
        _markersHost = markersHost;
        if (markersHost != null)
        {
            markersHost.pickingMode = PickingMode.Ignore;
        }

        _combatFxHost = new VisualElement { name = "combat-fx-overlay" };
        _combatFxHost.AddToClassList("rtcombat-combat-fx-overlay");
        _combatFxHost.pickingMode = PickingMode.Ignore;
        _combatFxCamera = GetComponent<CombatFxCameraHost>()
                           ?? gameObject.AddComponent<CombatFxCameraHost>();
        var fxWorldGo = new GameObject("CombatFxWorld");
        fxWorldGo.transform.SetParent(transform, false);
        _combatFxWorldRoot = fxWorldGo.transform;

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
                var fxInsertIdx = _viewportHost.IndexOf(markersHost);
                if (fxInsertIdx >= 0)
                {
                    _viewportHost.Insert(Mathf.Max(0, fxInsertIdx), _combatFxHost);
                }
                else
                {
                    _viewportHost.Add(_combatFxHost);
                }

                _combatFxCamera.Bind(_viewportHost, _combatFxHost, _viewportCamera, _combatFxWorldRoot);
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
        ClientGameSettings.CombatFxEnabledChanged += OnCombatFxEnabledChanged;
        _lastCombatFxWallTime = Time.unscaledTime;
    }

    public void RefreshViewportNow() => RefreshAll();

    private void OnCombatViewSettingsChanged()
    {
        if (isActiveAndEnabled)
        {
            RefreshAll();
        }
    }

    private void OnCombatFxEnabledChanged()
    {
        if (!ClientGameSettings.CombatFxEnabled)
        {
            _combatFxTracers?.ClearAll();
            _combatFxCamera?.SetActive(false);
        }
        else if (isActiveAndEnabled)
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
            _combatFxHost,
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

            _combatFxCamera?.SetActive(false);
        }
        else
        {
            _openSystemInteriorOnStarMap = false;
            _spaceBackground?.SetActive(true);
            _combatFxCamera?.SetActive(true);
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
        _heavyDirty = true;
        _nextHeavyRefresh = 0f;
        RefreshInteractionLayer();
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
        ClientGameSettings.CombatFxEnabledChanged -= OnCombatFxEnabledChanged;
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

        var interactionStamp = RealtimeInteractionFramePolicy.BeginStamp();
        var core = GameAppHost.Instance?.Core;
        if (core != null && core.State.combatRealtimeActive)
        {
            if (_inputSource.TryPoll(out var sample))
            {
                _inputBridge?.Send(sample);
            }
        }

        // 交互层每帧：点选面 + 高亮 + 摘要（绝不 Clear 右栏、不追加重 UITK）
        RefreshInteractionLayer();
        // 弹道 UITK + 场域 3D 球壳：每帧推进（勿绑重表现时隙）
        // 仿真跳帧时跳过场域刷新，把主线程让给 Core.Tick
        TickCombatFxTracers(core);
        if (!RealtimeInteractionFramePolicy.ShouldSkipSimTick)
        {
            TickFieldAuraShell(core);
        }

        if (Time.unscaledTime >= _nextAgentInteractionPerfLog)
        {
            _nextAgentInteractionPerfLog = Time.unscaledTime + 1f;
            // #region agent log
            AgentSessionDebugLog.WriteDebugSession(
                "D3-D4",
                "CombatRealtimeController.cs:Update:interaction",
                "interaction presentation frame cost",
                new
                {
                    frame = Time.frameCount,
                    interactionMs = RealtimeInteractionFramePolicy.ElapsedMs(interactionStamp),
                    frameMs = Time.unscaledDeltaTime * 1000f,
                    simSkipPending = RealtimeInteractionFramePolicy.PendingSimSkipFrames,
                    viewMode = CombatViewModeState.Mode.ToString(),
                });
            // #endregion
        }

        // 重表现严格按时隙；若上帧仿真已要求跳帧，本帧继续只保交互
        if (RealtimeInteractionFramePolicy.ShouldSkipSimTick)
        {
            return;
        }

        if (Time.unscaledTime >= _nextHeavyRefresh)
        {
            var dense = BattlefieldScalePolicy.IsDense(ActiveBf(core?.State));
            var interval = dense
                ? RealtimeInteractionFramePolicy.DenseHeavyRefreshIntervalSec
                : RealtimeInteractionFramePolicy.SparseHeavyRefreshIntervalSec;
            _nextHeavyRefresh = Time.unscaledTime + interval;
            TryRefreshHeavyPresentation(force: false);
        }
    }

    /// <summary>场域壳：仅 3D 空心球壳（Layer29→RT）；禁止 UITK 2D 盘。</summary>
    private void TickFieldAuraShell(SimulationCore? core)
    {
        // 立场禁止 2D 投影盘；清掉历史 UITK 实例
        if (_fieldAuraUitk != null)
        {
            _fieldAuraUitk.ClearAll();
            _fieldAuraUitk = null;
        }

        if (core == null || !core.State.combatRealtimeActive)
        {
            _fieldAuraVfx?.Refresh(null, null, Vector3.zero);
            _interdictionVfx?.Refresh(null, null, Vector3.zero);
            return;
        }

        if (CombatViewModeState.Mode == CombatViewMode.StarMap)
        {
            _fieldAuraVfx?.Refresh(null, null, Vector3.zero);
            _interdictionVfx?.Refresh(null, null, Vector3.zero);
            return;
        }

        if (_fieldAuraVfx == null && _combatFxWorldRoot != null)
        {
            _fieldAuraVfx = new FieldAuraVfxPresenter(_combatFxWorldRoot, core.Ships, core.Modules);
        }

        if (_interdictionVfx == null && _combatFxWorldRoot != null)
        {
            _interdictionVfx = new InterdictionVfxPresenter(_combatFxWorldRoot);
        }

        _combatFxCamera?.PrepareWorldRootForFrame();
        var fxFocus = _combatFxCamera != null ? _combatFxCamera.CurrentFocusWorld : Vector3.zero;
        var activeBf = ActiveBf(core.State);
        _fieldAuraVfx?.Refresh(core.State, activeBf, fxFocus);
        _interdictionVfx?.Refresh(core.State, activeBf, fxFocus);
        // #region agent log
        if (Time.frameCount % 90 == 0)
        {
            var bf = ActiveBf(core.State);
            var n3d = _combatFxWorldRoot != null ? _combatFxWorldRoot.childCount : -1;
            CombatFxAgentLog.Write(
                "E",
                "CombatRealtimeController.TickFieldAuraShell",
                "tick",
                "{\"fxEnabled\":" + (ClientGameSettings.CombatFxEnabled ? "true" : "false")
                + ",\"worldChildren\":" + n3d
                + ",\"uitkOff\":true"
                + ",\"path\":\"3d-shell\""
                + ",\"bfT\":" + (bf != null ? bf.timeSec.ToString("F1") : "-1") + "}");
        }
        // #endregion
    }

    /// <summary>混伤弹道：每帧 Drain+推进，与舰标同宿主投影。</summary>
    private void TickCombatFxTracers(SimulationCore? core)
    {
        if (core == null || !core.State.combatRealtimeActive)
        {
            return;
        }

        if (CombatViewModeState.Mode == CombatViewMode.StarMap)
        {
            return;
        }

        var host = _markersHost ?? _combatFxHost;
        if (_combatFxTracers == null && host != null && _viewportCamera != null)
        {
            _combatFxTracers = new CombatFxTracerPresenter(
                host, _viewportCamera, core.Modules, core.Ships);
        }

        var now = Time.unscaledTime;
        var fxDt = Mathf.Max(0f, now - _lastCombatFxWallTime);
        _lastCombatFxWallTime = now;
        _combatFxTracers?.Refresh(core.State, ActiveBf(core.State), fxDt);
    }

    /// <summary>全量刷新（星图切换等）；内部拆成交互 + 表现。</summary>
    private void RefreshAll()
    {
        _heavyDirty = true;
        RefreshInteractionLayer();
        TryRefreshHeavyPresentation(force: true);
    }

    /// <summary>轻量：点选面 / 选中高亮 / 摘要 / 底栏；禁止全表右栏重建。</summary>
    private void RefreshInteractionLayer()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }

        var s = core.State;
        var bf = ActiveBf(s);
        // 点选依赖批画像素坐标：交互帧保证批画同步 + 选中稀疏 marker
        if (CombatViewModeState.Mode != CombatViewMode.StarMap && bf != null)
        {
            _viewportPresenter?.RefreshPickSurface(s, bf);
        }

        _rightRail?.RefreshInteractionOnly(s);
        _fleetBar?.RefreshGate(s);
        _unitHover?.Refresh(s, bf);
        RefreshHudChrome(s, bf);
    }

    private void TryRefreshHeavyPresentation(bool force)
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            SetStatus("模拟未启动");
            return;
        }

        var stamp = RealtimeInteractionFramePolicy.BeginStamp();
        var s = core.State;
        var bf = ActiveBf(s);
        var dense = BattlefieldScalePolicy.IsDense(bf);
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

        // 1) 舰标批画必须优先（点选 / 高亮依赖）
        if (CombatViewModeState.Mode != CombatViewMode.StarMap)
        {
            _spaceBackground?.Refresh(CombatSpaceBackgroundState.ActiveSetId);
            _viewportPresenter?.Refresh(s, bf);
            _planeOverlay?.Refresh(s, bf);
            _navMarker?.Refresh(s, bf);
        }

        // 场域球壳已在 TickFieldAuraShell 每帧刷新；此处只保证 FX 相机/RT 叠层开启
        var tactical = CombatViewModeState.Mode != CombatViewMode.StarMap && bf != null;
        _combatFxCamera?.PrepareWorldRootForFrame();
        _combatFxCamera?.SetActive(tactical && ClientGameSettings.CombatFxEnabled);

        if (!force
            && RealtimeInteractionFramePolicy.Exceeded(
                stamp, RealtimeInteractionFramePolicy.MaxHeavyUiWallMsPerFrame))
        {
            // #region agent log
            if (Time.frameCount % 120 == 0)
            {
                CombatFxAgentLog.Write(
                    "F",
                    "CombatRealtimeController.TryRefreshHeavyPresentation",
                    "budget-exit",
                    "{\"pendingFx\":" + (bf?.pendingCombatFx.Count ?? 0)
                    + ",\"pendingHp\":" + (bf?.pendingHpDeltas.Count ?? 0)
                    + ",\"dense\":" + (dense ? "true" : "false")
                    + ",\"ms\":" + RealtimeInteractionFramePolicy.ElapsedMs(stamp).ToString("F2") + "}");
            }
            // #endregion
            LogHeavyPresentationCost(stamp, force, dense, "viewport-budget-exit");
            _heavyDirty = true;
            return;
        }

        _floatingText?.Refresh(s, bf);

        // 2) 右栏：步进 / 虚拟窗只在重表现帧推进（交互帧只做高亮）
        if (force || _heavyDirty || !dense)
        {
            _rightRail?.Refresh(s);
        }
        else
        {
            _rightRail?.RefreshHeavyStep(s);
        }

        if (CombatViewModeState.Mode == CombatViewMode.StarMap && _starMap != null)
        {
            _starMap.SyncFromState(s);
            if (!_openSystemInteriorOnStarMap)
            {
                HighlightActiveBattlefieldSystem(s);
            }
        }

        if (Time.unscaledTime >= _nextCombatDebugRefresh)
        {
            _nextCombatDebugRefresh = Time.unscaledTime + 1f;
            RefreshCombatDebug(core);
        }

        _heavyDirty = false;
        LogHeavyPresentationCost(stamp, force, dense, "complete");
    }

    private void LogHeavyPresentationCost(float stamp, bool force, bool dense, string outcome)
    {
        if (Time.unscaledTime < _nextAgentHeavyPerfLog)
        {
            return;
        }

        _nextAgentHeavyPerfLog = Time.unscaledTime + 1f;
        // #region agent log
        AgentSessionDebugLog.WriteDebugSession(
            "D3",
            "CombatRealtimeController.cs:TryRefreshHeavyPresentation",
            "heavy presentation frame cost",
            new
            {
                frame = Time.frameCount,
                elapsedMs = RealtimeInteractionFramePolicy.ElapsedMs(stamp),
                frameMs = Time.unscaledDeltaTime * 1000f,
                force,
                dense,
                outcome,
            });
        // #endregion
    }

    private void RefreshHudChrome(GameState s, BattlefieldState? bf)
    {
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
            var selIds = TacticalSelectionState.GetSelectedFriendlyUnitIds();
            var selCount = selIds.Count;
            var fireOn = 0;
            var interOn = 0;
            if (bf != null && selCount > 0)
            {
                foreach (var id in selIds)
                {
                    var u = BattlefieldSystem.FindUnit(bf, id);
                    if (u == null || u.IsDestroyed())
                    {
                        continue;
                    }

                    if (FleetOrderService.EffectiveAutoFire(s, u))
                    {
                        fireOn++;
                    }

                    if (FleetOrderService.EffectiveAutoInterdiction(s, u))
                    {
                        interOn++;
                    }
                }
            }

            var fireSummary = selCount > 0
                ? $"自开火 {fireOn}/{selCount} 开"
                : (s.autoFireEnabled ? "自开火默认开" : "自开火默认关");
            var interSummary = selCount > 0
                ? $"自动拦截 {interOn}/{selCount} 开"
                : (s.fleetDefaultAutoInterdiction ? "自动拦截默认开" : "自动拦截默认关");
            _possessionLabel.text = "附身: " + (s.possessingMemberId ?? "无")
                + " · 框选=" + selCount
                + " · 默认距=" + (TacticalSelectionState.EffectiveDefaultCommandRangeKm <= 0.01f
                    ? "0km(不限)"
                    : TacticalSelectionState.EffectiveDefaultCommandRangeKm.ToString("0") + "km")
                + " · 油门=" + throttle
                + " · " + fireSummary
                + " · " + interSummary;
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
        var text = string.IsNullOrWhiteSpace(dump) ? "（无日志）" : dump;
        if (text == _lastCombatDebugText)
        {
            return;
        }

        _lastCombatDebugText = text;
        _combatDebugLabel.text = text;
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

    private static BattlefieldState? ActiveBf(GameState? s)
    {
        if (s?.activeBattlefieldId == null)
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
        // #region agent log
        try
        {
            var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
            var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"P\",\"location\":\"CombatRealtimeController.OnViewportContextCommand\",\"message\":\"rclick\",\"data\":{"
                       + "\"picked\":\"" + (picked ?? "") + "\""
                       + ",\"scope\":\"" + s.fleetCommandScope + "\""
                       + ",\"selN\":" + (TacticalSelectionState.GetSelectedFriendlyUnitIds()?.Count ?? 0)
                       + ",\"x\":" + overlayLocalPos.x.ToString("F0")
                       + ",\"y\":" + overlayLocalPos.y.ToString("F0")
                       + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(path, line);
        }
        catch
        {
        }
        // #endregion
        if (picked != null)
        {
            var unit = FindBfUnit(bf, picked);
            if (unit == null)
            {
                // #region agent log
                try
                {
                    var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                    var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"P\",\"location\":\"CombatRealtimeController.OnViewportContextCommand\",\"message\":\"rclick-unit-missing\",\"data\":{"
                               + "\"picked\":\"" + picked + "\"},\"timestamp\":"
                               + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(path, line);
                }
                catch
                {
                }
                // #endregion
                return;
            }

            if (unit.side == UnitSide.ENEMY && !unit.isBuilding)
            {
                // #region agent log
                try
                {
                    var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                    var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"P\",\"location\":\"CombatRealtimeController.OnViewportContextCommand\",\"message\":\"rclick-enemy-focus\",\"data\":{"
                               + "\"picked\":\"" + picked + "\"},\"timestamp\":"
                               + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(path, line);
                }
                catch
                {
                }
                // #endregion
                IssueFleetOrder(FleetOrderService.OrderFocus(
                    s, bf, picked, TacticalSelectionState.GetSelectedFriendlyUnitIds(),
                    GameAppHost.Instance?.Core?.Modules));
                return;
            }

            if (unit.side == UnitSide.FRIENDLY && !unit.isBuilding)
            {
                // #region agent log
                try
                {
                    var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                    var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"P\",\"location\":\"CombatRealtimeController.OnViewportContextCommand\",\"message\":\"rclick-friendly\",\"data\":{"
                               + "\"picked\":\"" + picked + "\""
                               + ",\"scope\":\"" + s.fleetCommandScope + "\""
                               + ",\"selN\":" + (TacticalSelectionState.GetSelectedFriendlyUnitIds()?.Count ?? 0)
                               + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(path, line);
                }
                catch
                {
                }
                // #endregion
                IssueFleetOrder(FleetOrderService.OrderRepairTarget(
                    s, bf, picked, TacticalSelectionState.GetSelectedFriendlyUnitIds(),
                    GameAppHost.Instance?.Core?.Modules));
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
            // #region agent log
            try
            {
                var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"P\",\"location\":\"CombatRealtimeController.OnViewportContextCommand\",\"message\":\"rclick-nav\",\"data\":{"
                           + "\"picked\":\"" + (picked ?? "") + "\""
                           + ",\"wx\":" + x.ToString("F0")
                           + ",\"wy\":" + y.ToString("F0")
                           + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                System.IO.File.AppendAllText(path, line);
            }
            catch
            {
            }
            // #endregion
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
