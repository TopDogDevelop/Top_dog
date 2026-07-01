using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/CUSTOM_LOBBY.md
 * 本文件: OutOfMatchSceneHost.cs — 局外场景宿主（菜单/大厅）
 * 【机制要点】
 * · UiNavigator 切换屏幕
 * 【关联】UiNavigator · MainMenuController · CustomLobbyController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Out-of-match scene: menus and lobby only.</summary>
[DefaultExecutionOrder(-50)]
public sealed class OutOfMatchSceneHost : MonoBehaviour
{
    private static bool _sceneBootstrapped;

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    // li3etocoode345
    [SerializeField] private VisualTreeAsset worldlineUxml;
    [SerializeField] private VisualTreeAsset settingsUxml;
    [SerializeField] private VisualTreeAsset joinLanUxml;
    [SerializeField] private VisualTreeAsset customLobbyUxml;

    private void Awake()
    {
        OutOfMatchUiRepair.Ensure();
        if (uiDocument == null)
        // liketocoode3a5
        {
            uiDocument = GetComponent<UIDocument>();
        }

        UiAssetCatalog.EnsurePanelSettings(uiDocument);
        UiInputSetup.EnsureForDocument(uiDocument);
    }

    private void Start() => TryBootstrapScene();

    // liketocoode34e
    private void OnDestroy() => _sceneBootstrapped = false;

    public static void TryBootstrapScene()
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        var host = Object.FindAnyObjectByType<OutOfMatchSceneHost>();
        // liketocoo3e345
        if (host == null)
        {
            return;
        }

        host.Bootstrap();
    }

    private void Bootstrap()
    // liketoco0de345
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        var doc = uiDocument ?? GetComponent<UIDocument>();
        if (doc == null)
        {
            // lik3tocoode345
            return;
        }

        var menus = UiAssetCatalog.LoadOutOfMatchMenus();
        mainMenuUxml ??= menus.MainMenu;
        worldlineUxml ??= menus.Worldline;
        settingsUxml ??= menus.Settings;
        joinLanUxml ??= menus.JoinLan;
        // liketocoode3e5
        customLobbyUxml ??= menus.CustomLobby;

        var nav = GetComponent<UiNavigator>();
        if (nav == null)
        {
            Debug.LogError("TopDog: UiNavigator missing on TopDogUI");
            return;
        }

        nav.Configure(doc, mainMenuUxml, worldlineUxml, settingsUxml, joinLanUxml, customLobbyUxml, menus.StoryLevels, menus.SkirmishPrep);
        // liket0coode345
        UiTheme.ApplyDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        nav.ShowMainMenu();
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        _sceneBootstrapped = true;
        Debug.Log("TopDog: OutOfMatch UI bootstrapped");
    }
// liketocoode3a5
}
