using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.Client;
using TopDog.Content;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Starting;
using TopDog.Lobby;
using TopDog.Net.Lan;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>军团约战准备（LEGION_SKIRMISH.md）：左上人机/真人 · 无用户建房。</summary>
public sealed class SkirmishLobbyController : UiScreenController
{
    private static readonly int[] ScaleChoices = { 10, 30, 50, 100, 200, 500 };

    public override UiScreenId ArtScreenId => UiScreenId.SkirmishLobby;

    protected override bool UseSafeAreaInsets => false;

    private SkirmishLobbyState _lobby = new();
    private readonly ShipRegistry _ships = ShipRegistry.LoadDefault();
    private readonly ModuleRegistry _modules = ModuleRegistry.LoadDefault();
    private readonly List<TemplateCatalogEntry> _templates = new();
    private readonly List<(Button btn, EventCallback<ClickEvent> handler)> _dynamicHandlers = new();

    private SkirmishMatchBroker? _matchBroker;
    private bool _matching;
    private bool _launching;
    private SkirmishLobbyPrepCore? _prepCore;

    private string? _selectedPlayerId;
    private string? _selectedRosterMemberId;
    private bool _templatePickerVisible;
    private bool _presetSavePickerVisible;
    private bool _presetLoadPickerVisible;

    private ScrollView? _playerList;
    private VisualElement? _playerSection;
    private VisualElement? _modeRow;
    private ScrollView? _rosterCodexScroll;
    private ScrollView? _templateMemberPicker;
    private ScrollView? _presetSavePicker;
    private ScrollView? _presetLoadPicker;
    private ScrollView? _fittingScroll;
    private VisualElement? _modulePickerPopup;
    private Label? _ruleLabel;
    private Label? _statusLabel;
    private Button? _startBtn;

    protected override void OnDisable()
    {
        CancelMatching();
        ClearDynamicHandlers();
        ShipFittingPanel.HideModulePicker(_modulePickerPopup);
        base.OnDisable();
    }

    private void Update()
    {
        if (!_matching || _matchBroker == null || _launching)
        {
            return;
        }

        _matchBroker.Tick(Time.deltaTime);
        var snap = _matchBroker.Snapshot;
        SetStatus(snap.StatusMessage);

        switch (snap.Phase)
        {
            case SkirmishMatchPhase.Seeking:
                SetRule("匹配中… 可返回修改配置后再次点击开始");
                break;
            case SkirmishMatchPhase.ScaleMismatch:
                SetRule(snap.StatusMessage);
                _matching = false;
                if (_startBtn != null)
                {
                    _startBtn.text = "开始约战";
                    _startBtn.SetEnabled(true);
                }
                break;
            case SkirmishMatchPhase.Ready:
                _matching = false;
                LaunchHumanMatch(snap);
                break;
        }
    }

    protected override void Bind(VisualElement root)
    {
        _playerList = root.Q<ScrollView>("player-list");
        _playerSection = root.Q<VisualElement>("player-section");
        _rosterCodexScroll = root.Q<ScrollView>("roster-codex-scroll");
        _templateMemberPicker = root.Q<ScrollView>("template-member-picker");
        _presetSavePicker = root.Q<ScrollView>("preset-save-picker");
        _presetLoadPicker = root.Q<ScrollView>("preset-load-picker");
        _fittingScroll = root.Q<ScrollView>("fitting-scroll");
        _modulePickerPopup = root.Q<VisualElement>("module-picker-popup");
        _ruleLabel = root.Q<Label>("lbl-rule");
        _statusLabel = root.Q<Label>("lbl-status");
        _startBtn = root.Q<Button>("btn-start");

        _lobby = CreateDefaultLobby();
        _templates.Clear();
        _templates.AddRange(SkirmishLobbyCatalog.MemberTemplates());
        _prepCore = SkirmishLobbyPrepCore.TryCreate(_lobby, _ships, _modules);

        _modeRow = root.Q<VisualElement>("mode-row");
        BuildModeRow(_modeRow);
        BuildScaleRow(root.Q<VisualElement>("scale-row"));
        BuildPresetRow(root.Q<VisualElement>("preset-row"));

        OnClick(root, "btn-add-member", ToggleTemplateMemberPicker);
        OnClick(root, "btn-save-preset", TogglePresetSavePicker);
        OnClick(root, "btn-copy-fit", CopyFitToAll);
        OnClick(root, "btn-start", StartMatch);
        OnClick(root, "btn-back", () =>
        {
            CancelMatching();
            GetComponent<UiNavigator>()?.ShowWorldline();
        });

        ApplyModeUi();
        RefreshAll();
    }

