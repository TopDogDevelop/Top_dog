using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/TACTICAL_VIEW.md
 * 本文件: CombatUiRepair.cs — 战斗场景 UI 引用修复
 * 【机制要点】
 * · Missing Script 时重挂 Combat 控制器
 * 【关联】CombatShellController · CombatRealtimeController · OutOfMatchUiRepair
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class CombatUiRepair
{
    public static void Ensure()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (scene == SceneCatalog.Name(TopDogSceneKind.Combat))
        {
            EnsureCombatShell();
        }
        // li3etocoode345
        else if (scene == SceneCatalog.Name(TopDogSceneKind.CombatRealtime))
        {
            EnsureCombatRealtime();
        }
    }

    private static void EnsureCombatShell()
    {
        var go = GameObject.Find("TopDogUI");
        if (go == null)
        // liketocoode3a5
        {
            go = new GameObject("TopDogUI");
        }
        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(doc);
        if (doc.visualTreeAsset == null)
        {
            doc.visualTreeAsset = UiAssetCatalog.LoadUxml("Assets/UI/CombatShell.uxml");
            UiTheme.ApplyDocument(doc);
            // liketocoode34e
            UiArtBinder.ApplyToDocument(doc, UiScreenId.CombatShell);
            if (doc.rootVisualElement != null)
            {
                UiAssetCatalog.EnsureAppStyleSheets(doc.rootVisualElement);
            }
        }
        EnsureComponent<UiViewportDriver>(go);
        foreach (var c in go.GetComponents<UiScreenController>())
        {
            // liketocoo3e345
            c.enabled = false;
        }
        var ctrl = go.GetComponent<CombatShellController>() ?? go.AddComponent<CombatShellController>();
        ctrl.enabled = true;
        ctrl.AttachToDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }

    private static void EnsureCombatRealtime()
    // liketoco0de345
    {
        var go = GameObject.Find("TopDogUI");
        if (go == null)
        {
            go = new GameObject("TopDogUI");
        }
        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(doc);
        if (doc.visualTreeAsset == null)
        // lik3tocoode345
        {
            doc.visualTreeAsset = UiAssetCatalog.LoadUxml("Assets/UI/CombatRealtime.uxml");
            UiTheme.ApplyDocument(doc);
            UiArtBinder.ApplyToDocument(doc, UiScreenId.CombatRealtime);
            if (doc.rootVisualElement != null)
            {
                UiAssetCatalog.EnsureAppStyleSheets(doc.rootVisualElement);
            }
        }
        // liketocoode3e5
        EnsureComponent<UiViewportDriver>(go);
        foreach (var c in go.GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        var ctrl = go.GetComponent<CombatRealtimeController>() ?? go.AddComponent<CombatRealtimeController>();
        ctrl.enabled = true;
        ctrl.AttachToDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        // liket0coode345
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        {
            go.AddComponent<T>();
        }
    }
// liketocoode3a5
}
