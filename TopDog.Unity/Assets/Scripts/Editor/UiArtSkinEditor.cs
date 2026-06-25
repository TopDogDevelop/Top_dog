#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TopDog.Client.Editor;

public static class UiArtSkinEditor
{
    private const string DefaultPath = "Assets/Settings/DefaultUiArtSkin.asset";

    [MenuItem("TopDog/Create Default UI Art Skin")]
    public static void CreateDefaultSkin()
    {
        EnsureDefaultSkinAsset();
        var asset = AssetDatabase.LoadAssetAtPath<UiArtSkinAsset>(DefaultPath);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log("TopDog: UI art skin at " + DefaultPath);
    }

    public static void EnsureDefaultSkinAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<UiArtSkinAsset>(DefaultPath) != null)
        {
            return;
        }

        var dir = Path.GetDirectoryName(DefaultPath);
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(dir!);
            AssetDatabase.Refresh();
        }

        var skin = ScriptableObject.CreateInstance<UiArtSkinAsset>();
        AssetDatabase.CreateAsset(skin, DefaultPath);
        AssetDatabase.SaveAssets();
    }
}
#endif