    private static SkirmishLobbyState CreateDefaultLobby()
    {
        var lobby = new SkirmishLobbyState { scale = 10, seed = Environment.TickCount, mode = SkirmishLobbyMode.VsAi };
        var human = new LobbyPlayer
        {
            local = true,
            host = true,
            displayName = "玩家",
            memberTemplateId = "template_1",
        };
        lobby.players.Add(human);
        lobby.selectedPlayerId = human.playerId;
        lobby.rosterByPlayerId[human.playerId] = new List<SkirmishRosterSlot>();
        return lobby;
    }

    private void BuildModeRow(VisualElement? row)
    {
        if (row == null)
        {
            return;
        }

        row.Clear();
        AddModeChip(row, "匹配人机对手", SkirmishLobbyMode.VsAi);
        AddModeChip(row, "匹配真人对手", SkirmishLobbyMode.VsHuman);
    }

    private void AddModeChip(VisualElement row, string label, SkirmishLobbyMode mode)
    {
        var btn = new Button { text = label };
        btn.AddToClassList("lobby-chip-btn");
        if (_lobby.mode == mode)
        {
            btn.AddToClassList("lobby-chip-btn-selected");
        }

        var captured = mode;
        btn.clicked += () =>
        {
            if (_lobby.mode == captured)
            {
                return;
            }

            CancelMatching();
            _lobby.mode = captured;
            TrimToLocalPlayerOnly();
            BuildModeRow(_modeRow);
            ApplyModeUi();
            RefreshAll();
        };
        row.Add(btn);
    }

    private void TrimToLocalPlayerOnly()
    {
        var local = _lobby.FindLocal();
        if (local == null)
        {
            return;
        }

        var roster = _lobby.rosterByPlayerId.GetValueOrDefault(local.playerId) ?? new List<SkirmishRosterSlot>();
        _lobby.players.Clear();
        _lobby.players.Add(local);
        _lobby.rosterByPlayerId.Clear();
        _lobby.rosterByPlayerId[local.playerId] = roster;
        _selectedPlayerId = local.playerId;
    }

    private void ApplyModeUi()
    {
        var vsAi = _lobby.mode == SkirmishLobbyMode.VsAi;
        _playerSection?.SetEnabled(vsAi);
        if (_playerSection != null)
        {
            _playerSection.style.display = vsAi ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_startBtn != null)
        {
            _startBtn.text = "开始约战";
            _startBtn.SetEnabled(true);
        }
    }

    private void BuildScaleRow(VisualElement? row)
    {
        if (row == null)
        {
            return;
        }

        row.Clear();
        foreach (var scale in ScaleChoices)
        {
            var btn = new Button { text = scale.ToString() };
            btn.AddToClassList("lobby-chip-btn");
            if (scale == _lobby.scale)
            {
                btn.AddToClassList("lobby-chip-btn-selected");
            }

            var captured = scale;
            btn.clicked += () =>
            {
                CancelMatching();
                _lobby.scale = captured;
                BuildScaleRow(row);
                TrimRosterToCap();
                RefreshAll();
            };
            row.Add(btn);
        }
    }

    private void BuildPresetRow(VisualElement? row)
    {
        if (row == null)
        {
            return;
        }

        row.Clear();
        for (var i = 0; i < SkirmishPresetService.PresetCount; i++)
        {
            var btn = new Button { text = (i + 1).ToString() };
            btn.AddToClassList("lobby-chip-btn");
            var slot = i;
            EventCallback<ClickEvent> handler = evt =>
            {
                CancelMatching();
                if (evt.shiftKey)
                {
                    SavePreset(slot);
                }
                else
                {
                    OpenLoadPresetPicker(slot);
                }
            };
            btn.RegisterCallback(handler);
            _dynamicHandlers.Add((btn, handler));
            row.Add(btn);
        }
    }

    private void SavePreset(int slot)
    {
        PullPrepIntoLobby();
        var key = "skirmish_preset_" + slot;
        PlayerPrefs.SetString(key, SkirmishPresetService.Serialize(_lobby));
        PlayerPrefs.Save();
        SetStatus("已保存配置保存槽 " + (slot + 1));
    }

