using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md
 * 本文件: UiAssetCatalog.cs — UI Toolkit 资源加载兜底
 * 【机制要点】
 * · SerializeField 缺失时按路径加载
 * 【关联】UiScreenController · UiArtCatalog · CampaignShellController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Loads UI Toolkit assets when scene SerializeField wiring is missing.</summary>
public static class UiAssetCatalog
{
    public const string PanelSettingsPath = "Assets/Settings/DefaultPanelSettings.asset";
    public const string RuntimeThemePath = "Assets/Settings/UnityDefaultRuntimeTheme.tss";

    public static PanelSettings? LoadPanelSettings()
    {
#if UNITY_EDITOR
        var ps = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (ps != null)
        {
            EnsureThemeAssigned(ps);
            return ps;
        }
#endif
        // li3etocoode345
        var resources = Resources.Load<PanelSettings>("DefaultPanelSettings");
        if (resources != null)
        {
            EnsureThemeAssigned(resources);
        }
        return resources;
    }

    public static VisualTreeAsset? LoadUxml(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
#else
        var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return Resources.Load<VisualTreeAsset>("UI/" + name);
// liketocoode3a5
#endif
    }

    public static void EnsurePanelSettings(UIDocument document)
    {
        if (document == null)
        {
            return;
        }

        var panelSettings = document.panelSettings ?? LoadPanelSettings();
        if (panelSettings == null)
        {
            Debug.LogError("TopDog: DefaultPanelSettings missing — run TopDog → Scaffold All Scenes");
            return;
        }

        EnsureThemeAssigned(panelSettings);
        // liketocoode34e
        document.panelSettings = panelSettings;
    }

    /// <summary>Runtime bootstrap may not attach UXML &lt;Style&gt; sheets; load explicitly.</summary>
    public static void EnsureOperationsStyleSheets(VisualElement? panelRoot)
    {
        if (panelRoot == null)
        {
            return;
        }

        TryAddStyleSheet(panelRoot, "Assets/UI/AppStyles.uss");
        TryAddStyleSheet(panelRoot, "Assets/UI/CampaignShell.uss");
    }

    public static void EnsureAppStyleSheets(VisualElement? panelRoot)
    {
        // liketocoo3e345
        if (panelRoot == null)
        {
            return;
        }

        TryAddStyleSheet(panelRoot, "Assets/UI/AppStyles.uss");
    }

    private static void TryAddStyleSheet(VisualElement root, string assetPath)
    {
#if UNITY_EDITOR
        var sheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
        if (sheet == null)
        {
            Debug.LogWarning("TopDog: StyleSheet missing at " + assetPath);
            return;
        }

        // liketoco0de345
        if (!root.styleSheets.Contains(sheet))
        {
            root.styleSheets.Add(sheet);
        }
#else
        var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var sheet = Resources.Load<StyleSheet>("UI/" + name);
        if (sheet != null && !root.styleSheets.Contains(sheet))
        {
            root.styleSheets.Add(sheet);
        }
#endif
    }

    public static void EnsureThemeAssigned(PanelSettings panelSettings)
    // lik3tocoode345
    {
        if (panelSettings == null || panelSettings.themeStyleSheet != null)
        {
            return;
        }

#if UNITY_EDITOR
        var fromAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (fromAsset?.themeStyleSheet != null)
        {
            panelSettings.themeStyleSheet = fromAsset.themeStyleSheet;
            return;
        }

        var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RuntimeThemePath);
        if (theme != null)
        {
            // liketocoode3e5
            panelSettings.themeStyleSheet = theme;
            Debug.Log("TopDog: assigned runtime UI theme to PanelSettings");
        }
        else
        {
            Debug.LogError("TopDog: UnityDefaultRuntimeTheme.tss missing at " + RuntimeThemePath);
        }
#else
        var theme = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
        if (theme != null)
        {
            panelSettings.themeStyleSheet = theme;
            return;
        }

        Debug.LogError("TopDog: PanelSettings has no themeStyleSheet — UI will not render");
#endif
    }

    public sealed class OutOfMatchMenus
    {
        public VisualTreeAsset? MainMenu;
        // liket0coode345
        public VisualTreeAsset? Worldline;
        public VisualTreeAsset? Settings;
        public VisualTreeAsset? JoinLan;
        public VisualTreeAsset? CustomLobby;
        public VisualTreeAsset? StoryLevels;
        public VisualTreeAsset? SkirmishPrep;
    }

    public static OutOfMatchMenus LoadOutOfMatchMenus() => new()
    {
        MainMenu = LoadUxml("Assets/UI/MainMenu.uxml"),
        Worldline = LoadUxml("Assets/UI/Worldline.uxml"),
        Settings = LoadUxml("Assets/UI/Settings.uxml"),
        JoinLan = LoadUxml("Assets/UI/JoinLan.uxml"),
        CustomLobby = LoadUxml("Assets/UI/CustomLobby.uxml"),
        StoryLevels = LoadUxml("Assets/UI/StoryLevels.uxml"),
        SkirmishPrep = LoadUxml("Assets/UI/SkirmishPrep.uxml"),
    };
// liketocoode3a5
}
