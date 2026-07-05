using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TopDog.App;
using TopDog.Client.StarMap;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §布局 · docs/VISION.md §6-§8
 * 本文件: CampaignShellController.cs — 运营阶段主壳层 UI
 * 【机制要点】
 * · 顶栏/左事件/中央星图/右栏/底栏命令行
 * · overlay：资产/配船/招新/图鉴
 * · StarMapHostController 嵌入
 * 【关联】StarMapHostController · MemberListView · OperationsOverlayBuilder
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Operations shell per OPERATIONS_UI.md; star map via StarMapHostController (Gate D).</summary>
public sealed class CampaignShellController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.CampaignShell;

    protected override bool UseSafeAreaInsets => false;

    private readonly StringBuilder _eventFeed = new();
    private readonly HashSet<string> _formationSelection = new();
    private readonly List<(VisualElement? el, EventCallback<ClickEvent> handler)> _dynamicClickHandlers = new();
    private int _companionLogSynced;

    private Label? _timerLabel;
    private Label? _dateLabel;
    private VisualElement? _eventFeedRoot;
    private Label? _toastLabel;
    private ScrollView? _memberDetailScroll;
    private Label? _dispatchHintLabel;
    private Label? _dispatchTargetLabel;
    private Label? _legionStatsLabel;
    private Label? _legionBuildingsLabel;
    private Label? _legionStockLabel;
    private Label? _legionLocationLabel;
    private Label? _formationHintLabel;
    private Label? _overlayTitle;
    private Label? _overlayBody;
    private TextField? _legionNameField;
    private TextField? _commandField;
    private VisualElement? _memberList;
    private ScrollView? _memberScroll;
    private VisualElement? _legionDetails;
    private VisualElement? _overlayLayer;
    private ScrollView? _overlayScroll;
    private VisualElement? _overlayPanel;
    private VisualElement? _modulePickerPopup;
    private Button? _combatPrepBtn;
    private Button? _formationBtn;
    private Button? _formationConfirmBtn;
    private Button? _formationDissolveBtn;
    private Button? _legionDetailsBtn;
    private Button? _enterSystemBtn;
    private Button? _exitSystemBtn;
    private Button? _fittingBtn;
    private StarMapHostController? _starMap;

    private string? _selectedMemberId;
    private string? _dispatchTargetSystemId;
    private bool _legionDetailsExpanded;
    private bool _formationEditMode;
    private string? _formationRangeAnchorKey;
    private float _nextRefresh;
    private GamePhase _lastUiPhase = GamePhase.OPERATIONS;
    private bool _overlayContentDirty;
    private (int queueIndex, CombatPrepStep step, GamePhase phase, bool awaiting) _lastCombatPrepUiSig;
    private string? _memberListSignature;
    private ActiveOverlay _activeOverlay = ActiveOverlay.None;
    private EventCallback<KeyDownEvent>? _keyHandler;
    private EventCallback<ChangeEvent<string>>? _legionNameHandler;
    private EventCallback<KeyDownEvent>? _commandKeyHandler;

    protected override void OnDisable()
    {
        ClearDynamicHandlers();
        if (Root != null && _keyHandler != null)
        {
            Root.UnregisterCallback(_keyHandler);
        }
        if (_legionNameHandler != null && _legionNameField != null)
        {
            _legionNameField.UnregisterValueChangedCallback(_legionNameHandler);
        }
        if (_commandKeyHandler != null && _commandField != null)
        {
            _commandField.UnregisterCallback(_commandKeyHandler);
        }
        _starMap?.Detach();
        base.OnDisable();
    }

    protected override void Bind(VisualElement root)
    {
        try
        {
            BindInner(root);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void BindInner(VisualElement root)
    {
        if (TryRedirectSkirmishToRealtime())
        {
            return;
        }

        _timerLabel = root.Q<Label>("lbl-timer");
        _dateLabel = root.Q<Label>("lbl-date");
        _eventFeedRoot = root.Q<VisualElement>("event-feed-root");
        CompanionLogRail.BindScroll(root.Q<ScrollView>("event-scroll"), _eventFeedRoot);
        _toastLabel = root.Q<Label>("lbl-toast");
        _memberDetailScroll = root.Q<ScrollView>("member-detail-scroll");
        _dispatchHintLabel = root.Q<Label>("lbl-dispatch-hint");
        _dispatchTargetLabel = root.Q<Label>("lbl-dispatch-target");
        _legionStatsLabel = root.Q<Label>("lbl-legion-stats");
        _legionBuildingsLabel = root.Q<Label>("lbl-legion-buildings");
        _legionStockLabel = root.Q<Label>("lbl-legion-stock");
        _legionLocationLabel = root.Q<Label>("lbl-legion-location");
        _formationHintLabel = root.Q<Label>("lbl-formation-hint");
        _overlayTitle = root.Q<Label>("overlay-title");
        _overlayBody = root.Q<Label>("overlay-body");
        _legionNameField = root.Q<TextField>("field-legion-name");
        _commandField = root.Q<TextField>("field-command");
        _memberList = root.Q<VisualElement>("member-list");
        _memberScroll = root.Q<ScrollView>("member-scroll");
        _legionDetails = root.Q<VisualElement>("legion-details");
        _overlayLayer = root.Q<VisualElement>("overlay-layer");
        _overlayScroll = root.Q<ScrollView>("overlay-scroll");
        _overlayPanel = root.Q<VisualElement>("overlay-panel");
        _modulePickerPopup = root.Q<VisualElement>("module-picker-popup");
        _combatPrepBtn = root.Q<Button>("btn-combat-prep");
        _formationBtn = root.Q<Button>("btn-formation");
        _formationConfirmBtn = root.Q<Button>("btn-formation-confirm");
        _formationDissolveBtn = root.Q<Button>("btn-formation-dissolve");
        _legionDetailsBtn = root.Q<Button>("btn-legion-details");
        _enterSystemBtn = root.Q<Button>("btn-enter-system");
        _exitSystemBtn = root.Q<Button>("btn-exit-system");
        _fittingBtn = root.Q<Button>("btn-fitting");

        BindTopBar(root);
        BindFormationAndDispatch(root);
        BindBottomBar(root);
        BindOverlay(root);
        OnClick(root, "btn-fitting", ShowFittingOverlay);
        OnClick(root, "btn-enter-system", EnterSelectedSystem);
        OnClick(root, "btn-exit-system", ExitSystemView);
        BindKeyboard(root);

        if (_memberDetailScroll != null)
        {
            _memberDetailScroll.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    ShowFittingOverlay();
                    evt.StopPropagation();
                }
            });
        }

        if (_legionNameField != null)
        {
            _legionNameHandler = evt =>
            {
                var core = GameAppHost.Instance?.Core;
                if (core != null)
                {
                    core.State.campaignName = evt.newValue;
                }
            };
            _legionNameField.RegisterValueChangedCallback(_legionNameHandler);
        }

        if (_commandField != null)
        {
            _commandKeyHandler = evt =>
            {
                // li3etocoode345
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    SubmitCommandLine();
                    evt.StopPropagation();
                }
            };
            _commandField.RegisterCallback(_commandKeyHandler);
        }

        _starMap = GetComponent<StarMapHostController>() ?? gameObject.AddComponent<StarMapHostController>();
        var mapHost = root.Q<VisualElement>("star-map-host");
        if (mapHost != null)
        {
            _starMap.Attach(mapHost, OnStarMapSystemPicked, OnEventRegionPicked);
            UiViewportControlBar.Bind(root, _starMap);
            UiViewportControlBar.EnsureRaised(root);
            var core = GameAppHost.Instance?.Core?.State;
            if (core != null)
            {
                _starMap.SyncFromState(core);
                _starMap.SetDispatchTarget(_dispatchTargetSystemId);
            }
        }

        PushEvent("运营阶段已加载");
        EnsureSelectedMember();
        RefreshAll();
    }

    private enum ActiveOverlay
    {
        None,
        LegionAssets,
        Codex,
        TraitCodex,
        Trade,
        Craft,
        DispatchRegion,
        Recruit,
        CombatPrep,
        Fitting,
        DefeatChoice,
    }

    private string? _pendingDispatchTask;
    private string _pendingDispatchAnchorMode = MemberDispatchService.AnchorModeSystem;

    private void BindTopBar(VisualElement root)
    {
        OnClick(root, "btn-trade", ShowTradeOverlay);
        OnClick(root, "btn-craft", ShowCraftOverlay);
        OnClick(root, "btn-legion-assets", ShowLegionAssetsOverlay);
        OnClick(root, "btn-codex", ShowCodexOverlay);
        OnClick(root, "btn-trait-codex", ShowTraitCodexOverlay);
        OnClick(root, "btn-hostile", () => PushEvent("敌对界面（占位）"));
        OnClick(root, "btn-friendly", () => PushEvent("友好界面（占位）"));
        OnClick(root, "btn-diplomacy", () => PushEvent("外交界面（占位）"));
        OnClick(root, "btn-combat-prep", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core == null)
            {
                return;
            }
            if (core.State.combatRealtimeActive)
            {
                GameSceneRouter.Instance?.Load(TopDogSceneKind.CombatRealtime);
            }
            else
            {
                ShowCombatPrepOverlay();
            }
        });
        OnClick(root, "btn-legion-details", ToggleLegionDetails);
    }

    private void BindFormationAndDispatch(VisualElement root)
    {
        OnClick(root, "btn-formation", ToggleFormationMode);
        OnClick(root, "btn-formation-confirm", ConfirmFormation);
        OnClick(root, "btn-formation-dissolve", DissolveFormation);
        OnClick(root, "btn-dispatch-mining", () => DispatchTask("采矿"));
        OnClick(root, "btn-dispatch-bounty", () => DispatchTask("赏金"));
        OnClick(root, "btn-dispatch-guard", () => DispatchTask("守卫"));
        OnClick(root, "btn-dispatch-ambush", () => DispatchTask("伏击"));
        OnClick(root, "btn-dispatch-harvest", () => DispatchTask("收割"));
        OnClick(root, "btn-dispatch-anchor", () => DispatchTask("锚定"));
        var randomBtn = root.Q<Button>("btn-dispatch-random");
        if (randomBtn != null)
        {
            randomBtn.style.display = DisplayStyle.None;
        }
    }

    private void BindBottomBar(VisualElement root)
    {
        BindCommandLink(root, "cmd-link-help", "帮助");
        BindCommandLink(root, "cmd-link-status", "状态");
        BindCommandLink(root, "cmd-link-continue", "继续");
        OnClick(root, "btn-recruit", ShowRecruitOverlay);
        OnClick(root, "btn-attack-buildings", AttackBuildingsInDispatchSystem);
    }

    private void AttackBuildingsInDispatchSystem()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            PushEvent("模拟未启动");
            return;
        }
        var msg = PlayerBuildingAssaultService.QueueAssaultOnSystem(
            core.State,
            _dispatchTargetSystemId,
            LegionRegistry.Local(core.State)?.legionId);
        PushEvent(msg);
        RefreshAll();
    }

    private void BindCommandLink(VisualElement root, string name, string command)
    {
        var link = root.Q<Label>(name);
        if (link == null)
        {
            return;
        }
        link.RegisterCallback<ClickEvent>(_ =>
        {
            RunCommand(command);
            if (_commandField != null)
            {
                _commandField.SetValueWithoutNotify(command);
            }
        });
    }

    private void BindOverlay(VisualElement root)
    {
        OnClick(root, "overlay-close", () =>
        {
            if (_activeOverlay != ActiveOverlay.DefeatChoice)
            {
                HideOverlay();
            }
        });
        if (_overlayLayer != null)
        {
            _overlayLayer.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _overlayLayer)
                {
        HideOverlay();
        if (_overlayScroll != null)
        {
            _overlayScroll.parent?.Q("assign-picker")?.RemoveFromHierarchy();
        }
    }
            });
        }
    }

    private void BindKeyboard(VisualElement root)
    // liketocoode3a5
    {
        root.focusable = true;
        root.Focus();
        _keyHandler = evt =>
        {
            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }
            MatchPauseOverlay.TryHandleEscape(root, () =>
            {
                if (_overlayLayer != null && _overlayLayer.ClassListContains("ops-overlay-layer-visible"))
                {
                    HideOverlay();
                    return true;
                }
                return false;
            });
            evt.StopPropagation();
        };
        root.RegisterCallback(_keyHandler);
    }

    private void Update()
    {
        if (!isActiveAndEnabled || Time.unscaledTime < _nextRefresh)
        {
            return;
        }
        _nextRefresh = Time.unscaledTime + 0.25f;
        RefreshAll();
    }

    private void RefreshAll()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        var s = core.State;
        var phaseChanged = _lastUiPhase != s.phase;
        if (phaseChanged)
        {
            _lastUiPhase = s.phase;
            _overlayContentDirty = true;
            if (s.phase == GamePhase.COMBAT_PREP)
            {
                ShowCombatPrepOverlay();
            }
        }
        RefreshTimer(s);
        RefreshLegionHeader(s);
        RefreshDispatchButtons(s);
        RefreshMemberList(s);
        RefreshMemberDetail(s);
        RefreshDispatchLabels(s);
        RefreshCombatPrep(s);
        RefreshFormationUi();
        RefreshStarMapModeBar();
        TryShowDefeatChoiceOverlay(core);
        RefreshSpectatorControls(s);
        _starMap?.SetDispatchTarget(_dispatchTargetSystemId);
        if (_overlayLayer != null && _overlayLayer.ClassListContains("ops-overlay-layer-visible"))
        {
            if (_overlayContentDirty || ShouldRefreshActiveOverlay(s))
            {
                RefreshActiveOverlay(core);
                _overlayContentDirty = false;
            }
        }
        else if (ShouldRefreshActiveOverlay(s))
        {
            RefreshActiveOverlay(core);
        }
        if (_eventFeedRoot != null)
        {
            CompanionLogRail.SyncCompanion(core, _eventFeedRoot, ref _companionLogSynced);
        }
    }

    private string _tradeTab = "market";
    private string _marketCategory = "";

    private bool ShouldRefreshActiveOverlay(GameState s) =>
        _activeOverlay switch
        {
            ActiveOverlay.Recruit => s.recruitProgressSec > 0f,
            ActiveOverlay.CombatPrep => CombatPrepOverlayNeedsRefresh(s),
            _ => false,
        };

    private bool CombatPrepOverlayNeedsRefresh(GameState s)
    {
        if (s.phase is not (GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            return false;
        }
        var sig = (s.combatQueueIndex, s.combatPrepStep, s.phase, s.combatAwaitingContinue);
        if (sig.Equals(_lastCombatPrepUiSig))
        {
            return false;
        }
        _lastCombatPrepUiSig = sig;
        return true;
    }

    private void RequestOverlayRefresh()
    {
        _overlayContentDirty = true;
        RefreshAll();
    }

    private void RefreshTimer(GameState s)
    {
        if (_timerLabel != null)
        {
            var rem = Mathf.Max(0f, s.operationTimeRemainingSec);
            var min = (int)(rem / 60f);
            var sec = (int)(rem % 60f);
            var time = $"{min:00}:{sec:00}";
            _timerLabel.text = GameAppHost.Instance?.NetworkGuest == true
                ? "联机客 · " + time
                : time;
        }
        if (_dateLabel != null)
        {
            _dateLabel.text = $"Y{s.gameYear} 第{s.gameWeek}周";
        }
    }

    private void RefreshLegionHeader(GameState s)
    {
        if (_legionNameField != null && _legionNameField.value != s.campaignName)
        {
            _legionNameField.SetValueWithoutNotify(s.campaignName ?? "军团");
        }
        if (_legionStatsLabel != null)
        {
            var cmd = CommanderDisplayName(s);
            var pendingAssaults = s.playerPendingAssaults.Count;
            var assaultHint = pendingAssaults > 0 ? $" · 待战约战 {pendingAssaults}" : "";
            _legionStatsLabel.text = $"团员 {s.members.Count} · 编队 {s.formations.Count}"
                + assaultHint
                + (cmd != null ? " · 军团长: " + cmd : "");
        }
        if (_legionBuildingsLabel != null)
        {
            var defeated = CampaignOutcomeService.Defeated.Equals(s.campaignOutcome, StringComparison.Ordinal);
            var victory = CampaignOutcomeService.Victory.Equals(s.campaignOutcome, StringComparison.Ordinal);
            var draw = CampaignOutcomeService.Draw.Equals(s.campaignOutcome, StringComparison.Ordinal);
            var suffix = s.spectatorMode ? " · 观战中"
                : draw ? " · 平局"
                : victory ? " · 胜利"
                : defeated ? " · 败北"
                : "";
            _legionBuildingsLabel.text = $"建筑 {s.buildings.Count} · 第 {s.storyRound} 回合{suffix}";
        }
        // liketocoode34e
        if (_legionStockLabel != null)
        {
            _legionStockLabel.text = $"舰库存 {CountLegionStock(s)} · 装备库存 {s.legionStock.Count}";
        }
        if (_legionLocationLabel != null)
        {
            _legionLocationLabel.text = $"出生/当前: {SystemName(s, s.currentSolarSystemId)}";
        }
    }

    private static string? CommanderDisplayName(GameState s)
    {
        if (string.IsNullOrWhiteSpace(s.commanderIdentityCode))
        {
            return null;
        }
        foreach (var m in s.members)
        {
            if (s.commanderIdentityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                return !string.IsNullOrWhiteSpace(m.name) ? m.name : m.accountName;
            }
        }
        return s.commanderIdentityCode;
    }

    private void RefreshDispatchButtons(GameState s)
    {
        var block = s.spectatorMode
            || CampaignOutcomeService.ShouldOfferDefeatChoice(s)
            || s.matchEnded
            || CampaignOutcomeService.Defeated.Equals(s.campaignOutcome, StringComparison.Ordinal);
        var ids = new[]
        {
            "btn-dispatch-mining", "btn-dispatch-bounty", "btn-dispatch-guard",
            "btn-dispatch-ambush", "btn-dispatch-harvest", "btn-dispatch-anchor",
        };
        foreach (var id in ids)
        {
            var btn = Root?.Q<Button>(id);
            if (btn != null)
            {
                btn.SetEnabled(!block);
            }
        }
    }

    private void RefreshSpectatorControls(GameState s)
    {
        var block = s.spectatorMode
            || CampaignOutcomeService.ShouldOfferDefeatChoice(s)
            || s.matchEnded;
        var topIds = new[]
        {
            "btn-trade", "btn-craft", "btn-legion-assets", "btn-codex", "btn-trait-codex",
            "btn-hostile", "btn-friendly", "btn-diplomacy",
        };
        foreach (var id in topIds)
        {
            Root?.Q<Button>(id)?.SetEnabled(!block);
        }
        _formationBtn?.SetEnabled(!block);
        _formationConfirmBtn?.SetEnabled(!block);
        _formationDissolveBtn?.SetEnabled(!block);
        _fittingBtn?.SetEnabled(!block);
        Root?.Q<Button>("btn-recruit")?.SetEnabled(!block);
        if (_legionNameField != null)
        {
            _legionNameField.SetEnabled(!block);
        }
        if (_commandField != null)
        {
            _commandField.SetEnabled(!block);
        }
    }

    private void TryShowDefeatChoiceOverlay(SimulationCore core)
    {
        if (!CampaignOutcomeService.ShouldOfferDefeatChoice(core.State))
        {
            if (_activeOverlay == ActiveOverlay.DefeatChoice)
            {
                HideOverlay();
            }
            return;
        }
        if (_activeOverlay == ActiveOverlay.DefeatChoice)
        {
            return;
        }
        _activeOverlay = ActiveOverlay.DefeatChoice;
        ShowOverlay("败北", "星图内已无可停靠建筑。对局仍在进行，你可以选择：", wide: true);
        if (_overlayScroll == null)
        {
            return;
        }
        _overlayScroll.Clear();
        var body = new Label("观战模式下可继续浏览星图与团员调动；实时交战阶段可观看全部场景。");
        body.AddToClassList("ops-overlay-body");
        _overlayScroll.Add(body);
        var watchBtn = new Button { text = "观战模式" };
        watchBtn.AddToClassList("menu-button-wide");
        watchBtn.clicked += () =>
        {
            SpectatorModeService.EnterSpectator(core.State);
            HideOverlay();
            PushEvent("已进入观战模式");
            RefreshAll();
        };
        _overlayScroll.Add(watchBtn);
        var menuBtn = new Button { text = "返回主菜单" };
        menuBtn.AddToClassList("menu-button-wide");
        menuBtn.clicked += () =>
        {
            GameAppHost.Instance?.EndCampaign(markCreditsDismissed: true);
            GameSceneRouter.Instance?.GoOutOfMatch();
        };
        _overlayScroll.Add(menuBtn);
    }

    private static int CountLegionStock(GameState s)
    {
        var total = 0;
        foreach (var kv in s.legionStock)
        {
            total += kv.Value;
        }
        return total;
    }

    private void RefreshMemberList(GameState s, bool force = false)
    {
        if (_memberList == null)
        {
            return;
        }

        var localId = LegionRegistry.Local(s)?.legionId;
        if (!string.IsNullOrWhiteSpace(localId))
        {
            LegionPlayerRegistry.EnsureRosterForLegion(s, localId);
        }
        else
        {
            LegionPlayerRegistry.EnsureAggregateFromBuckets(s);
        }

        var signature = BuildMemberListSignature(s);
        var rosterCount = MemberRosterSort.RosterForLegion(s, localId).Count;
        if (!force
            && signature == _memberListSignature
            && _memberList.childCount > 0
            && rosterCount > 0)
        {
            return;
        }
        _memberListSignature = signature;
        ClearDynamicHandlers();

        // liketocoo3e345
        MemberListView.Populate(_memberList, s, new MemberListView.Options
        {
            Style = MemberListView.Presentation.SidebarOverview,
            FormationEditMode = _formationEditMode,
            SelectedKey = _selectedMemberId,
            FormationPickedKeys = _formationSelection,
            OnRowActivated = OnMemberListRowActivated,
            ScrollHost = _memberScroll,
            LocalLegionId = LegionRegistry.Local(s)?.legionId,
        });
    }

    private string BuildMemberListSignature(GameState s)
    {
        var sb = new StringBuilder(512);
        sb.Append(_formationEditMode ? '1' : '0').Append('|');
        sb.Append(_selectedMemberId).Append('|');
        foreach (var key in _formationSelection.OrderBy(k => k, StringComparer.Ordinal))
        {
            sb.Append(key).Append(',');
        }
        sb.Append('|');
        foreach (var m in MemberRosterSort.RosterForLegion(s, LegionRegistry.Local(s)?.legionId))
        {
            sb.Append(MemberSelectionKeys.For(m));
            sb.Append(':').Append(m.formationId);
            sb.Append(':').Append(m.equippedHullId);
            sb.Append(':').Append(m.assignedTask);
            sb.Append(':').Append(m.appraised);
            sb.Append(':').Append(m.currentSolarSystemId);
            sb.Append('|');
        }
        return sb.ToString();
    }

    private void OnMemberListRowActivated(MemberState member, string key, bool shiftKey)
    {
        if (_formationEditMode)
        {
            var orderedKeys = OrderedMemberKeys(GameAppHost.Instance!.Core!.State);
            if (shiftKey && _formationRangeAnchorKey != null)
            {
                SelectFormationRange(_formationRangeAnchorKey, key, orderedKeys);
            }
            else if (_formationSelection.Contains(key))
            {
                _formationSelection.Remove(key);
            }
            else
            {
                _formationSelection.Add(key);
            }

            _formationRangeAnchorKey = key;
            PushEvent($"已选 {_formationSelection.Count} 人");
            RefreshMemberList(GameAppHost.Instance!.Core!.State);
            return;
        }
        OnMemberCardClicked(member);
    }

    private static List<string> OrderedMemberKeys(GameState state)
    {
        var localId = LegionRegistry.Local(state)?.legionId;
        var roster = string.IsNullOrWhiteSpace(localId)
            ? MemberRosterSort.Order(state.members)
            : MemberRosterSort.RosterForLegion(state, localId);
        var keys = new List<string>(roster.Count);
        foreach (var m in roster)
        {
            var key = MemberSelectionKeys.For(m);
            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private void SelectFormationRange(string anchorKey, string endKey, IReadOnlyList<string> orderedKeys)
    {
        var a = -1;
        var b = -1;
        for (var i = 0; i < orderedKeys.Count; i++)
        {
            if (orderedKeys[i].Equals(anchorKey, StringComparison.Ordinal))
            {
                a = i;
            }

            if (orderedKeys[i].Equals(endKey, StringComparison.Ordinal))
            {
                b = i;
            }
        }

        if (a < 0 || b < 0)
        {
            _formationSelection.Add(endKey);
            return;
        }

        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);
        for (var i = lo; i <= hi; i++)
        {
            _formationSelection.Add(orderedKeys[i]);
        }
    }

    private void EnsureSelectedMember()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null || core.State.members.Count == 0)
        {
            return;
        }
        if (_selectedMemberId != null && FindSelectedMember(core.State) != null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(core.State.possessingMemberId)
            && FindMemberByKey(core.State, core.State.possessingMemberId) != null)
        {
            _selectedMemberId = core.State.possessingMemberId;
            return;
        }
        var roster = MemberRosterSort.RosterForLegion(core.State, LegionRegistry.Local(core.State)?.legionId);
        if (roster.Count > 0)
        {
            _selectedMemberId = MemberSelectionKeys.For(roster[0]);
            return;
        }

        _selectedMemberId = MemberSelectionKeys.For(core.State.members[0]);
    }

    private void RefreshActiveOverlay(SimulationCore core)
    {
        if (_overlayLayer == null || _overlayScroll == null
            || !_overlayLayer.ClassListContains("ops-overlay-layer-visible"))
        {
            return;
        }
        switch (_activeOverlay)
        {
            case ActiveOverlay.LegionAssets:
                LegionAssetsPanel.Populate(_overlayScroll, core, PushEvent, RequestOverlayRefresh);
                break;
            case ActiveOverlay.Codex:
                MemberCodexPanel.Populate(_overlayScroll, core, PushEvent, RequestOverlayRefresh);
                break;
            case ActiveOverlay.TraitCodex:
                TraitCodexPanel.Populate(_overlayScroll, core, PushEvent);
                break;
            case ActiveOverlay.Trade:
                TradeOverlayPanel.Populate(
                    _overlayScroll, core, PushEvent, RequestOverlayRefresh, _tradeTab, tab => _tradeTab = tab,
                    _marketCategory, cat => _marketCategory = cat);
                break;
            case ActiveOverlay.Craft:
                CraftOverlayPanel.Populate(_overlayScroll, core, PushEvent, RequestOverlayRefresh);
                break;
            case ActiveOverlay.DispatchRegion:
                if (_pendingDispatchTask != null && _dispatchTargetSystemId != null && _selectedMemberId != null)
                {
                    DispatchRegionOverlay.Populate(
                        _overlayScroll,
                        core,
                        _dispatchTargetSystemId,
                        _pendingDispatchTask,
                        (regionId, regionExplicit) =>
                        {
                            PushEvent(core.DispatchMemberToSystem(
                                _selectedMemberId,
                                _pendingDispatchTask,
                                _dispatchTargetSystemId,
                                _pendingDispatchAnchorMode,
                                regionId,
                                regionExplicit));
                            _pendingDispatchTask = null;
                            HideOverlay();
                            RefreshAll();
                        });
                }
                break;
            case ActiveOverlay.Recruit:
                RecruitOverlayPanel.Populate(_overlayScroll, core, PushEvent, RequestOverlayRefresh);
                break;
            case ActiveOverlay.CombatPrep:
                CombatPrepPanel.Populate(_overlayScroll, core, PushEvent, RequestOverlayRefresh);
                break;
            case ActiveOverlay.Fitting:
                var m = FindSelectedMember(core.State);
                if (m != null)
                {
                    ShipFittingPanel.Populate(
                        _overlayScroll, _modulePickerPopup, core, m, PushEvent, RequestOverlayRefresh);
                }
                break;
        }
    }

    private static MemberState? FindMemberByKey(GameState s, string key)
    {
        foreach (var m in s.members)
        {
            if (key.Equals(MemberSelectionKeys.For(m), StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    // liketoco0de345
    }

    private void RefreshMemberDetail(GameState s)
    {
        if (_memberDetailScroll == null)
        {
            return;
        }
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        var m = FindSelectedMember(s);
        if (_fittingBtn != null)
        {
            _fittingBtn.SetEnabled(m != null);
        }
        MemberDetailPanel.Populate(
            _memberDetailScroll,
            core,
            m,
            PushEvent,
            ShowFittingOverlay,
            UseSuppressionSkill);
    }

    private void UseSuppressionSkill(string traitId, string label)
    {
        var core = GameAppHost.Instance?.Core;
        var s = core?.State;
        var m = s != null ? FindSelectedMember(s) : null;
        if (core == null || m?.memberId == null)
        {
            return;
        }
        var echo = core.UseSuppressionSkill(traitId, m.memberId);
        PushEvent(label + "：" + echo);
        if (s != null)
        {
            RefreshMemberDetail(s);
        }
    }

    private void RefreshDispatchLabels(GameState s)
    {
        var targetText = _dispatchTargetSystemId != null
            ? SystemName(s, _dispatchTargetSystemId)
            : "(点击星系)";
        var line = $"派遣目标: {targetText}";
        if (_dispatchHintLabel != null)
        {
            _dispatchHintLabel.text = line;
        }
        if (_dispatchTargetLabel != null)
        {
            _dispatchTargetLabel.text = line;
        }
    }

    private void RefreshCombatPrep(GameState s)
    {
        if (_combatPrepBtn == null)
        {
            return;
        }
        var visible = s.phase is GamePhase.COMBAT_PREP or GamePhase.COMBAT;
        _combatPrepBtn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _combatPrepBtn.text = s.combatRealtimeActive ? "进入战术" : CombatPrepLabel(s);
    }

    private static string CombatPrepLabel(GameState s) =>
        s.phase == GamePhase.COMBAT ? "继续" : "自动交战";

    private void RefreshFormationUi()
    {
        if (_formationBtn != null)
        {
            _formationBtn.text = _formationEditMode ? "取消编队" : "编队";
        }
        if (_formationConfirmBtn != null)
        {
            _formationConfirmBtn.style.display = _formationEditMode ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_formationDissolveBtn != null)
        {
            _formationDissolveBtn.style.display = _formationEditMode ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_formationHintLabel != null)
        {
            if (_formationEditMode)
            {
                _formationHintLabel.style.display = DisplayStyle.Flex;
                _formationHintLabel.text = "编队模式：点选团员 · Shift+点第二个首尾连选 · 确认编队";
            }
            else
            {
                _formationHintLabel.style.display = DisplayStyle.None;
            }
        }
    }

    private void ToggleLegionDetails()
    {
        _legionDetailsExpanded = !_legionDetailsExpanded;
        if (_legionDetails != null)
        {
            _legionDetails.style.display = _legionDetailsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_legionDetailsBtn != null)
        {
            _legionDetailsBtn.text = _legionDetailsExpanded ? "军团详情 ▲" : "军团详情 ▼";
        }
    }

    private void ToggleFormationMode()
    {
        _formationEditMode = !_formationEditMode;
        if (!_formationEditMode)
        {
            _formationSelection.Clear();
            _formationRangeAnchorKey = null;
        }
        RefreshAll();
    }

    private void ConfirmFormation()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        if (_formationSelection.Count < 2)
        {
            PushEvent("编队需要至少 2 名团员");
            return;
        }
        PushEvent(core.CreateFormation(new List<string>(_formationSelection)));
        _formationEditMode = false;
        _formationSelection.Clear();
        _formationRangeAnchorKey = null;
        RefreshAll();
    }

    private void DissolveFormation()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        var memberId = _selectedMemberId;
        if (memberId == null && _formationSelection.Count > 0)
        {
            foreach (var id in _formationSelection)
            {
                memberId = id;
                break;
            }
        }
        if (memberId == null)
        // lik3tocoode345
        {
            PushEvent("请先选中一名在编队中的团员");
            return;
        }
        PushEvent(core.DissolveFormationForMember(memberId));
        RefreshAll();
    }

    private void OnMemberCardClicked(MemberState member)
    {
        if (_formationEditMode)
        {
            return;
        }
        var key = MemberSelectionKeys.For(member);
        if (key == null)
        {
            return;
        }
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        _selectedMemberId = key;
        RefreshMemberList(core.State);
        RefreshMemberDetail(core.State);
    }

    private void DispatchTask(string label)
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        if (_selectedMemberId == null)
        {
            PushEvent("请先选中一名团员");
            return;
        }
        if (string.IsNullOrEmpty(_dispatchTargetSystemId))
        {
            PushEvent("请先选择派遣目标星系（点击星系图标）");
            return;
        }
        if (core.State.campaignOutcome != null
            && CampaignOutcomeService.Defeated.Equals(core.State.campaignOutcome, StringComparison.Ordinal)
            && core.State.matchEnded)
        {
            PushEvent("对局已结束");
            return;
        }
        if (SpectatorModeService.IsSpectating(core.State))
        {
            PushEvent("观战模式中无法下达指令");
            return;
        }
        if (CampaignOutcomeService.ShouldOfferDefeatChoice(core.State))
        {
            PushEvent("请选择观战或返回主菜单");
            return;
        }
        var task = label == "锚定" ? MemberDispatchService.TaskAnchor : label;
        if (EventRegionPicker.RequiredKindForTask(task) != null)
        {
            _pendingDispatchTask = task;
            _pendingDispatchAnchorMode = MemberDispatchService.AnchorModeSystem;
            var overlayTitle = task == MemberDispatchService.TaskAnchor ? "选择行星锚点（军堡 3000 星币）" : "选择部署区域";
            _activeOverlay = ActiveOverlay.DispatchRegion;
            ShowOverlay(overlayTitle, "", wide: true, mode: OverlayMode.Docked);
            DispatchRegionOverlay.Populate(
                _overlayScroll!,
                core,
                _dispatchTargetSystemId,
                task,
                (regionId, regionExplicit) =>
                {
                    PushEvent(core.DispatchMemberToSystem(
                        _selectedMemberId,
                        task,
                        _dispatchTargetSystemId,
                        MemberDispatchService.AnchorModeSystem,
                        regionId,
                        regionExplicit));
                    _pendingDispatchTask = null;
                    HideOverlay();
                    RefreshAll();
                });
            return;
        }
        PushEvent(core.DispatchMemberToSystem(
            _selectedMemberId, task, _dispatchTargetSystemId, MemberDispatchService.AnchorModeSystem));
        RefreshAll();
    }

    private void RefreshStarMapModeBar()
    {
        var interior = _starMap != null && _starMap.IsSystemInterior;
        if (_enterSystemBtn != null)
        {
            _enterSystemBtn.style.display = interior ? DisplayStyle.None : DisplayStyle.Flex;
            _enterSystemBtn.SetEnabled(!string.IsNullOrEmpty(_dispatchTargetSystemId));
        }
        if (_exitSystemBtn != null)
        {
            _exitSystemBtn.style.display = interior ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_dispatchTargetLabel != null && interior && _starMap?.InteriorSystemId != null)
        {
            var core = GameAppHost.Instance?.Core?.State;
            var name = core != null ? SystemName(core, _starMap.InteriorSystemId) : _starMap.InteriorSystemId;
            _dispatchTargetLabel.text = $"星系内部: {name}（仅显示地点）";
        }
    }

    private void ShowLegionAssetsOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("军团资产", "模拟未启动", wide: true, mode: OverlayMode.Docked, legionAssets: true);
            return;
        }
        _activeOverlay = ActiveOverlay.LegionAssets;
        ShowOverlay("军团资产", "", wide: true, mode: OverlayMode.Docked, legionAssets: true);
        LegionAssetsPanel.Populate(_overlayScroll!, core, PushEvent, RequestOverlayRefresh);
    }

    private void ShowCodexOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("团员图鉴", "模拟未启动", wide: true, mode: OverlayMode.Docked);
            return;
        }
        _activeOverlay = ActiveOverlay.Codex;
        ShowOverlay("团员图鉴", "", wide: true, mode: OverlayMode.Docked);
        MemberCodexPanel.Populate(_overlayScroll!, core, PushEvent, RequestOverlayRefresh);
    }

    private void ShowTraitCodexOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("词条图鉴", "模拟未启动", wide: true, mode: OverlayMode.Docked);
            return;
        }
        _activeOverlay = ActiveOverlay.TraitCodex;
        ShowOverlay("词条图鉴", "", wide: true, mode: OverlayMode.Docked);
        TraitCodexPanel.Populate(_overlayScroll!, core, PushEvent);
    }

    private void ShowCraftOverlay()
    {
        // liketocoode3e5
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("制造", "模拟未启动", wide: true, mode: OverlayMode.Docked);
            return;
        }

        _activeOverlay = ActiveOverlay.Craft;
        ShowOverlay("制造", "", wide: true, mode: OverlayMode.Docked);
        CraftOverlayPanel.Populate(_overlayScroll!, core, PushEvent, RequestOverlayRefresh);
    }

    private void ShowTradeOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("交易", "模拟未启动", wide: true, mode: OverlayMode.Docked, trade: true);
            return;
        }
        _activeOverlay = ActiveOverlay.Trade;
        ShowOverlay("交易", "", wide: true, mode: OverlayMode.Docked, trade: true);
        TradeOverlayPanel.Populate(
            _overlayScroll!, core, PushEvent, RequestOverlayRefresh, _tradeTab, tab => _tradeTab = tab,
            _marketCategory, cat => _marketCategory = cat);
    }

    private void ShowRecruitOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("招新", "模拟未启动", wide: true);
            return;
        }
        _activeOverlay = ActiveOverlay.Recruit;
        ShowOverlay("招新", "", wide: true);
        RecruitOverlayPanel.Populate(_overlayScroll!, core, PushEvent, RequestOverlayRefresh);
    }

    private void ShowCombatPrepOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            ShowOverlay("交战准备", "模拟未启动", wide: true, mode: OverlayMode.Docked);
            return;
        }
        _activeOverlay = ActiveOverlay.CombatPrep;
        _lastCombatPrepUiSig = default;
        ShowOverlay("交战准备", "", wide: true, mode: OverlayMode.Docked);
        CombatPrepPanel.Populate(_overlayScroll!, core, PushEvent, RequestOverlayRefresh);
    }

    private void ShowFittingOverlay()
    {
        var core = GameAppHost.Instance?.Core;
        var m = core?.State != null ? FindSelectedMember(core.State) : null;
        if (m == null || core == null)
        {
            PushEvent("请先选中一名团员");
            return;
        }
        if (_overlayScroll == null)
        {
            return;
        }
        _activeOverlay = ActiveOverlay.Fitting;
        ShowOverlay("配船", "", wide: true, mode: OverlayMode.Fitting);
        ShipFittingPanel.Populate(_overlayScroll, _modulePickerPopup, core, m, PushEvent, RequestOverlayRefresh);
    }

    private void EnterSelectedSystem()
    {
        if (string.IsNullOrEmpty(_dispatchTargetSystemId))
        {
            PushEvent("请先点击星图选择目标星系");
            return;
        }
        _starMap?.EnterSystemInterior(_dispatchTargetSystemId);
        var core = GameAppHost.Instance?.Core?.State;
        var name = core != null ? SystemName(core, _dispatchTargetSystemId) : _dispatchTargetSystemId;
        PushEvent($"进入星系: {name}");
        RefreshStarMapModeBar();
        RefreshDispatchLabels(core ?? new GameState());
    }

    private void ExitSystemView()
    {
        _starMap?.ExitSystemInterior();
        PushEvent("返回战略星图");
        RefreshStarMapModeBar();
        var core = GameAppHost.Instance?.Core?.State;
        if (core != null)
        {
            RefreshDispatchLabels(core);
        }
    }

    private void OnEventRegionPicked(string regionId)
    {
        PushEvent($"选中地点: {regionId}");
    }

    private void OnStarMapSystemPicked(string systemId)
    {
        _dispatchTargetSystemId = systemId;
        _starMap?.SetDispatchTarget(systemId);
        var core = GameAppHost.Instance?.Core?.State;
        var name = core != null ? SystemName(core, systemId) : systemId;
        PushEvent($"派遣目标: {name}");
        if (core != null)
        {
            RefreshDispatchLabels(core);
            RefreshStarMapModeBar();
        }
    }

    private void SubmitCommandLine()
    {
        if (_commandField == null)
        {
            return;
        }
        RunCommand(_commandField.value);
        _commandField.value = string.Empty;
    }

    private void RunCommand(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }
        var echo = GameAppHost.Instance?.SubmitCommand(line.Trim()) ?? "模拟未启动";
        PushEvent(echo);
        if (_toastLabel != null)
        {
            _toastLabel.text = echo;
        }
    }

    private void PushEvent(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        if (_eventFeed.Length > 0)
        {
            _eventFeed.Append('\n');
        }
        _eventFeed.Append(message);
        CompanionLogRail.AppendSystemLine(_eventFeedRoot, message);
        if (_toastLabel != null)
        {
            _toastLabel.text = message;
        }
    }

    private enum OverlayMode
    // liket0coode345
    {
        Modal,
        Docked,
        Fitting,
    }

    private void ShowOverlay(
        string title,
        string body,
        bool wide = false,
        OverlayMode mode = OverlayMode.Modal,
        bool legionAssets = false,
        bool trade = false)
    {
        if (_overlayLayer == null)
        {
            return;
        }
        if (_overlayTitle != null)
        {
            _overlayTitle.text = title;
        }
        if (_overlayPanel != null)
        {
            if (wide)
            {
                _overlayPanel.AddToClassList("ops-overlay-panel-wide");
            }
            else
            {
                _overlayPanel.RemoveFromClassList("ops-overlay-panel-wide");
            }
            _overlayPanel.RemoveFromClassList("ops-overlay-panel-fitting");
            _overlayPanel.RemoveFromClassList("ops-overlay-panel-docked");
            _overlayPanel.RemoveFromClassList("ops-overlay-panel-legion-assets");
            _overlayPanel.RemoveFromClassList("ops-overlay-panel-trade");
            if (mode == OverlayMode.Fitting)
            {
                _overlayPanel.AddToClassList("ops-overlay-panel-fitting");
            }
            else if (mode == OverlayMode.Docked)
            {
                _overlayPanel.AddToClassList("ops-overlay-panel-docked");
            }
            if (legionAssets)
            {
                _overlayPanel.AddToClassList("ops-overlay-panel-legion-assets");
            }
            if (trade)
            {
                _overlayPanel.AddToClassList("ops-overlay-panel-trade");
            }
            _overlayPanel.pickingMode = PickingMode.Position;
        }
        if (_overlayScroll != null)
        {
            if (legionAssets)
            {
                _overlayScroll.AddToClassList("ops-overlay-scroll-legion-assets");
                _overlayScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }
            else
            {
                _overlayScroll.RemoveFromClassList("ops-overlay-scroll-legion-assets");
            }
            if (trade)
            {
                _overlayScroll.AddToClassList("ops-overlay-scroll-trade");
            }
            else
            {
                _overlayScroll.RemoveFromClassList("ops-overlay-scroll-trade");
            }
        }
        if (mode == OverlayMode.Fitting || mode == OverlayMode.Docked)
        {
            _overlayLayer.pickingMode = PickingMode.Ignore;
            _overlayLayer.AddToClassList("ops-overlay-layer-pass-through");
        }
        else
        {
            _overlayLayer.pickingMode = PickingMode.Position;
            _overlayLayer.RemoveFromClassList("ops-overlay-layer-pass-through");
        }
        ShipFittingPanel.HideModulePicker(_modulePickerPopup);
        if (_overlayScroll != null)
        {
            _overlayScroll.Clear();
            if (!string.IsNullOrEmpty(body))
            {
                var label = new Label(body);
                label.AddToClassList("ops-overlay-body");
                _overlayScroll.Add(label);
            }
        }
        else if (_overlayBody != null)
        {
            _overlayBody.text = body;
        }
        _overlayLayer.AddToClassList("ops-overlay-layer-visible");
        _overlayLayer.style.display = DisplayStyle.Flex;
    }

    private void HideOverlay()
    {
        if (_overlayLayer == null)
        {
            return;
        }
        ShipFittingPanel.HideModulePicker(_modulePickerPopup);
        _activeOverlay = ActiveOverlay.None;
        _overlayLayer.RemoveFromClassList("ops-overlay-layer-visible");
        _overlayLayer.RemoveFromClassList("ops-overlay-layer-pass-through");
        _overlayLayer.pickingMode = PickingMode.Position;
        _overlayPanel?.RemoveFromClassList("ops-overlay-panel-fitting");
        _overlayPanel?.RemoveFromClassList("ops-overlay-panel-docked");
        _overlayLayer.style.display = DisplayStyle.None;
        _overlayScroll?.parent?.Q("assign-picker")?.RemoveFromHierarchy();
    }

    private MemberState? FindSelectedMember(GameState s) =>
        MemberSelectionKeys.FindMember(s, _selectedMemberId);

    private static string MemberDisplayName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "团员";

    private static string SystemName(GameState s, string? systemId)
    {
        if (string.IsNullOrEmpty(systemId))
        {
            return "—";
        }
        var map = s.map?.Project;
        if (map?.systems != null)
        {
            foreach (var sys in map.systems)
            {
                if (systemId.Equals(sys.solarSystemId, System.StringComparison.Ordinal))
                {
                    return !string.IsNullOrEmpty(sys.name) ? sys.name : systemId;
                }
            }
        }
        return systemId;
    }

    private void ClearDynamicHandlers()
    {
        foreach (var (el, handler) in _dynamicClickHandlers)
        {
            el?.UnregisterCallback(handler);
        }
        _dynamicClickHandlers.Clear();
    }

    private static bool TryRedirectSkirmishToRealtime()
    {
        var state = GameAppHost.Instance?.Core?.State;
        if (state == null || !SkirmishPhaseRules.InSkirmishSession(state) || state.matchEnded)
        {
            return false;
        }

        GameSceneRouter.Instance?.Load(TopDogSceneKind.CombatRealtime);
        return true;
    }
// liketocoode3a5
}