    private void TogglePresetSavePicker()
    {
        _presetSavePickerVisible = !_presetSavePickerVisible;
        if (_presetSavePickerVisible)
        {
            _templatePickerVisible = false;
            _presetLoadPickerVisible = false;
            if (_templateMemberPicker != null)
            {
                _templateMemberPicker.style.display = DisplayStyle.None;
            }

            if (_presetLoadPicker != null)
            {
                _presetLoadPicker.style.display = DisplayStyle.None;
            }
        }
        if (_presetSavePicker != null)
        {
            _presetSavePicker.style.display = _presetSavePickerVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (_presetSavePickerVisible)
        {
            RefreshPresetSavePicker();
            SetStatus("选择要保存到的配置保存槽");
        }
    }

    private void RefreshPresetSavePicker()
    {
        if (_presetSavePicker == null)
        {
            return;
        }

        _presetSavePicker.Clear();
        for (var i = 0; i < SkirmishPresetService.PresetCount; i++)
        {
            var slot = i;
            var hasData = PlayerPrefs.HasKey("skirmish_preset_" + slot);
            var btn = new Button { text = "槽 " + (slot + 1) + (hasData ? "（覆盖）" : "（空）") };
            btn.AddToClassList("lobby-secondary-btn");
            btn.clicked += () =>
            {
                CancelMatching();
                SavePreset(slot);
                _presetSavePickerVisible = false;
                if (_presetSavePicker != null)
                {
                    _presetSavePicker.style.display = DisplayStyle.None;
                }
            };
            _presetSavePicker.Add(btn);
        }
    }

    private void OpenLoadPresetPicker(int slot)
    {
        var key = "skirmish_preset_" + slot;
        if (!PlayerPrefs.HasKey(key))
        {
            SetStatus("配置保存槽 " + (slot + 1) + " 为空");
            return;
        }

        var loaded = SkirmishPresetService.Deserialize(PlayerPrefs.GetString(key));
        var scheme = SkirmishPresetScheme.Extract(loaded);
        if (scheme == null)
        {
            SetStatus("配置保存槽 " + (slot + 1) + " 解析失败");
            return;
        }

        _presetLoadPickerVisible = true;
        _presetSavePickerVisible = false;
        _templatePickerVisible = false;
        if (_templateMemberPicker != null)
        {
            _templateMemberPicker.style.display = DisplayStyle.None;
        }

        if (_presetSavePicker != null)
        {
            _presetSavePicker.style.display = DisplayStyle.None;
        }

        if (_presetLoadPicker != null)
        {
            _presetLoadPicker.style.display = DisplayStyle.Flex;
        }

        RefreshPresetLoadPicker(slot, scheme);
        SetStatus("配置槽 " + (slot + 1) + " 方案预览");
    }

    private void RefreshPresetLoadPicker(int slot, SkirmishPresetScheme scheme)
    {
        if (_presetLoadPicker == null)
        {
            return;
        }

        _presetLoadPicker.Clear();
        var summary = new Label(SkirmishPresetScheme.FormatSummary(scheme));
        summary.AddToClassList("lobby-section-caption");
        _presetLoadPicker.Add(summary);

        foreach (var line in scheme.RosterLines)
        {
            var hull = string.IsNullOrWhiteSpace(line.HullId) ? "未配舰" : line.HullId;
            _presetLoadPicker.Add(new Label("· " + line.DisplayName + " · " + hull));
        }

        var applyBtn = new Button { text = "应用此方案" };
        applyBtn.AddToClassList("lobby-secondary-btn");
        applyBtn.clicked += () =>
        {
            CancelMatching();
            SkirmishPresetScheme.ApplyToLobby(_lobby, scheme);
            TrimToLocalPlayerOnly();
            ApplyModeUi();
            _presetLoadPickerVisible = false;
            if (_presetLoadPicker != null)
            {
                _presetLoadPicker.style.display = DisplayStyle.None;
            }

            SetStatus("已应用配置槽 " + (slot + 1) + " 方案 · 规模 " + _lobby.scale);
            RefreshAll();
        };
        _presetLoadPicker.Add(applyBtn);
    }

    private void LoadPreset(int slot)
    {
        OpenLoadPresetPicker(slot);
    }

    private void EnsureAiOpponent()
    {
        if (_lobby.players.Any(p => p.kind == LobbyPlayerKind.AI))
        {
            return;
        }

        _lobby.players.Add(new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "规则玩家",
            memberTemplateId = "template_1",
        });
        var ai = _lobby.players[^1];
        _lobby.rosterByPlayerId[ai.playerId] = new List<SkirmishRosterSlot>();
        SkirmishAiRosterGenerator.FillAiRoster(_lobby, _ships, _modules, new System.Random(_lobby.seed));
    }

