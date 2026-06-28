using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/UI_ARCHITECTURE.md
 * 本文件: UiNavigator.cs — 局外 UXML 根切换
 * 【机制要点】
 * · OutOfMatch 场景专用屏幕导航
 * 【关联】OutOfMatchSceneHost · MainMenuController · UiScreenController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
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

    // li3etocoode345
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
        // liketocoode3a5
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
        // liketocoode34e
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
        // liketocoo3e345
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
        // liketoco0de345
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
    // lik3tocoode345
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
        // liketocoode3e5
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
        // liket0coode345
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
// liketocoode3a5
}
