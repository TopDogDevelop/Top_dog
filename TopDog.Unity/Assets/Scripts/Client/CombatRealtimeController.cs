using TopDog.App;
using TopDog.App.Brick;
using TopDog.Client.StarMap;
using TopDog.Client.Tactical;
using TopDog.Content;
using TopDog.Content.Traits;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

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
    private TacticalRightRail _rightRail;
    private TacticalViewportPresenter _viewportPresenter;
    private TacticalViewportCamera _viewportCamera;
    private TacticalPlaneOverlay _planeOverlay;
    private TacticalViewportInputOverlay _inputOverlay;
    private FleetCommandBar _fleetBar;
    private VisualElement _skillBar;
    private VisualElement _viewportHost;
    private VisualElement _starMapHost;
    private VisualElement _fleetCommandBar;
    private Button _viewToggleBtn;
    private StarMapHostController _starMap;
    private readonly ITacticalInputSource _inputSource = new KeyboardTacticalInputSource();
    private PossessionInputBridge _inputBridge;
    private float _nextRefresh;
    private EventCallback<KeyDownEvent>? _keyHandler;

    protected override void Bind(VisualElement root)
    {
        _timerLabel = root.Q<Label>("lbl-timer");
        _statusLabel = root.Q<Label>("lbl-status");
        _overviewLabel = root.Q<Label>("lbl-overview");
        _broadcastLabel = root.Q<Label>("lbl-broadcast");
        _possessionLabel = root.Q<Label>("lbl-possession");

        _viewportCamera = GetComponent<TacticalViewportCamera>()
                          ?? gameObject.AddComponent<TacticalViewportCamera>();
        _viewportHost = root.Q<VisualElement>("tactical-viewport-host");
        _starMapHost = root.Q<VisualElement>("star-map-host");
        _fleetCommandBar = root.Q<VisualElement>("fleet-command-bar");
        _viewToggleBtn = root.Q<Button>("btn-view-toggle");
        var markersHost = root.Q<VisualElement>("tactical-markers");
        if (markersHost != null)
        {
            markersHost.pickingMode = PickingMode.Position;
        }

        var artBg = root.Q<VisualElement>("art-viewport-bg");
        if (artBg != null)
        {
            artBg.style.backgroundImage = StyleKeyword.None;
            artBg.style.backgroundColor = new StyleColor(new Color(0.03f, 0.04f, 0.06f, 1f));
        }

        _planeOverlay = new TacticalPlaneOverlay(_viewportCamera);
        var grid = root.Q<VisualElement>("tactical-grid");
        if (_viewportHost != null)
        {
            if (grid != null)
            {
                _viewportHost.Insert(_viewportHost.IndexOf(grid) + 1, _planeOverlay);
            }
            else
            {
                _viewportHost.Insert(1, _planeOverlay);
            }
        }

        _viewportPresenter = new TacticalViewportPresenter(markersHost, _viewportCamera);
        _skillBar = root.Q<VisualElement>("skill-bar");
        _rightRail = new TacticalRightRail(root.Q<VisualElement>("right-rail") ?? root);
        _fleetBar = new FleetCommandBar(
            root,
            () => GameAppHost.Instance != null ? GameAppHost.Instance.Core : null,
            SetStatus,
            OnCommandIssued);
        _inputBridge = new PossessionInputBridge(() => GameAppHost.Instance?.Session);

        _inputOverlay = new TacticalViewportInputOverlay();
        if (_viewportHost != null)
        {
            _viewportHost.Insert(_viewportHost.childCount - 1, _inputOverlay);
            _inputOverlay.Bind(_viewportCamera, _viewportPresenter, RefreshAll);
        }

        UiViewportControlBar.BindWithin(_viewportHost ?? root, root, _viewportCamera, RefreshAll);
        if (_starMapHost != null)
        {
            _starMap = GetComponent<StarMapHostController>() ?? gameObject.AddComponent<StarMapHostController>();
            _starMap.Attach(_starMapHost, OnCombatStarMapSystemPicked);
            UiViewportControlBar.BindWithin(_starMapHost, root, _starMap, RefreshAll);
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
                CombatViewModeState.Toggle();
            };
        }

        OnClick(root, "btn-abort", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core != null)
            {
                core.State.combatRealtimeActive = false;
                core.SetPhase(GamePhase.COMBAT_PREP);
            }
        });
        BindKeyboard(root);
        ApplyViewMode();
        RefreshAll();
    }

    private void OnCombatStarMapSystemPicked(string systemId)
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
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

    private void ApplyViewMode()
    {
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
            _fleetCommandBar.SetEnabled(!starMap);
            _fleetCommandBar.style.opacity = starMap ? 0.45f : 1f;
        }
        if (_viewToggleBtn != null)
        {
            _viewToggleBtn.text = starMap ? "战斗视野" : "星图";
        }
        if (starMap)
        {
            var core = GameAppHost.Instance?.Core?.State;
            if (core != null && _starMap != null)
            {
                _starMap.SyncFromState(core);
                HighlightActiveBattlefieldSystem(core);
                _starMap.FrameAll();
            }
        }
    }

    private void HighlightActiveBattlefieldSystem(GameState s)
    {
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
        SetStatus(msg);
        if (success)
        {
            _viewportPresenter?.FlashCommandAck(TacticalSelectionState.GetSelectedFriendlyUnitIds());
        }
    }

    private void BindKeyboard(VisualElement root)
    {
        root.focusable = true;
        _keyHandler = evt =>
        {
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
        base.OnDisable();
    }

    private void OnSelectionChanged() => RefreshAll();

    private void Update()
    {
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
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            SetStatus("模拟未启动");
            return;
        }
        var s = core.State;
        var bf = ActiveBf(s);
        _rightRail?.Refresh(s);
        _planeOverlay?.Refresh(s, bf);
        _viewportPresenter?.Refresh(s, bf);
        _fleetBar?.RefreshGate(s);
        RefreshCombatSkills(core, s);
        if (CombatViewModeState.Mode == CombatViewMode.StarMap && _starMap != null)
        {
            _starMap.SyncFromState(s);
            HighlightActiveBattlefieldSystem(s);
        }

        if (_timerLabel != null)
        {
            var modeLabel = CombatViewModeState.Mode == CombatViewMode.StarMap ? "战略星图" : "战斗视野";
            var vision = s.spectatorMode || s.spectatorFullVision
                ? "观战·全场景"
                : "附身 " + (s.possessingMemberId ?? "无");
            var t = bf != null ? $"T={bf.timeSec:0}s" : "";
            var zoom = _viewportCamera != null ? $" zoom={_viewportCamera.ZoomScale:0.00}" : "";
            var guest = GameAppHost.Instance?.NetworkGuest == true ? "联机客 · " : "";
            _timerLabel.text = guest + modeLabel + " · " + vision + " " + t + zoom;
        }
        if (_possessionLabel != null)
        {
            var possessed = FindPossessedUnit(bf, s.possessingMemberId);
            var throttle = possessed?.throttleOn == true ? "开" : "关";
            var selCount = TacticalSelectionState.GetSelectedFriendlyUnitIds().Count;
            _possessionLabel.text = "附身: " + (s.possessingMemberId ?? "无")
                + " · 框选=" + selCount
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
    }

    private static BattlefieldUnit? FindPossessedUnit(BattlefieldState? bf, string? memberId)
    {
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

    private static string FormatSelectionSummary(BattlefieldState? bf)
    {
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
                return $"选中: {u.displayName} · {tonnage} · {side} · {u.SpeedMps():0} m/s";
            }
        }
        return "未选中目标";
    }

    private void RefreshCombatSkills(SimulationCore core, GameState s)
    {
        if (_skillBar == null)
        {
            return;
        }

        _skillBar.Clear();
        if (s.phase is not (GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            return;
        }

        var catalog = TraitCatalog.LoadDefault();
        var summonTrait = catalog.Find(TraitActiveSkillService.BoardSummonTraitId);
        var summonLabel = DisplayLabels.TraitBilingual(summonTrait);

        foreach (var entry in CombatActiveSkillGate.ListUsableActiveSkills(s, TraitActiveSkillService.BoardSummonTraitId))
        {
            var id = entry.Identity;
            var cd = TraitActiveSkillService.CooldownRoundsRemaining(s, id, TraitActiveSkillService.BoardSummonTraitId);
            var btn = new Button { text = cd > 0 ? $"{summonLabel}({cd})" : summonLabel };
            btn.AddToClassList("rtcombat-fleet-btn");
            btn.tooltip = summonLabel + " · 本场参战现实人 · 冷却按身份共享";
            btn.SetEnabled(cd == 0);
            var memberId = entry.Caster.memberId!;
            btn.clicked += () =>
            {
                SetStatus(core.UseSuppressionSkill(TraitActiveSkillService.BoardSummonTraitId, memberId));
                RefreshAll();
            };
            _skillBar.Add(btn);
        }
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }
}
