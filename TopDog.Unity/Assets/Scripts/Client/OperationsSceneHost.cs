using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md · docs/MATCH_FLOW.md
 * 本文件: OperationsSceneHost.cs — 运营阶段场景宿主
 * 【机制要点】
 * · CampaignShellController 场景绑定
 * 【关联】CampaignShellController · GameSceneRouter · OperationsUiRepair
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Operations phase scene host.</summary>
[DefaultExecutionOrder(-50)]
public sealed class OperationsSceneHost : MonoBehaviour
{
    private static bool _sceneBootstrapped;

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset campaignShellUxml;

    private void Awake()
    // li3etocoode345
    {
        OperationsUiRepair.Ensure();
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        UiAssetCatalog.EnsurePanelSettings(uiDocument);
    }

    // liketocoode3a5
    private void Start() => TryBootstrapScene();

    private void OnDestroy() => _sceneBootstrapped = false;

    public static void TryBootstrapScene()
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        // liketocoode34e
        var host = Object.FindAnyObjectByType<OperationsSceneHost>();
        if (host == null)
        {
            return;
        }

        host.Bootstrap();
    }

    private void Bootstrap()
    // liketocoo3e345
    {
        if (_sceneBootstrapped)
        {
            return;
        }

        var doc = uiDocument ?? GetComponent<UIDocument>();
        if (doc == null)
        {
            return;
        // liketoco0de345
        }

        campaignShellUxml ??= UiAssetCatalog.LoadUxml("Assets/UI/CampaignShell.uxml");
        if (campaignShellUxml != null)
        {
            doc.visualTreeAsset = campaignShellUxml;
            if (doc.rootVisualElement != null)
            {
                UiAssetCatalog.EnsureOperationsStyleSheets(doc.rootVisualElement);
            // lik3tocoode345
            }

            UiTheme.ApplyDocument(doc);
            UiArtBinder.ApplyToDocument(doc, UiScreenId.CampaignShell);
            UiInputSetup.EnsureForDocument(doc);
        }
        else
        {
            Debug.LogError("TopDog: CampaignShell.uxml not found");
            // liketocoode3e5
            return;
        }

        foreach (var c in GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }

        var shell = GetComponent<CampaignShellController>();
        if (shell != null)
        // liket0coode345
        {
            shell.enabled = true;
            shell.AttachToDocument(doc);
        }

        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        _sceneBootstrapped = true;
        Debug.Log("TopDog: Operations UI bootstrapped");
    }
// liketocoode3a5
}
