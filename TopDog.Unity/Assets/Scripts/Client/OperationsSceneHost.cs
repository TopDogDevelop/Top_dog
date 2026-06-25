using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Operations phase scene host.</summary>
[DefaultExecutionOrder(-50)]
public sealed class OperationsSceneHost : MonoBehaviour
{
    private static bool _sceneBootstrapped;

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset campaignShellUxml;

    private void Awake()
    {
        OperationsUiRepair.Ensure();
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        UiAssetCatalog.EnsurePanelSettings(uiDocument);
    }

    private void Start() => TryBootstrapScene();

    private void OnDestroy() => _sceneBootstrapped = false;

    public static void TryBootstrapScene()
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        var host = Object.FindAnyObjectByType<OperationsSceneHost>();
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

        campaignShellUxml ??= UiAssetCatalog.LoadUxml("Assets/UI/CampaignShell.uxml");
        if (campaignShellUxml != null)
        {
            doc.visualTreeAsset = campaignShellUxml;
            if (doc.rootVisualElement != null)
            {
                UiAssetCatalog.EnsureOperationsStyleSheets(doc.rootVisualElement);
            }

            UiTheme.ApplyDocument(doc);
            UiArtBinder.ApplyToDocument(doc, UiScreenId.CampaignShell);
            UiInputSetup.EnsureForDocument(doc);
        }
        else
        {
            Debug.LogError("TopDog: CampaignShell.uxml not found");
            return;
        }

        foreach (var c in GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }

        var shell = GetComponent<CampaignShellController>();
        if (shell != null)
        {
            shell.enabled = true;
            shell.AttachToDocument(doc);
        }

        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        _sceneBootstrapped = true;
        Debug.Log("TopDog: Operations UI bootstrapped");
    }
}
