using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TopDog.Editor
{
    /// <summary>
    /// HybridCLR settings for TopDog.
    /// 权威: docs/RELEASE_AND_HOTUPDATE.md §2.7 · §5
    /// Unity 6000.3.x LTS + HybridCLR 8.12 (native branch v6000.3.x-*) — enable player HybridCLR.
    /// </summary>
    public static class HybridClrSettingsBootstrap
    {
        /// <summary>True on 6000.3.x with HybridCLR 8.12; keep false only on mismatched Editor (e.g. 6000.5).</summary>
        public const bool EnableHybridClrPlayer = true;

        [InitializeOnLoadMethod]
        private static void EnsureHotAssemblyListed()
        {
            EditorApplication.delayCall += Apply;
        }

        [MenuItem("TopDog/HybridCLR/Configure Hot Assemblies")]
        public static void ApplyMenu() => Apply();

        public static void Apply()
        {
            var settings = HybridCLRSettings.LoadOrCreate();
            settings.enable = EnableHybridClrPlayer;
            var asmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(
                "Assets/Scripts/Hot/TopDog.Hot.asmdef");
            // asmdef list only — do not also put the name in hotUpdateAssemblies (duplicate → build fail)
            settings.hotUpdateAssemblyDefinitions = asmdef != null
                ? new[] { asmdef }
                : System.Array.Empty<AssemblyDefinitionAsset>();
            settings.hotUpdateAssemblies = System.Array.Empty<string>();
            if (settings.patchAOTAssemblies == null || settings.patchAOTAssemblies.Length == 0)
            {
                settings.patchAOTAssemblies = new[]
                {
                    "mscorlib",
                    "System",
                    "System.Core",
                    "TopDog.Core",
                    "TopDog.Client",
                };
            }

            HybridCLRSettings.Save();
            Debug.Log("HybridCLRSettings: enable=" + settings.enable +
                      " hotUpdateAssemblyDefinitions=TopDog.Hot saved");
        }
    }
}