    private void ToggleTemplateMemberPicker()
    {
        _templatePickerVisible = !_templatePickerVisible;
        if (_templateMemberPicker != null)
        {
            _templateMemberPicker.style.display = _templatePickerVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (_templatePickerVisible)
        {
            RefreshTemplateMemberPicker();
            SetStatus("选择要加入名册的团员");
        }
    }

    private void RefreshTemplateMemberPicker()
    {
        if (_templateMemberPicker == null)
        {
            return;
        }

        _templateMemberPicker.Clear();
        var player = ResolveSelectedPlayer();
        if (player == null)
        {
            _templateMemberPicker.Add(new Label("未选择玩家"));
            return;
        }

        if (!_lobby.rosterByPlayerId.TryGetValue(player.playerId, out var roster))
        {
            roster = new List<SkirmishRosterSlot>();
            _lobby.rosterByPlayerId[player.playerId] = roster;
        }

        var hasAny = false;
        foreach (var template in _templates)
        {
            if (string.IsNullOrWhiteSpace(template.templateId))
            {
                continue;
            }

            var members = StartingTemplateLoader.LoadMembers(template.templateId);
            if (members.Count == 0)
            {
                continue;
            }

            hasAny = true;
            var header = new Label(template.displayName ?? template.templateId);
            header.AddToClassList("lobby-section-caption");
            _templateMemberPicker.Add(header);

            foreach (var member in members)
            {
                var templateId = template.templateId!;
                var rowKey = SkirmishTemplateRows.RowKey(templateId, member);
                var onRoster = SkirmishTemplateRows.IsAlreadyOnRoster(roster, rowKey);
                var btn = new Button
                {
                    text = SkirmishTemplateRows.DisplayLabel(template, member) + (onRoster ? " · 已在名册" : ""),
                };
                btn.AddToClassList("lobby-pick-btn");
                if (onRoster)
                {
                    btn.SetEnabled(false);
                }
                else
                {
                    var capturedTemplate = template;
                    var capturedMember = member;
                    btn.clicked += () => AddMemberFromTemplateRow(capturedTemplate, capturedMember);
                }

                _templateMemberPicker.Add(btn);
            }
        }

        if (!hasAny)
        {
            _templateMemberPicker.Add(new Label("无可用团员模版"));
        }
    }

    private void AddMemberFromTemplateRow(TemplateCatalogEntry template, MemberState src)
    {
        var player = ResolveSelectedPlayer();
        if (player == null || string.IsNullOrWhiteSpace(template.templateId))
        {
            return;
        }

        if (!_lobby.rosterByPlayerId.TryGetValue(player.playerId, out var roster))
        {
            roster = new List<SkirmishRosterSlot>();
            _lobby.rosterByPlayerId[player.playerId] = roster;
        }

        if (roster.Count >= _lobby.scale)
        {
            SetStatus("已达规模上限 " + _lobby.scale);
            return;
        }

        var rowKey = SkirmishTemplateRows.RowKey(template.templateId, src);
        if (SkirmishTemplateRows.IsAlreadyOnRoster(roster, rowKey))
        {
            SetStatus("该团员已在名册中");
            return;
        }

        var slot = new SkirmishRosterSlot
        {
            memberTemplateId = template.templateId,
            memberTemplateRowId = rowKey,
            memberId = "sk_" + Guid.NewGuid().ToString("N")[..8],
            displayName = src.name ?? template.displayName ?? template.templateId,
            hullId = string.IsNullOrWhiteSpace(src.equippedHullId) ? "hull_frigate_pineapple" : src.equippedHullId,
        };
        roster.Add(slot);
        _selectedRosterMemberId = slot.memberId;
        _templatePickerVisible = false;
        if (_templateMemberPicker != null)
        {
            _templateMemberPicker.style.display = DisplayStyle.None;
        }

        SetStatus("已添加 " + slot.displayName);
        EnsurePrepCore();
        RefreshAll();
    }

    private void RemoveRosterMember(string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return;
        }

        var player = ResolveSelectedPlayer();
        if (player == null || !_lobby.rosterByPlayerId.TryGetValue(player.playerId, out var roster))
        {
            return;
        }

        roster.RemoveAll(s => s.memberId == memberId);
        if (_selectedRosterMemberId == memberId)
        {
            _selectedRosterMemberId = roster.Count > 0 ? roster[0].memberId : null;
        }

        EnsurePrepCore();
        RefreshAll();
    }

