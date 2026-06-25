using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.Client.StarMap;
using TopDog.Content.Assets;
using TopDog.Content.Map;
using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Net.Lan;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Custom skirmish lobby: map / templates / spawn / LAN per CUSTOM_LOBBY.md.</summary>
public sealed class CustomLobbyController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.CustomLobby;

    protected override bool UseSafeAreaInsets => false;

    private CustomLobbyState _lobby = new();
    private readonly List<MapCatalogEntry> _maps = new();
    private readonly List<TemplateCatalogEntry> _templates = new();
    private readonly List<AssetCatalogEntry> _assets = new();
    private readonly List<(Button btn, EventCallback<ClickEvent> handler)> _dynamicHandlers = new();

    private LoadedMap? _loadedMap;
    private readonly StarMapPreviewPanel _starMapPreview = new();
    private LanLobbyBeacon? _beacon;
    private LanJoinClient? _joinClient;
    private bool _joinMode;
    private string? _joinHostIp;
    private string? _joinMapHint;
    private int _lastPlayerCount;

    private ScrollView? _playerList;
    private ScrollView? _mapList;
    private ScrollView? _templateList;
    private ScrollView? _assetList;
    private ScrollView? _spawnList;
    private VisualElement? _randomMapOptions;
    private VisualElement? _systemCountRow;
    private VisualElement? _bridgeDensityRow;
    private Slider? _bridgeDensitySlider;
    private Label? _bridgeDensityLabel;
    private Button? _viewTopBtn;
    private Button? _viewSideBtn;
    private Button? _viewFrontBtn;
    private Label? _ruleLabel;
    private Label? _spawnHint;
    private Label? _statusLabel;
    private Button? _startBtn;
    private Button? _addAiBtn;

    private static readonly int[] SystemCountChoices = { 12, 16, 20, 24, 30, 40, 50 };

    private static string BridgeDensityLabel(float density)
    {
        var rounded = ProceduralMapOptions.RoundBridgeDensity(density);
        var tag = rounded switch
        {
            <= 0.3f => "稀疏",
            <= 0.55f => "较疏",
            <= 1.05f => "标准",
            <= 1.55f => "较密",
            <= 2.05f => "密集",
            _ => "极密",
        };
        return rounded.ToString("0.00") + " " + tag;
    }

    protected override void OnDisable()
    {
        ClearDynamicHandlers();
        _beacon?.Dispose();
        _beacon = null;
        _joinClient?.Dispose();
        _joinClient = null;
        _starMapPreview.Detach();
        base.OnDisable();
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        UpdateLanStatus();
    }

    protected override void Bind(VisualElement root)
    {
        _beacon?.Dispose();
        _beacon = null;
        _joinClient?.Dispose();
        _joinClient = null;
        _lobby = new CustomLobbyState();

        var launch = GetComponent<UiNavigator>()?.ConsumeLobbyLaunchArgs() ?? CustomLobbyLaunchArgs.Host();
        _joinMode = launch.JoinMode;
        _joinHostIp = launch.JoinHostIp;
        _joinMapHint = launch.JoinMapHint;

        _playerList = root.Q<ScrollView>("player-list");
        _mapList = root.Q<ScrollView>("map-list");
        _randomMapOptions = root.Q<VisualElement>("random-map-options");
        _systemCountRow = root.Q<VisualElement>("system-count-row");
        _bridgeDensityRow = root.Q<VisualElement>("bridge-density-row");
        _bridgeDensitySlider = root.Q<Slider>("bridge-density-slider");
        _bridgeDensityLabel = root.Q<Label>("lbl-bridge-density");
        if (_bridgeDensitySlider != null)
        {
            _bridgeDensitySlider.lowValue = ProceduralMapOptions.MinBridgeDensity;
            _bridgeDensitySlider.highValue = ProceduralMapOptions.MaxBridgeDensity;
            _bridgeDensitySlider.RegisterValueChangedCallback(OnBridgeDensitySliderChanged);
        }
        _viewTopBtn = root.Q<Button>("btn-view-top");
        _viewSideBtn = root.Q<Button>("btn-view-side");
        _viewFrontBtn = root.Q<Button>("btn-view-front");
        _spawnList = root.Q<ScrollView>("spawn-list");
        _templateList = root.Q<ScrollView>("template-list");
        _assetList = root.Q<ScrollView>("asset-list");
        _ruleLabel = root.Q<Label>("lbl-rule");
        _spawnHint = root.Q<Label>("lbl-spawn-hint");
        _statusLabel = root.Q<Label>("lbl-status");
        _startBtn = root.Q<Button>("btn-start");
        _addAiBtn = root.Q<Button>("btn-add-ai");

        var mapHost = root.Q<VisualElement>("star-map-host");
        if (mapHost != null)
        {
            _starMapPreview.Attach(mapHost);
        }

        try
        {
            InitCatalogs();
            SeedLocalPlayer();
            ApplyJoinMapHint();
            ApplyInitialMap();
            WireStarMapViewButtons(root);
            StartLan();
            ApplyJoinModeUi();
            RefreshAll();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            SetStatus("大厅加载失败: " + e.Message);
        }

        OnClick(root, "btn-add-ai", AddAiPlayer);
        OnClick(root, "btn-start", StartCampaign);
        OnClick(root, "btn-regenerate-map", RegenerateProceduralMap);
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowWorldline());
    }

    private void InitCatalogs()
    {
        _maps.Clear();
        _maps.AddRange(ContentCatalog.ListMaps());
        _templates.Clear();
        _templates.AddRange(ContentCatalog.ListLobbyMemberTemplates());
        _assets.Clear();
        foreach (var a in ContentCatalog.ListAssetTemplates())
        {
            if (LobbyCatalogConstants.DefaultTestAssetId.Equals(a.assetTemplateId, StringComparison.Ordinal))
            {
                _assets.Add(a);
            }
        }
        if (_assets.Count == 0)
        {
            foreach (var a in ContentCatalog.ListAssetTemplates())
            {
                _assets.Add(a);
                break;
            }
        }
    }

    private void SeedLocalPlayer()
    {
        _lobby.players.Clear();
        var local = new LobbyPlayer
        {
            local = true,
            host = !_joinMode,
            kind = LobbyPlayerKind.HUMAN,
            displayName = LocalNetworkUtil.LocalIpv4(),
        };
        ApplyDefaultTemplates(local);
        _lobby.players.Add(local);
        _lobby.selectedPlayerId = local.playerId;
    }

    private void ApplyJoinMapHint()
    {
        if (!_joinMode || string.IsNullOrWhiteSpace(_joinMapHint))
        {
            return;
        }
        foreach (var m in _maps)
        {
            if (_joinMapHint.Equals(m.id, StringComparison.Ordinal)
                || _joinMapHint.Equals(System.IO.Path.GetFileName(m.path), StringComparison.OrdinalIgnoreCase)
                || (_joinMapHint.Equals(m.displayName, StringComparison.Ordinal)))
            {
                _lobby.mapPath = m.path;
                _lobby.mapDisplayName = m.displayName;
                return;
            }
        }
    }

    private void StartLan()
    {
        if (_joinMode && !string.IsNullOrWhiteSpace(_joinHostIp))
        {
            try
            {
                _joinClient = new LanJoinClient(_joinHostIp, _lobby.lanPort);
                _joinClient.Start();
                SetStatus("正在加入 " + _joinHostIp + "…");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                SetStatus("加入失败: " + e.Message);
            }
        }
        else if (!_joinMode)
        {
            try
            {
                _beacon = new LanLobbyBeacon(_lobby);
                _beacon.Start();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                SetStatus("局域网广播失败（端口占用？）");
            }
        }
    }

    private void ApplyJoinModeUi()
    {
        if (_addAiBtn != null)
        {
            _addAiBtn.SetEnabled(!_joinMode);
            _addAiBtn.style.display = _joinMode ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_startBtn != null)
        {
            _startBtn.text = _joinMode ? "连接 Host" : "开始战役";
            _startBtn.SetEnabled(true);
        }
    }

    private void UpdateLanStatus()
    {
        if (_beacon != null)
        {
            var prev = _lastPlayerCount;
            _beacon.SyncDiscoveredHumans();
            if (_lobby.players.Count != prev)
            {
                _lastPlayerCount = _lobby.players.Count;
                RefreshAll();
            }
            SetStatus("局域网广播中 · IP " + _beacon.LocalIp + " · 玩家 " + _lobby.players.Count + " 人");
        }
        else if (_joinMode && !string.IsNullOrWhiteSpace(_joinHostIp))
        {
            SetStatus("已加入 " + _joinHostIp + " · 等待房主开始");
        }
    }

    private void ApplyInitialMap()
    {
        if (_lobby.mapPath == null && _maps.Count > 0)
        {
            var m = _maps[0];
            _lobby.proceduralMap = IsProceduralMapEntry(m);
            _lobby.mapPath = m.path;
            _lobby.mapDisplayName = m.displayName;
            if (_lobby.proceduralMap && _lobby.proceduralSeed == 0)
            {
                _lobby.proceduralSeed = Environment.TickCount;
            }
        }
        ReloadMapPreview();
        foreach (var p in _lobby.players)
        {
            if (p.spawnSolarSystemId == null && _loadedMap != null)
            {
                p.spawnSolarSystemId = ContentCatalog.DefaultSpawnForMap(_loadedMap, AssetFor(p.assetTemplateId));
            }
        }
    }

    private void WireStarMapViewButtons(VisualElement root)
    {
        OnClick(root, "btn-view-top", () => SetStarMapProjection(StarMapPreviewProjection.TopDownXz));
        OnClick(root, "btn-view-side", () => SetStarMapProjection(StarMapPreviewProjection.SideXy));
        OnClick(root, "btn-view-front", () => SetStarMapProjection(StarMapPreviewProjection.FrontYz));
    }

    private void SetStarMapProjection(StarMapPreviewProjection projection)
    {
        _starMapPreview.SetProjection(projection);
        UpdateStarMapViewButtons();
    }

    private void UpdateStarMapViewButtons()
    {
        UpdateViewBtn(_viewTopBtn, _starMapPreview.Projection == StarMapPreviewProjection.TopDownXz);
        UpdateViewBtn(_viewSideBtn, _starMapPreview.Projection == StarMapPreviewProjection.SideXy);
        UpdateViewBtn(_viewFrontBtn, _starMapPreview.Projection == StarMapPreviewProjection.FrontYz);
    }

    private static void UpdateViewBtn(Button? btn, bool selected)
    {
        if (btn == null)
        {
            return;
        }
        if (selected)
        {
            btn.AddToClassList("lobby-view-btn-selected");
        }
        else
        {
            btn.RemoveFromClassList("lobby-view-btn-selected");
        }
    }

    private void RebuildRandomOptionChips()
    {
        if (_systemCountRow != null)
        {
            _systemCountRow.Clear();
            foreach (var count in SystemCountChoices)
            {
                var btn = new Button { text = count.ToString() };
                btn.AddToClassList("lobby-option-chip");
                if (_lobby.proceduralSystemCount == count)
                {
                    btn.AddToClassList("lobby-option-chip-selected");
                }
                if (_joinMode || !_lobby.proceduralMap)
                {
                    btn.SetEnabled(false);
                }
                else
                {
                    var value = count;
                    EventCallback<ClickEvent> handler = _ => ApplyRandomSystemCount(value);
                    btn.RegisterCallback(handler);
                    _dynamicHandlers.Add((btn, handler));
                }
                _systemCountRow.Add(btn);
            }
        }

        if (_bridgeDensityRow != null)
        {
            var density = ProceduralMapOptions.RoundBridgeDensity(_lobby.proceduralBridgeDensity);
            _lobby.proceduralBridgeDensity = density;
            if (_bridgeDensitySlider != null)
            {
                _bridgeDensitySlider.SetValueWithoutNotify(density);
                _bridgeDensitySlider.SetEnabled(!_joinMode && _lobby.proceduralMap);
            }
            if (_bridgeDensityLabel != null)
            {
                _bridgeDensityLabel.text = BridgeDensityLabel(density);
            }
            _bridgeDensityRow.style.display = _lobby.proceduralMap && !_joinMode
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    private void OnBridgeDensitySliderChanged(ChangeEvent<float> evt)
    {
        if (_joinMode || !_lobby.proceduralMap)
        {
            return;
        }
        var density = ProceduralMapOptions.RoundBridgeDensity(evt.newValue);
        if (Math.Abs(density - evt.newValue) > 0.001f && _bridgeDensitySlider != null)
        {
            _bridgeDensitySlider.SetValueWithoutNotify(density);
        }
        ApplyRandomBridgeDensity(density);
    }

    private void ApplyRandomSystemCount(int count)
    {
        if (_joinMode || !_lobby.proceduralMap)
        {
            return;
        }
        _lobby.proceduralSystemCount = count;
        OnRandomOptionsChanged(reroll: true);
    }

    private void ApplyRandomBridgeDensity(float density)
    {
        if (_joinMode || !_lobby.proceduralMap)
        {
            return;
        }
        _lobby.proceduralBridgeDensity = ProceduralMapOptions.RoundBridgeDensity(density);
        if (_bridgeDensityLabel != null)
        {
            _bridgeDensityLabel.text = BridgeDensityLabel(_lobby.proceduralBridgeDensity);
        }
        OnRandomOptionsChanged(reroll: false);
    }

    private void OnRandomOptionsChanged(bool reroll)
    {
        if (!_lobby.proceduralMap)
        {
            return;
        }
        if (reroll || _lobby.proceduralSeed == 0)
        {
            _lobby.proceduralSeed = Environment.TickCount;
        }
        ReloadMapPreview();
        RefreshAll();
    }

    private void RegenerateProceduralMap()
    {
        if (_joinMode || !_lobby.proceduralMap)
        {
            return;
        }
        _lobby.proceduralSeed = Environment.TickCount;
        ReloadMapPreview();
        RefreshAll();
        SetStatus("已重新生成随机星图 · 种子 " + _lobby.proceduralSeed);
    }

    private void UpdateRandomMapOptionsVisibility()
    {
        if (_randomMapOptions == null)
        {
            return;
        }
        if (_lobby.proceduralMap && !_joinMode)
        {
            _randomMapOptions.AddToClassList("lobby-random-options-visible");
        }
        else
        {
            _randomMapOptions.RemoveFromClassList("lobby-random-options-visible");
        }
    }

    private static bool IsProceduralMapEntry(MapCatalogEntry m)
        => m.procedural || MapCatalogEntry.ProceduralMapId.Equals(m.id, StringComparison.Ordinal);

    private static bool MapEntryMatchesLobby(MapCatalogEntry m, CustomLobbyState lobby)
    {
        if (IsProceduralMapEntry(m))
        {
            return lobby.proceduralMap;
        }
        return lobby.mapPath != null && lobby.mapPath.Equals(m.path, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshAll()
    {
        RebuildPlayerList();
        RebuildMapList();
        RebuildTemplateList();
        RebuildAssetList();
        RebuildSpawnList();
        RefreshStartRule();
        RefreshSpawnHint();
        UpdateMapHighlight();
        UpdateRandomMapOptionsVisibility();
        RebuildRandomOptionChips();
        UpdateStarMapViewButtons();
        _lastPlayerCount = _lobby.players.Count;
    }

    private void RebuildTemplateList()
    {
        if (_templateList == null)
        {
            return;
        }

        _templateList.Clear();
        var sel = _lobby.FindSelected();

        AddPickButton(_templateList, LobbyCatalogConstants.RandomChoiceLabel,
            sel != null && LobbyCatalogConstants.IsRandomMember(sel.memberTemplateId),
            _joinMode ? null : () => SelectMemberTemplate(LobbyCatalogConstants.RandomMemberTemplateId));

        foreach (var t in _templates)
        {
            if (string.IsNullOrWhiteSpace(t.templateId))
            {
                continue;
            }
            var templateId = t.templateId;
            var label = t.displayName ?? templateId;
            AddPickButton(_templateList, label,
                sel != null && templateId.Equals(sel.memberTemplateId, StringComparison.Ordinal),
                _joinMode ? null : () => SelectMemberTemplate(templateId));
        }
    }

    private void RebuildAssetList()
    {
        if (_assetList == null)
        {
            return;
        }

        _assetList.Clear();
        var sel = _lobby.FindSelected();

        AddPickButton(_assetList, LobbyCatalogConstants.RandomChoiceLabel,
            sel != null && LobbyCatalogConstants.IsRandomAsset(sel.assetTemplateId),
            _joinMode ? null : () => SelectAssetTemplate(LobbyCatalogConstants.RandomAssetTemplateId));

        foreach (var a in _assets)
        {
            if (string.IsNullOrWhiteSpace(a.assetTemplateId))
            {
                continue;
            }
            var assetId = a.assetTemplateId;
            var label = AssetDisplayName(a);
            AddPickButton(_assetList, label,
                sel != null && assetId.Equals(sel.assetTemplateId, StringComparison.Ordinal),
                _joinMode ? null : () => SelectAssetTemplate(assetId!));
        }
    }

    private void AddPickButton(ScrollView list, string label, bool selected, Action? onClick)
    {
        var btn = new Button { text = label };
        btn.AddToClassList("lobby-pick-btn");
        if (selected)
        {
            btn.AddToClassList("lobby-pick-btn-selected");
        }
        if (onClick == null)
        {
            btn.SetEnabled(false);
        }
        else
        {
            EventCallback<ClickEvent> handler = _ => onClick();
            btn.RegisterCallback(handler);
            _dynamicHandlers.Add((btn, handler));
        }
        list.Add(btn);
    }

    private void SelectMemberTemplate(string templateId)
    {
        var sel = _lobby.FindSelected();
        if (sel == null || _joinMode)
        {
            return;
        }
        sel.memberTemplateId = templateId;
        if (!LobbyCatalogConstants.IsRandomMember(templateId))
        {
            foreach (var t in _templates)
            {
                if (templateId.Equals(t.templateId, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(t.assetTemplateId)
                    && !LobbyCatalogConstants.IsRandomAsset(sel.assetTemplateId))
                {
                    sel.assetTemplateId = t.assetTemplateId!;
                }
            }
        }
        RefreshAll();
    }

    private void SelectAssetTemplate(string assetTemplateId)
    {
        var sel = _lobby.FindSelected();
        if (sel == null || _joinMode)
        {
            return;
        }
        sel.assetTemplateId = assetTemplateId;
        if (LobbyCatalogConstants.IsRandomAsset(assetTemplateId) && _loadedMap != null)
        {
            sel.spawnSolarSystemId = LobbyRandomBootstrap.PickRandomSpawnSystem(_loadedMap);
        }
        else if (sel.spawnSolarSystemId == null && _loadedMap != null)
        {
            sel.spawnSolarSystemId = ContentCatalog.DefaultSpawnForMap(_loadedMap, AssetFor(assetTemplateId));
        }
        RefreshAll();
    }

    private static string AssetDisplayName(AssetCatalogEntry a)
    {
        if (LobbyCatalogConstants.DefaultTestAssetId.Equals(a.assetTemplateId, StringComparison.Ordinal))
        {
            return LobbyCatalogConstants.DefaultTestAssetDisplayName;
        }
        return a.displayName ?? a.assetTemplateId ?? "?";
    }

    private void RebuildPlayerList()
    {
        if (_playerList == null)
        {
            return;
        }
        ClearDynamicHandlers();
        _playerList.Clear();
        foreach (var p in _lobby.players)
        {
            var label = p.displayName;
            if (p.kind == LobbyPlayerKind.AI)
            {
                label += " [AI]";
            }
            else if (p.local)
            {
                label += " (本机)";
            }
            else
            {
                label += " [远程]";
            }
            if (!string.IsNullOrWhiteSpace(p.spawnSolarSystemId))
            {
                label += " · " + SystemDisplayName(p.spawnSolarSystemId);
            }
            var btn = new Button { text = label };
            btn.AddToClassList("lobby-player-btn");
            if (_lobby.FindSelected()?.playerId == p.playerId)
            {
                btn.AddToClassList("lobby-player-btn-selected");
            }
            var player = p;
            EventCallback<ClickEvent> handler = _ =>
            {
                _lobby.selectedPlayerId = player.playerId;
                RefreshAll();
            };
            btn.RegisterCallback(handler);
            _dynamicHandlers.Add((btn, handler));
            _playerList.Add(btn);
        }
    }

    private void RebuildMapList()
    {
        if (_mapList == null)
        {
            return;
        }

        _mapList.Clear();
        for (var i = 0; i < _maps.Count; i++)
        {
            var m = _maps[i];
            var label = m.displayName ?? m.id ?? "?";
            var btn = new Button { text = label };
            btn.AddToClassList("lobby-map-btn");
            if (_lobby.mapPath != null && _lobby.mapPath.Equals(m.path, StringComparison.OrdinalIgnoreCase))
            {
                btn.AddToClassList("lobby-map-btn-selected");
            }
            else if (IsProceduralMapEntry(m) && _lobby.proceduralMap)
            {
                btn.AddToClassList("lobby-map-btn-selected");
            }

            if (_joinMode)
            {
                btn.SetEnabled(false);
            }
            else
            {
                var idx = i;
                EventCallback<ClickEvent> handler = _ => SelectMapByIndex(idx);
                btn.RegisterCallback(handler);
                _dynamicHandlers.Add((btn, handler));
            }

            _mapList.Add(btn);
        }
    }

    private void SelectMapByIndex(int idx)
    {
        if (_joinMode || idx < 0 || idx >= _maps.Count)
        {
            return;
        }

        var m = _maps[idx];
        _lobby.proceduralMap = IsProceduralMapEntry(m);
        _lobby.mapPath = m.path;
        _lobby.mapDisplayName = m.displayName;
        if (_lobby.proceduralMap && _lobby.proceduralSeed == 0)
        {
            _lobby.proceduralSeed = Environment.TickCount;
        }
        ReloadMapPreview();
        foreach (var p in _lobby.players)
        {
            if (p.spawnSolarSystemId == null || FindSystemName(p.spawnSolarSystemId) == null)
            {
                p.spawnSolarSystemId = _loadedMap != null
                    ? ContentCatalog.DefaultSpawnForMap(_loadedMap, AssetFor(p.assetTemplateId))
                    : null;
            }
        }

        RefreshAll();
    }

    private void RebuildSpawnList()
    {
        if (_spawnList == null || _loadedMap == null)
        {
            _spawnList?.Clear();
            return;
        }
        _spawnList.Clear();
        var sel = _lobby.FindSelected();
        foreach (var sys in _loadedMap.Project.systems)
        {
            if (sys.solarSystemId == null)
            {
                continue;
            }
            var taken = IsSpawnTakenByOther(sys.solarSystemId, sel);
            var name = sys.name ?? sys.solarSystemId;
            var btn = new Button { text = taken ? name + " (已占用)" : name };
            btn.AddToClassList("lobby-spawn-btn");
            if (sel?.spawnSolarSystemId == sys.solarSystemId)
            {
                btn.AddToClassList("lobby-spawn-btn-selected");
            }
            if (taken)
            {
                btn.AddToClassList("lobby-spawn-btn-disabled");
                btn.SetEnabled(false);
            }
            else
            {
                var systemId = sys.solarSystemId;
                EventCallback<ClickEvent> handler = _ => AssignSpawn(systemId);
                btn.RegisterCallback(handler);
                _dynamicHandlers.Add((btn, handler));
            }
            _spawnList.Add(btn);
        }
    }

    private void AssignSpawn(string systemId)
    {
        var sel = _lobby.FindSelected();
        if (sel == null || IsSpawnTakenByOther(systemId, sel))
        {
            return;
        }
        sel.spawnSolarSystemId = systemId;
        RefreshAll();
    }

    private void AddAiPlayer()
    {
        if (_joinMode)
        {
            return;
        }
        if (_lobby.players.Count >= _lobby.maxPlayers)
        {
            SetStatus("玩家已满");
            return;
        }
        var n = _lobby.NextAiNumber();
        var ai = new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "人机 " + n,
        };
        ApplyDefaultTemplates(ai);
        if (_loadedMap != null && !string.IsNullOrEmpty(_loadedMap.Project.systems.FirstOrDefault()?.solarSystemId))
        {
            ai.spawnSolarSystemId = PickUnusedSpawn();
        }
        _lobby.players.Add(ai);
        _lobby.selectedPlayerId = ai.playerId;
        RefreshAll();
    }

    private void StartCampaign()
    {
        if (_joinMode)
        {
            ConnectAsGuest();
            return;
        }
        if (_lobby.players.Count < 2)
        {
            SetStatus("至少需要 2 名玩家（可添加人机）");
            return;
        }
        if (string.IsNullOrWhiteSpace(_lobby.mapPath))
        {
            SetStatus("请选择地图");
            return;
        }
        try
        {
            LobbyMapSpawnService.SyncProceduralFlag(_lobby);
            if (_lobby.proceduralMap && _lobby.proceduralSeed == 0)
            {
                _lobby.proceduralSeed = Environment.TickCount;
            }
            var map = ContentCatalog.ResolveLobbyMap(_lobby);
            LobbyMapSpawnService.EnsureValidSpawns(_lobby, map);
            _loadedMap = map;
            _beacon?.Dispose();
            _beacon = null;
            GameAppHost.Instance?.StartFromLobby(_lobby);
            try
            {
                GameAppHost.Instance?.StartLanHost(GameAppHost.DefaultTcpGamePort);
            }
            catch (Exception lanEx)
            {
                Debug.LogWarning("LAN Host 未启动: " + lanEx.Message);
            }
            GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.Operations);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            SetStatus("启动失败: " + e.Message);
        }
    }

    private void ConnectAsGuest()
    {
        if (string.IsNullOrWhiteSpace(_joinHostIp))
        {
            SetStatus("未选择 Host");
            return;
        }
        try
        {
            _joinClient?.Dispose();
            _joinClient = null;
            GameAppHost.Instance?.ConnectLanGuest(_joinHostIp, GameAppHost.DefaultTcpGamePort);
            GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.Operations);
            SetStatus("已连接 " + _joinHostIp + " · 同步 Host 状态");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            SetStatus("连接 Host 失败: " + e.Message);
        }
    }

    private void ReloadMapPreview()
    {
        if (_lobby.proceduralMap)
        {
            try
            {
                _loadedMap = ContentCatalog.GenerateProceduralPreview(_lobby);
                _lobby.mapDisplayName = _loadedMap.Project.projectName;
                _starMapPreview.LoadMap(_loadedMap);
                AssignDefaultSpawnsAfterMapChange();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                _loadedMap = null;
                _starMapPreview.LoadMap(null);
                SetStatus("随机星图生成失败: " + e.Message);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(_lobby.mapPath))
        {
            _loadedMap = null;
            _starMapPreview.LoadMap(null);
            return;
        }
        try
        {
            _loadedMap = ContentCatalog.LoadMap(_lobby.mapPath);
            _starMapPreview.LoadMap(_loadedMap);
            AssignDefaultSpawnsAfterMapChange();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            _loadedMap = null;
            _starMapPreview.LoadMap(null);
            SetStatus("地图加载失败: " + e.Message);
        }
    }

    private void AssignDefaultSpawnsAfterMapChange()
    {
        var local = _lobby.FindLocal();
        if (local != null && local.spawnSolarSystemId == null)
        {
            local.spawnSolarSystemId = ContentCatalog.DefaultSpawnForMap(_loadedMap!, AssetFor(local.assetTemplateId));
        }
    }

    private void RefreshStartRule()
    {
        if (_joinMode)
        {
            if (_ruleLabel != null)
            {
                _ruleLabel.style.display = DisplayStyle.Flex;
                _ruleLabel.text = "客人模式：地图与开始由房主控制";
            }
            _startBtn?.SetEnabled(false);
            return;
        }
        var countOk = _lobby.players.Count >= 2;
        var mapOk = !string.IsNullOrWhiteSpace(_lobby.mapPath);
        var ok = countOk && mapOk;
        if (_ruleLabel != null)
        {
            if (ok)
            {
                _ruleLabel.style.display = DisplayStyle.None;
            }
            else
            {
                _ruleLabel.style.display = DisplayStyle.Flex;
                if (!countOk)
                {
                    _ruleLabel.text = "需要 ≥2 名玩家（含 1 人 + 1 人机）方可开始";
                }
                else if (!mapOk)
                {
                    _ruleLabel.text = "请先选择地图";
                }
            }
        }
        if (_startBtn != null)
        {
            _startBtn.SetEnabled(ok);
        }
    }

    private void RefreshSpawnHint()
    {
        if (_spawnHint == null)
        {
            return;
        }
        var sel = _lobby.FindSelected();
        if (sel == null)
        {
            _spawnHint.text = "请先在左侧选择玩家";
            return;
        }
        if (string.IsNullOrWhiteSpace(sel.spawnSolarSystemId))
        {
            _spawnHint.text = "为「" + sel.displayName + "」选择出生星系";
            return;
        }
        _spawnHint.text = sel.displayName + " 出生在 " + SystemDisplayName(sel.spawnSolarSystemId);
    }

    private void UpdateMapHighlight()
    {
        var sel = _lobby.FindSelected();
        _starMapPreview.SetHighlightedSystem(sel?.spawnSolarSystemId);
    }

    private void ApplyDefaultTemplates(LobbyPlayer player)
    {
        if (player.kind == LobbyPlayerKind.AI)
        {
            player.memberTemplateId = LobbyRandomBootstrap.PickRandomMemberTemplateId(_templates);
        }
        else
        {
            var t = PreferTemplate("template_1") ?? (_templates.Count > 0 ? _templates[0] : null);
            player.memberTemplateId = t?.templateId ?? "template_1";
        }
        var picked = PreferTemplate(player.memberTemplateId);
        if (!string.IsNullOrWhiteSpace(picked?.assetTemplateId))
        {
            player.assetTemplateId = picked.assetTemplateId!;
        }
        else
        {
            player.assetTemplateId = LobbyCatalogConstants.DefaultTestAssetId;
        }
    }

    private TemplateCatalogEntry? PreferTemplate(string templateId)
    {
        foreach (var t in _templates)
        {
            if (templateId.Equals(t.templateId, StringComparison.Ordinal))
            {
                return t;
            }
        }
        return null;
    }

    private AssetCatalogEntry? AssetFor(string? assetTemplateId)
    {
        if (string.IsNullOrWhiteSpace(assetTemplateId)
            || LobbyCatalogConstants.IsRandomAsset(assetTemplateId))
        {
            return null;
        }
        foreach (var a in _assets)
        {
            if (assetTemplateId.Equals(a.assetTemplateId, StringComparison.Ordinal))
            {
                return a;
            }
        }
        return StartingAssetLoader.LoadEntry(assetTemplateId);
    }

    private string? PickUnusedSpawn()
    {
        if (_loadedMap == null)
        {
            return null;
        }
        foreach (var s in _loadedMap.Project.systems)
        {
            if (s.solarSystemId == null)
            {
                continue;
            }
            var used = false;
            foreach (var p in _lobby.players)
            {
                if (s.solarSystemId.Equals(p.spawnSolarSystemId, StringComparison.Ordinal))
                {
                    used = true;
                    break;
                }
            }
            if (!used)
            {
                return s.solarSystemId;
            }
        }
        return _loadedMap.Project.systems.Count > 0
            ? _loadedMap.Project.systems[0].solarSystemId
            : null;
    }

    private bool IsSpawnTakenByOther(string systemId, LobbyPlayer? selected)
    {
        foreach (var p in _lobby.players)
        {
            if (selected != null && selected.playerId == p.playerId)
            {
                continue;
            }
            if (systemId.Equals(p.spawnSolarSystemId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private string? FindSystemName(string systemId)
    {
        if (_loadedMap == null)
        {
            return null;
        }
        foreach (var s in _loadedMap.Project.systems)
        {
            if (systemId.Equals(s.solarSystemId, StringComparison.Ordinal))
            {
                return s.name ?? s.solarSystemId;
            }
        }
        return null;
    }

    private string SystemDisplayName(string? systemId)
    {
        if (systemId == null)
        {
            return "?";
        }
        return FindSystemName(systemId) ?? systemId;
    }

    private int IndexOfMap(string? path)
    {
        for (var i = 0; i < _maps.Count; i++)
        {
            if (path != null && path.Equals(_maps[i].path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return 0;
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
