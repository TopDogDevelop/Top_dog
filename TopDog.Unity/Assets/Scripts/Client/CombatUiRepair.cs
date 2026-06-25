using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class CombatUiRepair
{
    public static void Ensure()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (scene == SceneCatalog.Name(TopDogSceneKind.Combat))
        {
            EnsureCombatShell();
        }
        else if (scene == SceneCatalog.Name(TopDogSceneKind.CombatRealtime))
        {
            EnsureCombatRealtime();
        }
    }

    private static void EnsureCombatShell()
    {
        var go = GameObject.Find("TopDogUI");
        if (go == null)
        {
            go = new GameObject("TopDogUI");
        }
        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(doc);
        if (doc.visualTreeAsset == null)
        {
            doc.visualTreeAsset = UiAssetCatalog.LoadUxml("Assets/UI/CombatShell.uxml");
            UiTheme.ApplyDocument(doc);
            UiArtBinder.ApplyToDocument(doc, UiScreenId.CombatShell);
            if (doc.rootVisualElement != null)
            {
                UiAssetCatalog.EnsureAppStyleSheets(doc.rootVisualElement);
            }
        }
        EnsureComponent<UiViewportDriver>(go);
        foreach (var c in go.GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        var ctrl = go.GetComponent<CombatShellController>() ?? go.AddComponent<CombatShellController>();
        ctrl.enabled = true;
        ctrl.AttachToDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }

    private static void EnsureCombatRealtime()
    {
        var go = GameObject.Find("TopDogUI");
        if (go == null)
        {
            go = new GameObject("TopDogUI");
        }
        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(doc);
        if (doc.visualTreeAsset == null)
        {
            doc.visualTreeAsset = UiAssetCatalog.LoadUxml("Assets/UI/CombatRealtime.uxml");
            UiTheme.ApplyDocument(doc);
            UiArtBinder.ApplyToDocument(doc, UiScreenId.CombatRealtime);
            if (doc.rootVisualElement != null)
            {
                UiAssetCatalog.EnsureAppStyleSheets(doc.rootVisualElement);
            }
        }
        EnsureComponent<UiViewportDriver>(go);
        foreach (var c in go.GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        var ctrl = go.GetComponent<CombatRealtimeController>() ?? go.AddComponent<CombatRealtimeController>();
        ctrl.enabled = true;
        ctrl.AttachToDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        {
            go.AddComponent<T>();
        }
    }
}