    private void EnsurePrepCore()
    {
        _prepCore ??= SkirmishLobbyPrepCore.TryCreate(_lobby, _ships, _modules);
        _prepCore?.SyncFromLobby(_lobby);
    }

    private void PullPrepIntoLobby()
    {
        _prepCore?.PullIntoLobby(_lobby);
    }

    private void CopyFitToAll()
    {
        var player = ResolveSelectedPlayer();
        var slot = FindSelectedSlot(player);
        if (player == null || slot == null || !_lobby.rosterByPlayerId.TryGetValue(player.playerId, out var roster))
        {
            return;
        }

        foreach (var other in roster)
        {
            if (other.memberId == slot.memberId)
            {
                continue;
            }

            other.hullId = slot.hullId;
            other.fittedModules = slot.fittedModules.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        EnsurePrepCore();
        SetStatus("已复制配装到全员");
        RefreshAll();
    }

    private void StartMatch()
    {
        if (!ValidateLocalRoster())
        {
            return;
        }

        if (_lobby.mode == SkirmishLobbyMode.VsAi)
        {
            LaunchAiMatch();
            return;
        }

        CancelMatching();
        _matching = true;
        _launching = false;
        _matchBroker ??= new SkirmishMatchBroker();
        _matchBroker.StartSeeking(_lobby.scale);
        if (_startBtn != null)
        {
            _startBtn.text = "匹配中…";
            _startBtn.SetEnabled(false);
        }
        SetRule("正在匹配局域网对手…");
        SetStatus(_matchBroker.LocalIp + " · 规模 " + _lobby.scale);
    }

    private bool ValidateLocalRoster()
    {
        if (!SkirmishRosterValidation.TryValidateLocalStart(_lobby, out var error))
        {
            SetRule(error ?? "名册无效");
            return false;
        }

        SetRule("");
        return true;
    }

    private void LaunchAiMatch()
    {
        try
        {
            TrimToLocalPlayerOnly();
            EnsureAiOpponent();
            SkirmishAiRosterGenerator.FillAiRoster(_lobby, _ships, _modules, new System.Random(_lobby.seed));
            PullPrepIntoLobby();
            GameAppHost.Instance?.StartFromSkirmishLobby(_lobby);
            GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.CombatRealtime);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            SetRule("启动失败: " + e.Message);
        }
    }

