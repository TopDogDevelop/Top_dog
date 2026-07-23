#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>首次打开工程：配置 URP、保存默认 SampleScene、挂上立场展示。</summary>
[InitializeOnLoad]
public static class FieldAuraVfxDemoSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PipelinePath = "Assets/Settings/UniversalRP.asset";
    private const string RendererPath = "Assets/Settings/ForwardRenderer.asset";
    private const string SessionKey = "FieldAuraVfxDemo.Initialized";

    static FieldAuraVfxDemoSetup()
    {
        EditorApplication.delayCall += EnsureProjectReady;
    }

    [MenuItem("TopDog/Field Aura Demo/Rebuild Showcase Scene")]
    public static void RebuildShowcaseScene()
    {
        SessionState.EraseBool(SessionKey);
        EnsureProjectReady();
    }

    static void EnsureProjectReady()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        EnsureFolders();
        EnsureUrpPipeline();
        EnsureShowcaseScene();
        SessionState.SetBool(SessionKey, true);
        Debug.Log("FieldAuraVfxDemo ready — open SampleScene and press Play (or view Scene view).");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }
    }

    static void EnsureUrpPipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (pipeline == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);

            pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }

        if (GraphicsSettings.defaultRenderPipeline != pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
        }

        QualitySettings.renderPipeline = pipeline;
        AssetDatabase.SaveAssets();
    }

    static void EnsureShowcaseScene()
    {
        Scene scene;
        if (File.Exists(ScenePath))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        if (Object.FindAnyObjectByType<FieldAuraShowcase>() == null)
        {
            var root = new GameObject("FieldAuraShowcase");
            root.AddComponent<FieldAuraShowcase>();
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 6f, -30f);
            cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
            if (cam.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
        }

        if (!scene.isDirty && File.Exists(ScenePath))
        {
            return;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorBuildSettings.scenes = scenes;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    public static void BatchSetup()
    {
        EnsureProjectReady();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }
}
#endif
