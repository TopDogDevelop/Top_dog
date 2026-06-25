using System;
using System.IO;
using TopDog.Client;
using TopDog.Client.StarMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDog.Client.Editor;

public static class ProjectScaffold
{
    private const string ScenesDir = "Assets/Scenes";
    private const string PanelSettingsPath = "Assets/Settings/DefaultPanelSettings.asset";

    [MenuItem("TopDog/Scaffold All Scenes")]
    public static void ScaffoldAllScenesMenu() => ScaffoldAllScenes();

    [MenuItem("TopDog/Repair All Scene References")]
    public static void RepairAllScenesMenu() => RepairAllScenes();

    [MenuItem("TopDog/Scaffold Boot Scene")]
    public static void CreateBootSceneMenu() => ScaffoldAllScenes();

    public static void ScaffoldAllScenes()
    {
        UiArtSkinEditor.EnsureDefaultSkinAsset();
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += ScaffoldAllScenesMenu;
            Debug.Log("TopDog: waiting for script compile before scaffolding scenes…");
            return;
        }

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Settings"));

        var panelSettings = EnsurePanelSettings();
        var uxml = LoadUxmlAssets();

        CreateBootScene();
        CreateOutOfMatchScene(panelSettings, uxml);
        CreateOperationsScene(panelSettings, uxml);
        CreateCombatScene(panelSettings, uxml);
        CreateCombatRealtimeScene(panelSettings, uxml);

        EditorBuildSettings.scenes = new[]
        {
            SceneEntry("Boot"),
            SceneEntry("OutOfMatch"),
            SceneEntry("Operations"),
            SceneEntry("Combat"),
            SceneEntry("CombatRealtime"),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Boot.unity"), OpenSceneMode.Single);
        Debug.Log("TopDog: all scenes scaffolded. Open Boot.unity and press Play.");
    }

    public static void RepairAllScenes()
    {
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += RepairAllScenesMenu;
            return;
        }

        var panelSettings = EnsurePanelSettings();
        var uxml = LoadUxmlAssets();

