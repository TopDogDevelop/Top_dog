using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Swaps root UXML on the shared UIDocument (OutOfMatch scene only).</summary>
public sealed class UiNavigator : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset worldlineUxml;
    [SerializeField] private VisualTreeAsset settingsUxml;
    [SerializeField] private VisualTreeAsset joinLanUxml;
    [SerializeField] private VisualTreeAsset customLobbyUxml;
    [SerializeField] private VisualTreeAsset storyLevelsUxml;

    private CustomLobbyLaunchArgs _pendingLobbyArgs;

    public UIDocument Document
    {
        get
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
            return uiDocument;
        }
    }

    public void Configure(
        UIDocument document,
        VisualTreeAsset mainMenu,
        VisualTreeAsset worldline,
        VisualTreeAsset settings,
        VisualTreeAsset joinLan,
        VisualTreeAsset customLobby,
        VisualTreeAsset? storyLevels = null)
    {
        uiDocument = document;
        mainMenuUxml = mainMenu;
        worldlineUxml = worldline;
        settingsUxml = settings;
        joinLanUxml = joinLan;
        customLobbyUxml = customLobby;
        storyLevelsUxml = storyLevels ?? UiAssetCatalog.LoadUxml("Assets/UI/StoryLevels.uxml");
    }

    public CustomLobbyLaunchArgs ConsumeLobbyLaunchArgs()
    {
        var args = _pendingLobbyArgs ?? CustomLobbyLaunchArgs.Host();
        _pendingLobbyArgs = null;
        return args;
    }

    public void ShowMainMenu() => Switch(mainMenuUxml, typeof(MainMenuController));
    public void ShowWorldline() => Switch(worldlineUxml, typeof(WorldlineController));
    public void ShowSettings() => Switch(settingsUxml, typeof(SettingsController));
    public void ShowJoinLan() => Switch(joinLanUxml, typeof(JoinLanController));

    public void ShowCustomLobby()
    {
        _pendingLobbyArgs = CustomLobbyLaunchArgs.Host();
        Switch(customLobbyUxml, typeof(CustomLobbyController));
    }

    public void ShowCustomLobbyJoin(string hostIp, string mapHint)
    {
        _pendingLobbyArgs = CustomLobbyLaunchArgs.Guest(hostIp, mapHint);
        Switch(customLobbyUxml, typeof(CustomLobbyController));
    }

    public void ShowStoryLevels() => Switch(storyLevelsUxml, typeof(StoryLevelsController));

    private void Switch(VisualTreeAsset asset, System.Type controllerType)
    {
        if (asset == null)
        {
            Debug.LogWarning("TopDog UI: missing UXML for " + controllerType.Name);
            return;
        }

        var target = GetComponent(controllerType) as UiScreenController;
        if (target == null)
        {
            target = (UiScreenController)gameObject.AddComponent(controllerType);
            Debug.LogWarning("TopDog UI: auto-added missing controller " + controllerType.Name);
        }

        foreach (var c in GetComponents<UiScreenController>())
        {
            if (c.GetType() != controllerType)
            {
                c.Detach();
            }
        }

        var doc = Document;
        if (doc.visualTreeAsset != asset)
        {
            doc.visualTreeAsset = asset;
        }

        UiTheme.ApplyDocument(doc);
        UiArtBinder.ApplyToDocument(doc, target.ArtScreenId);
        UiInputSetup.EnsureForDocument(doc);

        target.AttachToDocument(doc);
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        Debug.Log("TopDog UI -> " + controllerType.Name);
    }
}