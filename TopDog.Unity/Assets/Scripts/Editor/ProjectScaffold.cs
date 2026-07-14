using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TopDog.Client;
using TopDog.Client.StarMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SCENE_ARCHITECTURE.md · docs/UI_ARCHITECTURE.md
 * 本文件: ProjectScaffold.cs — 场景脚手架 / RepairAllScenes
 * 【机制要点】
 * · Repair：Boot 挂持久化；其它场景仅主相机（Player 防 levelN corrupted）
 * · UI 运行时由 *UiRepair / OutOfMatchRuntimeBootstrap 创建
 * · SyncRuntimeUiResources → Assets/Resources（PanelSettings / UXML）
 * 【关联】BatchBuild · GameSceneRouter · OutOfMatchRuntimeBootstrap
 * ══
 */

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
        SyncRuntimeUiResources(panelSettings);
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

    [MenuItem("TopDog/Sync Runtime UI Resources")]
    public static void SyncRuntimeUiResourcesMenu()
    {
        var panelSettings = EnsurePanelSettings();
        SyncRuntimeUiResources(panelSettings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("TopDog: synced PanelSettings/UXML/USS into Assets/Resources for player builds.");
    }

    /// <summary>
    /// Player builds cannot use AssetDatabase paths. UiAssetCatalog loads
    /// Resources/DefaultPanelSettings and Resources/UI/*.
    /// </summary>
    public static void SyncRuntimeUiResources(PanelSettings panelSettings)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources", "UI"));

        const string resourcesPanel = "Assets/Resources/DefaultPanelSettings.asset";
        if (AssetDatabase.LoadAssetAtPath<PanelSettings>(resourcesPanel) == null)
        {
            if (!AssetDatabase.CopyAsset(PanelSettingsPath, resourcesPanel))
            {
                Debug.LogError("TopDog: failed to copy DefaultPanelSettings into Resources/");
            }
        }
        else
        {
            // Keep theme / resolution in sync with Settings copy.
            var dest = AssetDatabase.LoadAssetAtPath<PanelSettings>(resourcesPanel);
            if (dest != null && panelSettings != null)
            {
                dest.themeStyleSheet = panelSettings.themeStyleSheet;
                UiViewportConfig.ApplyToPanel(dest);
                EditorUtility.SetDirty(dest);
            }
        }

        const string themeSrc = "Assets/Settings/UnityDefaultRuntimeTheme.tss";
        const string themeDst = "Assets/Resources/UnityDefaultRuntimeTheme.tss";
        if (AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeSrc) != null
            && AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeDst) == null)
        {
            AssetDatabase.CopyAsset(themeSrc, themeDst);
        }

        string[] uiFiles =
        {
            "MainMenu.uxml", "MainMenu.uss",
            "Worldline.uxml",
            "Settings.uxml",
            "JoinLan.uxml", "JoinLan.uss",
            "CustomLobby.uxml", "CustomLobby.uss",
            "StoryLevels.uxml", "StoryLevels.uss",
            "SkirmishPrep.uxml",
            "CampaignShell.uxml", "CampaignShell.uss",
            "CombatShell.uxml", "CombatShell.uss",
            "CombatRealtime.uxml", "CombatRealtime.uss",
            "CombatShipDetailHud.template.uxml",
            "AppStyles.uss",
        };
        foreach (var file in uiFiles)
        {
            var src = "Assets/UI/" + file;
            var dst = "Assets/Resources/UI/" + file;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(src) == null)
            {
                continue;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst) == null)
            {
                AssetDatabase.CopyAsset(src, dst);
            }
            else
            {
                File.Copy(
                    Path.Combine(Application.dataPath, "UI", file),
                    Path.Combine(Application.dataPath, "Resources", "UI", file),
                    overwrite: true);
                AssetDatabase.ImportAsset(dst);
            }
        }
    }

    public static void RepairAllScenes()
    {
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += RepairAllScenesMenu;
            return;
        }

        var panelSettings = EnsurePanelSettings();
        SyncRuntimeUiResources(panelSettings);
        AssetDatabase.Refresh();
        // Refresh invalidates previous asset references in batchmode — reload before CreateUiRoot.
        panelSettings = EnsurePanelSettings();
        if (panelSettings == null)
        {
            throw new InvalidOperationException("TopDog: DefaultPanelSettings.asset missing after Refresh");
        }

        var uxml = LoadUxmlAssets();
        AssertOutOfMatchUxml(uxml);

        RepairBootScene();
        RepairOutOfMatchScene(uxml);
        RepairOperationsScene(uxml);
        RepairCombatScene(uxml);
        RepairCombatRealtimeScene(uxml);

        AssetDatabase.SaveAssets();
        Debug.Log("TopDog: repaired script references on all scenes.");
    }

    private static void AssertScenesHaveClassicMonoScriptGuids()
    {
        var scenes = new[]
        {
            "Boot.unity",
            "OutOfMatch.unity",
            "Operations.unity",
            "Combat.unity",
            "CombatRealtime.unity",
        };
        foreach (var name in scenes)
        {
            var path = Path.Combine(ScenesDir, name);
            var text = File.ReadAllText(path);
            var noGuid = Regex.Matches(
                text,
                @"m_Script: \{fileID: (?!11500000)\d+\}").Count;
            // Allow UnityEngine UIDocument etc. with built-in guids (type: 0).
            if (Regex.IsMatch(text, @"m_EditorClassIdentifier: TopDog\.[^\r\n]+::TopDog\.") &&
                Regex.IsMatch(text, @"m_Script: \{fileID: (?!11500000)\d+\}"))
            {
                throw new InvalidOperationException(
                    "TopDog: " + path + " still has type-id-only TopDog MonoScript refs after GUID rewrite");
            }

            Debug.Log("TopDog: AssertScenesHaveClassicMonoScriptGuids OK " + name + " bareFileIds=" + noGuid);
        }
    }

    /// <summary>
    /// Unity 6000 batchmode may save MonoBehaviours as type-id-only <c>m_Script: {fileID: N}</c>.
    /// Player BuildPlayer then reports missing scripts and writes corrupt levelN (Position out of bounds).
    /// Rewrite to classic <c>fileID: 11500000, guid, type: 3</c> using MonoScript assets.
    /// </summary>
    public static void RewriteAllSceneMonoScriptsToClassicGuids()
    {
        var scenes = new[]
        {
            "Boot.unity",
            "OutOfMatch.unity",
            "Operations.unity",
            "Combat.unity",
            "CombatRealtime.unity",
        };
        var guidByType = BuildMonoScriptGuidIndex();
        foreach (var name in scenes)
        {
            RewriteSceneMonoScriptsToClassicGuids(Path.Combine(ScenesDir, name), guidByType);
        }

        AssetDatabase.Refresh();
    }

    private static Dictionary<string, string> BuildMonoScriptGuidIndex()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            var type = script != null ? script.GetClass() : null;
            if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                // Fallback: file name without extension matches short type name (batchmode GetClass can be null).
                var shortName = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(shortName) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    map["TopDog.Client." + shortName] = guid;
                    map[shortName] = guid;
                }

                continue;
            }

            map[type.FullName ?? type.Name] = guid;
            map[type.Name] = guid;
        }

        return map;
    }

    private static void RewriteSceneMonoScriptsToClassicGuids(string scenePath, Dictionary<string, string> guidByType)
    {
        if (!File.Exists(scenePath))
        {
            return;
        }

        var text = File.ReadAllText(scenePath);
        var rx = new Regex(
            @"m_Script: \{fileID: (?!11500000)\d+\}\r?\n  m_Name: \r?\n  m_EditorClassIdentifier: (?<asm>[^\r\n:]+)::(?<type>[^\r\n]+)",
            RegexOptions.Multiline);
        var replaced = 0;
        var missing = new List<string>();
        text = rx.Replace(text, match =>
        {
            var typeName = match.Groups["type"].Value.Trim();
            if (!guidByType.TryGetValue(typeName, out var guid))
            {
                var shortName = typeName;
                var dot = typeName.LastIndexOf('.');
                if (dot >= 0 && dot < typeName.Length - 1)
                {
                    shortName = typeName.Substring(dot + 1);
                }

                if (!guidByType.TryGetValue(shortName, out guid))
                {
                    missing.Add(typeName);
                    return match.Value;
                }
            }

            replaced++;
            return "m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}\n  m_Name: \n  m_EditorClassIdentifier: "
                   + match.Groups["asm"].Value + "::" + typeName;
        });

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "TopDog: no MonoScript GUID for: " + string.Join(", ", missing.Distinct()));
        }

        if (replaced > 0)
        {
            File.WriteAllText(scenePath, text);
            Debug.Log("TopDog: rewrote " + replaced + " MonoScript GUID refs in " + scenePath);
        }
    }

    /// <summary>
    /// Player build aborts with "levelN is corrupted" when scenes serialize stripped/missing MonoScripts.
    /// Call after RepairAllScenes and before BuildPipeline.BuildPlayer.
    /// </summary>
    public static void ValidateScenesHaveNoMissingScripts()
    {
        var scenes = new[]
        {
            "Boot.unity",
            "OutOfMatch.unity",
            "Operations.unity",
            "Combat.unity",
            "CombatRealtime.unity",
        };
        var failures = 0;
        foreach (var name in scenes)
        {
            var path = Path.Combine(ScenesDir, name);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                failures += CountMissingScriptsRecursive(root);
            }
        }

        if (failures > 0)
        {
            throw new InvalidOperationException(
                "TopDog: " + failures +
                " missing MonoBehaviour(s) in Scenes — fix RepairAllScenes before BuildPlayer (avoids levelN corrupted).");
        }

        Debug.Log("TopDog: ValidateScenesHaveNoMissingScripts OK");
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
        RebindMonoScriptsInScene();
        PurgeMissingScriptsInScene();
        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairOutOfMatchScene(UxmlAssets uxml)
    {
        // Camera-only scene: Unity 6000 batchmode + HybridCLR was serializing TopDogUI
        // MonoBehaviours into corrupt player level1 (Position out of bounds). UI spawns at runtime.
        _ = uxml;
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "OutOfMatch.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "TopDogUI" || root.GetComponent<UIDocument>() != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("TopDog: failed to save OutOfMatch.unity");
        }

        Debug.Log("TopDog: OutOfMatch.unity is camera-only — UI via OutOfMatchRuntimeBootstrap");
    }

    private static void RepairOperationsScene(UxmlAssets uxml)
    {
        _ = uxml;
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Operations.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "TopDogUI" || root.GetComponent<UIDocument>() != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TopDog: Operations.unity is camera-only — UI via OperationsUiRepair");
    }

    private static void RepairCombatScene(UxmlAssets uxml)
    {
        _ = uxml;
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "Combat.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "TopDogUI" || root.GetComponent<UIDocument>() != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TopDog: Combat.unity is camera-only — UI via CombatUiRepair");
    }

    private static void RepairCombatRealtimeScene(UxmlAssets uxml)
    {
        _ = uxml;
        var scene = EditorSceneManager.OpenScene(Path.Combine(ScenesDir, "CombatRealtime.unity"), OpenSceneMode.Single);
        PurgeMissingScriptsInScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "TopDogUI" || root.GetComponent<UIDocument>() != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        StyleMainCamera();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TopDog: CombatRealtime.unity is camera-only — UI via CombatUiRepair");
    }

    private static PanelSettings RequirePanelSettings()
    {
        // Prefer Resources copy — Settings asset uses a scaffold placeholder GUID that can
        // break player scene loads (Position out of bounds / levelN corrupted).
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/DefaultPanelSettings.asset");
        if (!panel)
        {
            panel = EnsurePanelSettings();
            SyncRuntimeUiResources(panel);
            AssetDatabase.Refresh();
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/DefaultPanelSettings.asset");
        }

        if (!panel)
        {
            throw new InvalidOperationException("TopDog: Resources/DefaultPanelSettings.asset missing");
        }

        return panel;
    }

    private static UxmlAssets RequireUxmlAssets()
    {
        var uxml = LoadUxmlAssets();
        AssertOutOfMatchUxml(uxml);
        return uxml;
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
        // Use Unity fake-null checks — C# == null can miss destroyed ScriptableObject refs after Refresh.
        if (!panelSettings)
        {
            throw new InvalidOperationException("TopDog: PanelSettings required for UIDocument");
        }

        if (!initialUxml)
        {
            throw new InvalidOperationException("TopDog: initial UXML required for UIDocument");
        }

        var uiGo = new GameObject("TopDogUI");
        var doc = uiGo.AddComponent<UIDocument>();
        // Prefer public API — SerializedObject “sourceAsset”/“m_PanelSettings” was saving as {fileID: 0}
        // in Unity 6000 batchmode, which produced corrupt player levelN files.
        doc.panelSettings = panelSettings;
        doc.visualTreeAsset = initialUxml;
        uiGo.AddComponent<UiViewportDriver>();
        EditorUtility.SetDirty(doc);
        EditorUtility.SetDirty(uiGo);
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

    /// <summary>
    /// Re-assign MonoScript assets after AddComponent so player builds keep resolvable refs
    /// (avoids Unity 6 batchmode "missing script" → corrupt levelN).
    /// </summary>
    private static void RebindMonoScriptsInScene()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            RebindMonoScriptsRecursive(root);
        }
    }

    private static void RebindMonoScriptsRecursive(GameObject go)
    {
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null)
            {
                continue;
            }

            var script = MonoScript.FromMonoBehaviour(mb);
            if (script == null)
            {
                Debug.LogError(
                    "TopDog: MonoScript.FromMonoBehaviour failed for " + mb.GetType().FullName +
                    " on " + go.name);
                continue;
            }

            var so = new SerializedObject(mb);
            var prop = so.FindProperty("m_Script");
            if (prop != null)
            {
                prop.objectReferenceValue = script;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        foreach (Transform child in go.transform)
        {
            RebindMonoScriptsRecursive(child.gameObject);
        }
    }

    private static int CountMissingScriptsRecursive(GameObject go)
    {
        var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        foreach (Transform child in go.transform)
        {
            count += CountMissingScriptsRecursive(child.gameObject);
        }

        return count;
    }

    private static void AssertOutOfMatchUxml(UxmlAssets uxml)
    {
        if (uxml.MainMenu == null || uxml.Worldline == null || uxml.Settings == null ||
            uxml.JoinLan == null || uxml.CustomLobby == null || uxml.StoryLevels == null)
        {
            throw new InvalidOperationException(
                "TopDog: Assets/UI/*.uxml missing after SyncRuntimeUiResources — cannot repair OutOfMatch.");
        }
    }

    private static void ValidateActiveUiDocumentWired(string sceneLabel)
    {
        var doc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
        if (doc == null)
        {
            throw new InvalidOperationException("TopDog: no UIDocument in " + sceneLabel);
        }

        if (doc.panelSettings == null)
        {
            throw new InvalidOperationException("TopDog: UIDocument.panelSettings null in " + sceneLabel);
        }

        if (doc.visualTreeAsset == null)
        {
            throw new InvalidOperationException("TopDog: UIDocument.visualTreeAsset null in " + sceneLabel);
        }

        Debug.Log("TopDog: UIDocument wired OK in " + sceneLabel + " → " + doc.visualTreeAsset.name);
    }

    private static void WireUiNavigator(UiNavigator nav, UIDocument doc, UxmlAssets uxml)
    {
        nav.Configure(
            doc,
            uxml.MainMenu,
            uxml.Worldline,
            uxml.Settings,
            uxml.JoinLan,
            uxml.CustomLobby,
            uxml.StoryLevels,
            null);
        EditorUtility.SetDirty(nav);
    }

    private static void WireOutOfMatchHost(OutOfMatchSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        AssignRefOrThrow(so, "uiDocument", doc);
        AssignRefOrThrow(so, "mainMenuUxml", uxml.MainMenu);
        AssignRefOrThrow(so, "worldlineUxml", uxml.Worldline);
        AssignRefOrThrow(so, "settingsUxml", uxml.Settings);
        AssignRefOrThrow(so, "joinLanUxml", uxml.JoinLan);
        AssignRefOrThrow(so, "customLobbyUxml", uxml.CustomLobby);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(host);
    }

    private static void AssignRefOrThrow(SerializedObject so, string property, UnityEngine.Object value)
    {
        var prop = so.FindProperty(property);
        if (prop == null)
        {
            throw new InvalidOperationException("TopDog: missing serialized property " + property + " on " + so.targetObject);
        }

        if (value == null)
        {
            throw new InvalidOperationException("TopDog: null asset for property " + property);
        }

        prop.objectReferenceValue = value;
    }

    private static void WireOperationsHost(OperationsSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        AssignRefOrThrow(so, "uiDocument", doc);
        AssignRefOrThrow(so, "campaignShellUxml", uxml.CampaignShell);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(host);
    }

    private static void WireCombatHost(CombatSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        AssignRefOrThrow(so, "uiDocument", doc);
        AssignRefOrThrow(so, "combatShellUxml", uxml.CombatShell);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(host);
    }

    private static void WireCombatRealtimeHost(CombatRealtimeSceneHost host, UIDocument doc, UxmlAssets uxml)
    {
        var so = new SerializedObject(host);
        AssignRefOrThrow(so, "uiDocument", doc);
        AssignRefOrThrow(so, "combatRealtimeUxml", uxml.CombatRealtime);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(host);
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