        RepairBootScene();
        RepairOutOfMatchScene(panelSettings, uxml);
        RepairOperationsScene(panelSettings, uxml);
        RepairCombatScene(panelSettings, uxml);
        RepairCombatRealtimeScene(panelSettings, uxml);

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Boot.unity"), OpenSceneMode.Single);
        Debug.Log("TopDog: repaired script references on all scenes.");
    }

    private static void RepairBootScene()
    {
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Boot.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        var old = GameObject.Find("TopDogPersistent");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }

        var persistent = new GameObject("TopDogPersistent");
        persistent.AddComponent<GameAppHost>();
        persistent.AddComponent<GameSceneRouter>();
        persistent.AddComponent<GameAppBootstrap>();
        PurgeMissingScriptsInScene();
        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairOutOfMatchScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "OutOfMatch.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        var old = GameObject.Find("TopDogUI");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }
        BuildOutOfMatchUi(panelSettings, uxml);
        PurgeMissingScriptsInScene();
        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairOperationsScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Operations.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        var old = GameObject.Find("TopDogUI");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }
        BuildOperationsUi(panelSettings, uxml);
        PurgeMissingScriptsInScene();
        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairCombatScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Combat.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        var old = GameObject.Find("TopDogUI");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }
        BuildCombatUi(panelSettings, uxml);
        PurgeMissingScriptsInScene();
        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairCombatRealtimeScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "CombatRealtime.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        var old = GameObject.Find("TopDogUI");
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }
        BuildCombatRealtimeUi(panelSettings, uxml);
        PurgeMissingScriptsInScene();
        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateBootScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject("TopDogPersistent");
        go.AddComponent<GameAppHost>();
        go.AddComponent<GameSceneRouter>();
        go.AddComponent<GameAppBootstrap>();
        StyleMainCamera();
        EditorSceneManager.SaveScene(scene, Path.Combine(ScenesDir, "Boot.unity"));
    }

    private static void CreateOutOfMatchScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildOutOfMatchUi(panelSettings, uxml);
        StyleMainCamera();
        EditorSceneManager.SaveScene(scene, Path.Combine(ScenesDir, "OutOfMatch.unity"));
    }

    private static void CreateOperationsScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildOperationsUi(panelSettings, uxml);
        StyleMainCamera();
        EditorSceneManager.SaveScene(scene, Path.Combine(ScenesDir, "Operations.unity"));
    }

    private static void CreateCombatScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildCombatUi(panelSettings, uxml);
        StyleMainCamera();
        EditorSceneManager.SaveScene(scene, Path.Combine(ScenesDir, "Combat.unity"));
    }

    private static void CreateCombatRealtimeScene(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildCombatRealtimeUi(panelSettings, uxml);
        StyleMainCamera();
        EditorSceneManager.SaveScene(scene, Path.Combine(ScenesDir, "CombatRealtime.unity"));
    }

    private static void BuildOutOfMatchUi(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var uiGo = CreateUiRoot(panelSettings, uxml.MainMenu);
        var doc = uiGo.GetComponent<UIDocument>();
        var nav = uiGo.AddComponent<UiNavigator>();
        uiGo.AddComponent<MainMenuController>();
        uiGo.AddComponent<WorldlineController>();
        uiGo.AddComponent<StoryLevelsController>();
        uiGo.AddComponent<SettingsController>();
        uiGo.AddComponent<JoinLanController>();
        uiGo.AddComponent<CustomLobbyController>();
        var host = uiGo.AddComponent<OutOfMatchSceneHost>();
        WireUiNavigator(nav, doc, uxml);
        WireOutOfMatchHost(host, doc, uxml);
    }

    private static void BuildOperationsUi(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var uiGo = CreateUiRoot(panelSettings, uxml.CampaignShell);
        uiGo.AddComponent<CampaignShellController>();
        uiGo.AddComponent<StarMapHostController>();
        var host = uiGo.AddComponent<OperationsSceneHost>();
        WireOperationsHost(host, uiGo.GetComponent<UIDocument>(), uxml);
    }

    private static void BuildCombatUi(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var uiGo = CreateUiRoot(panelSettings, uxml.CombatShell);
        uiGo.AddComponent<CombatShellController>();
        var host = uiGo.AddComponent<CombatSceneHost>();
        WireCombatHost(host, uiGo.GetComponent<UIDocument>(), uxml);
    }

    private static void BuildCombatRealtimeUi(PanelSettings panelSettings, UxmlAssets uxml)
    {
        var uiGo = CreateUiRoot(panelSettings, uxml.CombatRealtime);
        uiGo.AddComponent<CombatRealtimeController>();
        uiGo.AddComponent<StubViewportCameraCommands>();
        var host = uiGo.AddComponent<CombatRealtimeSceneHost>();
        WireCombatRealtimeHost(host, uiGo.GetComponent<UIDocument>(), uxml);
    }

    private static PanelSettings EnsurePanelSettings()
    {
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
        }
        var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiAssetCatalog.RuntimeThemePath);
        if (theme != null && panelSettings.themeStyleSheet == null)
        {
            panelSettings.themeStyleSheet = theme;
            EditorUtility.SetDirty(panelSettings);
        }
        UiViewportConfig.ApplyToPanel(panelSettings);
        return panelSettings;
    }

    private static UxmlAssets LoadUxmlAssets()
    {
        var assets = new UxmlAssets
        {
            MainMenu = LoadUxml("Assets/UI/MainMenu.uxml"),
            Worldline = LoadUxml("Assets/UI/Worldline.uxml"),
            Settings = LoadUxml("Assets/UI/Settings.uxml"),
            JoinLan = LoadUxml("Assets/UI/JoinLan.uxml"),
            CustomLobby = LoadUxml("Assets/UI/CustomLobby.uxml"),
            StoryLevels = LoadUxml("Assets/UI/StoryLevels.uxml"),
            CampaignShell = LoadUxml("Assets/UI/CampaignShell.uxml"),
            CombatShell = LoadUxml("Assets/UI/CombatShell.uxml"),
            CombatRealtime = LoadUxml("Assets/UI/CombatRealtime.uxml"),
        };

        if (assets.MainMenu == null)
        {
            Debug.LogError("TopDog scaffold: UXML assets missing under Assets/UI/. Import project assets first.");
        }

        return assets;
    }

    private static GameObject CreateUiRoot(PanelSettings panelSettings, VisualTreeAsset initialUxml)
    {
        var uiGo = new GameObject("TopDogUI");
        var doc = uiGo.AddComponent<UIDocument>();
        var docSo = new SerializedObject(doc);
        docSo.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        docSo.FindProperty("sourceAsset").objectReferenceValue = initialUxml;
        docSo.ApplyModifiedPropertiesWithoutUndo();
        uiGo.AddComponent<UiViewportDriver>();
        return uiGo;
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        if (go.GetComponent<T>() == null)
        {
            go.AddComponent<T>();
        }
    }

    private static void PurgeMissingScriptsInScene()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            PurgeMissingRecursive(root);
        }
    }

    private static void PurgeMissingRecursive(GameObject go)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            PurgeMissingRecursive(child.gameObject);
        }
    }

    private static void WireUiNavigator(UiNavigator nav, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(nav);
        so.FindProperty("uiDocument").objectReferenceValue = doc;
        so.FindProperty("mainMenuUxml").objectReferenceValue = uxml.MainMenu;
        so.FindProperty("worldlineUxml").objectReferenceValue = uxml.Worldline;
        so.FindProperty("settingsUxml").objectReferenceValue = uxml.Settings;
        so.FindProperty("joinLanUxml").objectReferenceValue = uxml.JoinLan;
        so.FindProperty("customLobbyUxml").objectReferenceValue = uxml.CustomLobby;
        so.FindProperty("storyLevelsUxml").objectReferenceValue = uxml.StoryLevels;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireOutOfMatchHost(OutOfMatchSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        so.FindProperty("uiDocument").objectReferenceValue = doc;
        so.FindProperty("mainMenuUxml").objectReferenceValue = uxml.MainMenu;
        so.FindProperty("worldlineUxml").objectReferenceValue = uxml.Worldline;
        so.FindProperty("settingsUxml").objectReferenceValue = uxml.Settings;
        so.FindProperty("joinLanUxml").objectReferenceValue = uxml.JoinLan;
        so.FindProperty("customLobbyUxml").objectReferenceValue = uxml.CustomLobby;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireOperationsHost(OperationsSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        so.FindProperty("uiDocument").objectReferenceValue = doc;
        so.FindProperty("campaignShellUxml").objectReferenceValue = uxml.CampaignShell;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireCombatHost(CombatSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        so.FindProperty("uiDocument").objectReferenceValue = doc;
        so.FindProperty("combatShellUxml").objectReferenceValue = uxml.CombatShell;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireCombatRealtimeHost(CombatRealtimeSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        so.FindProperty("uiDocument").objectReferenceValue = doc;
        so.FindProperty("combatRealtimeUxml").objectReferenceValue = uxml.CombatRealtime;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleMainCamera()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    private static EditorBuildSettingsScene SceneEntry(string sceneName) =>
        new(Path.Combine(ScenesDir, sceneName + ".unity"), true);

    private static VisualTreeAsset LoadUxml(string path) =>
        AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);

    private sealed class UxmlAssets
    {
        public VisualTreeAsset MainMenu;
        public VisualTreeAsset Worldline;
        public VisualTreeAsset Settings;
        public VisualTreeAsset JoinLan;
        public VisualTreeAsset CustomLobby;
        public VisualTreeAsset StoryLevels;
        public VisualTreeAsset CampaignShell;
        public VisualTreeAsset CombatShell;
        public VisualTreeAsset CombatRealtime;
    }
}