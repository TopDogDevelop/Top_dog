using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

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
            panelSettings.themeStyleSheet = theme;
            Debug.Log("TopDog: assigned runtime UI theme to PanelSettings");
        }
        else
        {
            Debug.LogError("TopDog: UnityDefaultRuntimeTheme.tss missing at " + RuntimeThemePath);
        }
#else
        Debug.LogError("TopDog: PanelSettings has no themeStyleSheet — UI will not render");
#endif
    }

    public sealed class OutOfMatchMenus
    {
        public VisualTreeAsset? MainMenu;
        public VisualTreeAsset? Worldline;
        public VisualTreeAsset? Settings;
        public VisualTreeAsset? JoinLan;
        public VisualTreeAsset? CustomLobby;
        public VisualTreeAsset? StoryLevels;
    }

    public static OutOfMatchMenus LoadOutOfMatchMenus() => new()
    {
        MainMenu = LoadUxml("Assets/UI/MainMenu.uxml"),
        Worldline = LoadUxml("Assets/UI/Worldline.uxml"),
        Settings = LoadUxml("Assets/UI/Settings.uxml"),
        JoinLan = LoadUxml("Assets/UI/JoinLan.uxml"),
        CustomLobby = LoadUxml("Assets/UI/CustomLobby.uxml"),
        StoryLevels = LoadUxml("Assets/UI/StoryLevels.uxml"),
    };
}
