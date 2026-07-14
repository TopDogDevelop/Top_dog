using System;
using TopDog.App;
using TopDog.Lobby;
using TopDog.Net.Protocol;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md §暂停 · docs/CLIENT_GAME_SETTINGS.md · docs/COMBAT_VIEW_BREATHING.md
 * 本文件: MatchPauseOverlay.cs — 战役内全屏暂停
 * 【机制要点】
 * · ESC 暂停/继续；覆盖 Operations/Combat 场景
 * · 主菜单：继续游戏 · 返回主菜单 · 设置（子面板，控件同 Settings.uxml）
 * · HideLocalOnly 时 ApplyPending 再关层
 * 【关联】CampaignShellController · CombatShellController · GameAppHost · GameSceneRouter
 * ══
 */

namespace TopDog.Client;

/// <summary>Full-screen pause overlay during match scenes (MAIN_MENU.md §暂停).</summary>
public static class MatchPauseOverlay
{
    private static VisualElement? _layer;
    private static VisualElement? _mainPanel;
    private static VisualElement? _settingsPanel;
    private static Label? _initiatorLabel;
    private static CombatViewSettingsBinder? _combatSettingsBinder;
    private static AudioSettingsBinder? _audioSettingsBinder;
    private static bool _suppressNetworkResume;

    public static bool IsVisible =>
        _layer != null && _layer.ClassListContains("match-pause-layer-visible");

    /// <summary>Esc handler: optional pre-step (e.g. close ops overlay). Returns true if consumed.</summary>
    public static bool TryHandleEscape(VisualElement root, Func<bool>? beforePause = null)
    {
        if (IsVisible && _settingsPanel != null && _settingsPanel.style.display == DisplayStyle.Flex)
        {
            ShowMainPanel(applySettings: true);
            return true;
        }

        if (beforePause != null && beforePause())
        {
            return true;
        }

        GameAppHost.Instance?.RequestTogglePause(root);
        return true;
    }

    public static void ShowFromNetwork(MatchPausePayload payload)
    {
        if (!payload.paused)
        {
            HideLocalOnly();
            return;
        }

        var root = FindMatchUiRoot();
        if (root == null)
        {
            return;
        }

        Show(root, payload.initiatorName, fromNetwork: true);
    }

    public static void Show(VisualElement root, string? initiatorName = null, bool fromNetwork = false)
    {
        var panelRoot = root.panel?.visualTree;
        if (panelRoot == null)
        {
            return;
        }

        UiAssetCatalog.EnsureAppStyleSheets(panelRoot);
        HideLocalOnly();

        _layer = new VisualElement { name = "match-pause-layer" };
        _layer.AddToClassList("match-pause-layer");
        _layer.pickingMode = PickingMode.Position;

        _mainPanel = new VisualElement();
        _mainPanel.AddToClassList("match-pause-panel");

        var title = new Label { text = "暂停" };
        title.AddToClassList("match-pause-title");
        _mainPanel.Add(title);

        _initiatorLabel = new Label { text = FormatInitiatorLine(initiatorName) };
        _initiatorLabel.AddToClassList("match-pause-initiator");
        _mainPanel.Add(_initiatorLabel);

        var resumeBtn = new Button { text = "继续游戏" };
        resumeBtn.AddToClassList("menu-button-wide");
        resumeBtn.clicked += () => GameAppHost.Instance?.RequestResume();
        _mainPanel.Add(resumeBtn);

        var menuBtn = new Button { text = "返回主菜单" };
        menuBtn.AddToClassList("menu-button-wide");
        menuBtn.clicked += ReturnToMainMenu;
        _mainPanel.Add(menuBtn);

        var settingsBtn = new Button { text = "设置" };
        settingsBtn.AddToClassList("menu-button-wide");
        settingsBtn.clicked += ShowSettingsPanel;
        _mainPanel.Add(settingsBtn);

        var hint = new Label { text = "Esc · 继续游戏" };
        hint.AddToClassList("match-pause-hint");
        _mainPanel.Add(hint);

        _settingsPanel = new VisualElement();
        _settingsPanel.AddToClassList("match-pause-panel");
        _settingsPanel.style.display = DisplayStyle.None;

        var settingsTitle = new Label { text = "设置" };
        settingsTitle.AddToClassList("match-pause-title");
        _settingsPanel.Add(settingsTitle);

        _combatSettingsBinder = new CombatViewSettingsBinder();
        _audioSettingsBinder = new AudioSettingsBinder();
        _settingsPanel.Add(BuildInMatchSettingsOptions(_combatSettingsBinder, _audioSettingsBinder));

        var backBtn = new Button { text = "返回" };
        backBtn.AddToClassList("menu-button-wide");
        backBtn.clicked += () => ShowMainPanel(applySettings: true);
        _settingsPanel.Add(backBtn);

        _layer.Add(_mainPanel);
        _layer.Add(_settingsPanel);
        panelRoot.Add(_layer);
        _layer.BringToFront();
        _layer.AddToClassList("match-pause-layer-visible");
        GameAppHost.Instance?.SetMatchPaused(true);
    }