    private void LaunchHumanMatch(SkirmishMatchSnapshot snap)
    {
        if (_launching || snap.PeerIp == null || snap.HostIp == null)
        {
            return;
        }

        _launching = true;
        try
        {
            CancelMatching();
            TrimToLocalPlayerOnly();
            EnsurePeerPlayer(snap.PeerIp, snap.IsLocalHost);
            _lobby.seed = SkirmishMatchLogic.StableHash(_matchBroker?.LocalIp + "|" + snap.PeerIp + "|" + _lobby.scale);
            PullPrepIntoLobby();
            if (snap.IsLocalHost)
            {
                GameAppHost.Instance?.StartFromSkirmishLobby(_lobby);
                GameAppHost.Instance?.StartLanHost(GameAppHost.DefaultTcpGamePort);
            }
            else
            {
                GameAppHost.Instance?.ConnectLanGuest(snap.PeerIp, GameAppHost.DefaultTcpGamePort);
            }

            GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.CombatRealtime);
        }
        catch (Exception e)
        {
            _launching = false;
            Debug.LogError(e);
            SetRule("联网启动失败: " + e.Message);
            if (_startBtn != null)
            {
                _startBtn.text = "开始约战";
                _startBtn.SetEnabled(true);
            }
        }
    }

    private void EnsurePeerPlayer(string peerIp, bool localIsHost)
    {
        if (_lobby.players.Any(p => peerIp.Equals(p.remoteHostIp, StringComparison.Ordinal)))
        {
            return;
        }

        _lobby.players.Add(new LobbyPlayer
        {
            kind = LobbyPlayerKind.HUMAN,
            displayName = "对手",
            remoteHostIp = peerIp,
            local = false,
            host = !localIsHost,
        });
        _lobby.rosterByPlayerId[_lobby.players[^1].playerId] = new List<SkirmishRosterSlot>();
    }

    private void CancelMatching()
    {
        _matching = false;
        _launching = false;
        _matchBroker?.Stop();
    }

    private void TrimRosterToCap()
    {
        foreach (var kv in _lobby.rosterByPlayerId.ToList())
        {
            while (kv.Value.Count > _lobby.scale)
            {
                kv.Value.RemoveAt(kv.Value.Count - 1);
            }
        }
    }

    private void RefreshAll()
    {
        EnsurePrepCore();
        RefreshPlayers();
        RefreshRosterCodex();
        RefreshFittingPanel();
        UpdateStartRule();
    }

    private void RefreshPlayers()
    {
        if (_playerList == null)
        {
            return;
        }

        _playerList.Clear();
        foreach (var p in _lobby.players)
        {
            var label = p.displayName + (p.kind == LobbyPlayerKind.AI ? " [AI]" : "") + (p.local ? " · 本机" : "");
            _playerList.Add(new Label(label));
        }
    }

    private void RefreshRosterCodex()
    {
        if (_rosterCodexScroll == null || _prepCore == null)
        {
            return;
        }

        SkirmishRosterCodexPanel.Populate(
            _rosterCodexScroll,
            _prepCore,
            _selectedRosterMemberId,
            memberId => { _selectedRosterMemberId = memberId; },
            SetStatus,
            RefreshAll,
            RemoveRosterMember);
    }

    private void RefreshFittingPanel()
    {
        if (_fittingScroll == null)
        {
            return;
        }

        if (_prepCore == null)
        {
            _fittingScroll.Clear();
            _fittingScroll.contentContainer.Add(new Label("无法初始化配船环境"));
            return;
        }

        PullPrepIntoLobby();
        _prepCore.SyncFromLobby(_lobby);

        if (_selectedRosterMemberId == null && _lobby.rosterByPlayerId.TryGetValue(_prepCore.LocalLegionId, out var roster) && roster.Count > 0)
        {
            _selectedRosterMemberId = roster[0].memberId;
        }

        var member = _prepCore.FindMember(_selectedRosterMemberId);
        if (member == null)
        {
            ShipFittingPanel.HideModulePicker(_modulePickerPopup);
            _fittingScroll.Clear();
            _fittingScroll.contentContainer.Add(new Label("选择团员以编辑圆环配船"));
            return;
        }

        ShipFittingPanel.Populate(
            _fittingScroll,
            _modulePickerPopup,
            _prepCore.Core,
            member,
            SetStatus,
            () =>
            {
                PullPrepIntoLobby();
                RefreshAll();
            });
    }

    private LobbyPlayer? ResolveSelectedPlayer() =>
        _lobby.players.Find(p => p.playerId == _selectedPlayerId) ?? _lobby.FindLocal();

    private SkirmishRosterSlot? FindSelectedSlot(LobbyPlayer? player)
    {
        if (player == null || !_lobby.rosterByPlayerId.TryGetValue(player.playerId, out var roster))
        {
            return null;
        }

        return roster.Find(s => s.memberId == _selectedRosterMemberId) ?? roster.FirstOrDefault();
    }

    private void UpdateStartRule()
    {
        if (_lobby.mode == SkirmishLobbyMode.VsAi)
        {
            SetRule("人机模式：配好名册后点击开始");
            return;
        }

        SetRule("真人模式：各端本地配名册 · 开始后自动匹配 · 规模需一致");
    }

    private void SetRule(string msg)
    {
        if (_ruleLabel != null)
        {
            _ruleLabel.text = msg;
        }
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }

    private void ClearDynamicHandlers()
    {
        foreach (var (btn, handler) in _dynamicHandlers)
        {
            btn?.UnregisterCallback(handler);
        }

        _dynamicHandlers.Clear();
    }
}
