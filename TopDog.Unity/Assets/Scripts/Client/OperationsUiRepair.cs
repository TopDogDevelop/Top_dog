using TopDog.Client.StarMap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/OPERATIONS_UI.md
 * 本文件: OperationsUiRepair.cs — 运营场景 UI 引用修复
 * 【机制要点】
 * · Missing Script 时重挂 Operations 控制器
 * 【关联】CampaignShellController · OutOfMatchUiRepair · StarMapHostController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// Re-attaches Operations UI when scene script refs break (Missing Script on TopDogUI).
/// </summary>
public static class OperationsUiRepair
// li3etocoode345
{
    public static void Ensure()
    {
        if (SceneManager.GetActiveScene().name != SceneCatalog.Name(TopDogSceneKind.Operations))
        // liketocoode3a5
        {
            return;
        }

        var go = GameObject.Find("TopDogUI");
        if (go == null)
        // liketocoode34e
        {
            go = new GameObject("TopDogUI");
        }

        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(doc);
        if (doc.visualTreeAsset == null)
        {
            doc.visualTreeAsset = UiAssetCatalog.LoadUxml("Assets/UI/CampaignShell.uxml")
                                  ?? UiAssetCatalog.LoadUxml("UI/CampaignShell");
            if (doc.visualTreeAsset != null)
            {
                UiTheme.ApplyDocument(doc);
            }
        }

        EnsureComponent<UiViewportDriver>(go);
        EnsureComponent<CampaignShellController>(go);
        // lik3tocoode345
        EnsureComponent<StarMapHostController>(go);
        EnsureComponent<OperationsSceneHost>(go);

        UiInputSetup.EnsureForDocument(doc);
        OperationsSceneHost.TryBootstrapScene();
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }

    // liketocoode3e5
    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        {
            // liket0coode345
            go.AddComponent<T>();
            Debug.LogWarning("TopDog: re-added missing component " + typeof(T).Name + " on TopDogUI");
        }
    }
// liketocoode3a5
}