    public static void Hide()
    {
        if (_suppressNetworkResume)
        {
            HideLocalOnly();
            return;
        }

        if (IsVisible && GameAppHost.Instance?.IsLanMatch == true)
        {
            GameAppHost.Instance.RequestResume();
            return;
        }

        HideLocalOnly();
    }

    public static void HideLocalOnly()
    {
        if (IsVisible)
        {
            _combatSettingsBinder?.ApplyPending();
        }

        if (_layer != null)
        {
            _layer.RemoveFromHierarchy();
            _layer = null;
        }

        _mainPanel = null;
        _settingsPanel = null;
        _initiatorLabel = null;
        _combatSettingsBinder = null;
        _audioSettingsBinder = null;
        GameAppHost.Instance?.SetMatchPaused(false);
    }

    internal static void ApplyNetworkResume()
    {
        _suppressNetworkResume = true;
        HideLocalOnly();
        _suppressNetworkResume = false;
    }

    private static void ShowSettingsPanel()
    {
        if (_mainPanel == null || _settingsPanel == null)
        {
            return;
        }

        _mainPanel.style.display = DisplayStyle.None;
        _settingsPanel.style.display = DisplayStyle.Flex;
        _combatSettingsBinder?.LoadFromSaved();
        _audioSettingsBinder?.LoadFromSaved();
    }

    private static void ShowMainPanel(bool applySettings)
    {
        if (_mainPanel == null || _settingsPanel == null)
        {
            return;
        }

        if (applySettings)
        {
            _combatSettingsBinder?.ApplyPending();
        }

        _settingsPanel.style.display = DisplayStyle.None;
        _mainPanel.style.display = DisplayStyle.Flex;
    }

    private static VisualElement BuildInMatchSettingsOptions(
        CombatViewSettingsBinder combatBinder,
        AudioSettingsBinder audioBinder)
    {
        var block = new VisualElement();
        block.AddToClassList("settings-options");
        combatBinder.AppendRowsTo(block);
        block.Add(audioBinder.BuildBgmToggleRow());
        block.Add(audioBinder.BuildUiClickToggleRow());
        block.Add(audioBinder.BuildVolumeRow());
        combatBinder.Bind(block);
        audioBinder.Bind(block);
        return block;
    }

    private static string FormatInitiatorLine(string? initiatorName) =>
        string.IsNullOrWhiteSpace(initiatorName) ? "" : initiatorName + " 发起暂停";

    private static VisualElement? FindMatchUiRoot()
    {
        var doc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
        if (doc?.rootVisualElement == null)
        {
            return null;
        }

        return doc.rootVisualElement.Q("root")
               ?? doc.rootVisualElement.Q(className: "screen-root")
               ?? doc.rootVisualElement;
    }

    private static void ReturnToMainMenu()
    {
        HideLocalOnly();
        GameAppHost.Instance?.EndCampaign();
        GameSceneRouter.Instance?.GoOutOfMatch();
    }
}
