using UnityEngine;

namespace TopDog.Client;

/// <summary>Resolves active UI art skin (ScriptableObject or mod override).</summary>
public static class UiArtCatalog
{
    public const string DefaultSkinPath = "Assets/Settings/DefaultUiArtSkin.asset";

    private static IUiArtSkin? _active;
    private static IUiArtSkin? _fallback;

    public static IUiArtSkin Active
    {
        get
        {
            if (_active != null)
            {
                return _active;
            }

            _fallback ??= (IUiArtSkin?)LoadDefaultAsset() ?? EmptyUiArtSkin.Instance;
            return _fallback;
        }
    }

    public static void SetActive(IUiArtSkin? skin)
    {
        _active = skin;
    }

    public static void ResetToDefault()
    {
        _active = null;
    }

    private static UiArtSkinAsset? LoadDefaultAsset()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<UiArtSkinAsset>(DefaultSkinPath);
#else
        return Resources.Load<UiArtSkinAsset>("DefaultUiArtSkin");
#endif
    }

    private sealed class EmptyUiArtSkin : IUiArtSkin
    {
        public static readonly EmptyUiArtSkin Instance = new();
        public string SkinId => "empty";
        public Texture2D? GetTexture(UiScreenId screen, string slotId) => null;
        public Sprite? GetSprite(UiScreenId screen, string slotId) => null;
        public UnityEngine.UIElements.StyleSheet? GetGlobalStyleOverride() => null;
        public UnityEngine.UIElements.StyleSheet? GetScreenStyleOverride(UiScreenId screen) => null;
        public Font? GetFont(string fontId) => null;
    }
}
