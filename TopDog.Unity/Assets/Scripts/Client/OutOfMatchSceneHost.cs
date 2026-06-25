using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Out-of-match scene: menus and lobby only.</summary>
[DefaultExecutionOrder(-50)]
public sealed class OutOfMatchSceneHost : MonoBehaviour
{
    private static bool _sceneBootstrapped;

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset mainMenuUxml;
    [SerializeField] private VisualTreeAsset worldlineUxml;
    [SerializeField] private VisualTreeAsset settingsUxml;
    [SerializeField] private VisualTreeAsset joinLanUxml;
    [SerializeField] private VisualTreeAsset customLobbyUxml;

    private void Awake()
    {
        OutOfMatchUiRepair.Ensure();
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        UiAssetCatalog.EnsurePanelSettings(uiDocument);
        UiInputSetup.EnsureForDocument(uiDocument);
    }

    private void Start() => TryBootstrapScene();

    private void OnDestroy() => _sceneBootstrapped = false;

    public static void TryBootstrapScene()
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        var host = Object.FindAnyObjectByType<OutOfMatchSceneHost>();
        if (host == null)
        {
            return;
        }

        host.Bootstrap();
    }

    private void Bootstrap()
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        var doc = uiDocument ?? GetComponent<UIDocument>();
        if (doc == null)
        {
            return;
        }

        var menus = UiAssetCatalog.LoadOutOfMatchMenus();
        mainMenuUxml ??= menus.MainMenu;
        worldlineUxml ??= menus.Worldline;
        settingsUxml ??= menus.Settings;
        joinLanUxml ??= menus.JoinLan;
        customLobbyUxml ??= menus.CustomLobby;

        var nav = GetComponent<UiNavigator>();
        if (nav == null)
        {
            Debug.LogError("TopDog: UiNavigator missing on TopDogUI");
            return;
        }

        nav.Configure(doc, mainMenuUxml, worldlineUxml, settingsUxml, joinLanUxml, customLobbyUxml, menus.StoryLevels);
        UiTheme.ApplyDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        nav.ShowMainMenu();
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        _sceneBootstrapped = true;
        Debug.Log("TopDog: OutOfMatch UI bootstrapped");
    }
}
