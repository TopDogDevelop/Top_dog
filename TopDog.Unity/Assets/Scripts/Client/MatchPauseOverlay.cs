using System;

using TopDog.App;

using TopDog.Lobby;

using TopDog.Net.Protocol;

using UnityEngine;

using UnityEngine.UIElements;



namespace TopDog.Client;



/// <summary>Full-screen pause overlay during match scenes (MAIN_MENU.md §暂停).</summary>

public static class MatchPauseOverlay

{

    private static VisualElement? _layer;

    private static Label? _initiatorLabel;

    private static bool _suppressNetworkResume;



    public static bool IsVisible =>

        _layer != null && _layer.ClassListContains("match-pause-layer-visible");



    /// <summary>Esc handler: optional pre-step (e.g. close ops overlay). Returns true if consumed.</summary>

    public static bool TryHandleEscape(VisualElement root, Func<bool>? beforePause = null)

    {

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



        var panel = new VisualElement();

        panel.AddToClassList("match-pause-panel");



        var title = new Label { text = "暂停" };

        title.AddToClassList("match-pause-title");

        panel.Add(title);



        _initiatorLabel = new Label { text = FormatInitiatorLine(initiatorName) };

        _initiatorLabel.AddToClassList("match-pause-initiator");

        panel.Add(_initiatorLabel);



        var resumeBtn = new Button { text = "继续游戏" };

        resumeBtn.AddToClassList("menu-button-wide");

        resumeBtn.clicked += () => GameAppHost.Instance?.RequestResume();

        panel.Add(resumeBtn);



        var menuBtn = new Button { text = "返回主菜单" };

        menuBtn.AddToClassList("menu-button-wide");

        menuBtn.clicked += ReturnToMainMenu;

        panel.Add(menuBtn);



        var hint = new Label { text = "Esc · 继续游戏" };

        hint.AddToClassList("match-pause-hint");

        panel.Add(hint);



        _layer.Add(panel);

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

        if (_layer != null)

        {

            _layer.RemoveFromHierarchy();

            _layer = null;

        }

        _initiatorLabel = null;

        GameAppHost.Instance?.SetMatchPaused(false);

    }



    internal static void ApplyNetworkResume()

    {

        _suppressNetworkResume = true;

        HideLocalOnly();

        _suppressNetworkResume = false;

    }



    private static string FormatInitiatorLine(string? initiatorName)

    {

        if (string.IsNullOrWhiteSpace(initiatorName))

        {

            return "";

        }

        return initiatorName + " 发起暂停";

    }



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


