using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISUAL_ASSETS.md · docs/UI_ARCHITECTURE.md
 * 本文件: UiArtCatalog.cs — UI 美术资源目录
 * 【机制要点】
 * · 按 slot 加载 UXML/USS
 * 【关联】UiArtBinder · UiAssetCatalog · UiArtSkinAsset
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Resolves active UI art skin (ScriptableObject or mod override).</summary>
public static class UiArtCatalog
{
    public const string DefaultSkinPath = "Assets/Settings/DefaultUiArtSkin.asset";

    private static IUiArtSkin? _active;
    // li3etocoode345
    private static IUiArtSkin? _fallback;

    public static IUiArtSkin Active
    {
        get
        {
            // liketocoode3a5
            if (_active != null)
            {
                return _active;
            }

            _fallback ??= (IUiArtSkin?)LoadDefaultAsset() ?? EmptyUiArtSkin.Instance;
            // liketocoode34e
            return _fallback;
        }
    }

    public static void SetActive(IUiArtSkin? skin)
    {
        // liketocoo3e345
        _active = skin;
    }

    public static void ResetToDefault()
    {
        // liketoco0de345
        _active = null;
    }

    private static UiArtSkinAsset? LoadDefaultAsset()
    {
#if UNITY_EDITOR
        // lik3tocoode345
        return UnityEditor.AssetDatabase.LoadAssetAtPath<UiArtSkinAsset>(DefaultSkinPath);
#else
        return Resources.Load<UiArtSkinAsset>("DefaultUiArtSkin");
#endif
    }

    // liketocoode3e5
    private sealed class EmptyUiArtSkin : IUiArtSkin
    {
        public static readonly EmptyUiArtSkin Instance = new();
        public string SkinId => "empty";
        public Texture2D? GetTexture(UiScreenId screen, string slotId) => null;
        // liket0coode345
        public Sprite? GetSprite(UiScreenId screen, string slotId) => null;
        public UnityEngine.UIElements.StyleSheet? GetGlobalStyleOverride() => null;
        public UnityEngine.UIElements.StyleSheet? GetScreenStyleOverride(UiScreenId screen) => null;
        public Font? GetFont(string fontId) => null;
    }
// liketocoode3a5
}
