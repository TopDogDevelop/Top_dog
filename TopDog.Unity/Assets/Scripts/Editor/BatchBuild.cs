using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §1 · docs/SCENE_ARCHITECTURE.md
 * 本文件: BatchBuild.cs — Win/Android 批处理打包入口
 * 【机制要点】
 * · 打包前 SyncRuntimeUiResources + RepairAllScenes（非 Boot 场景仅相机，防 levelN corrupted）
 * · HybridCLR GenerateAll 在 Repair 前；关闭 Splash；AppIcon；产 builds/；套壳 → disv1 两文件
 * 【关联】ProjectScaffold · build_and_publish_disv1.ps1 · publish_online_update.ps1
 * ══
 */

namespace TopDog.Editor
{
    /// <summary>Batchmode entry points for Windows / Android player builds.</summary>
    public static class BatchBuild
    {
        private const string AppIconPath = "Assets/Art/AppIcon/AppIcon.png";

        private static readonly string[] DefaultScenes =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/OutOfMatch.unity",
            "Assets/Scenes/Operations.unity",
            "Assets/Scenes/Combat.unity",
            "Assets/Scenes/CombatRealtime.unity",
        };

        private static void ApplyReleasePlayerSettings()
        {
            PlayerSettings.productName = "TopDog";
            PlayerSettings.companyName = "TopDogDevelop";
            try
            {
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.SplashScreen.showUnityLogo = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("SplashScreen settings skipped: " + e.Message);
            }

            // HybridCLR requires IL2CPP on both platforms (Unity 6000.3.x + HC 8.12).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            ApplyAppIcon();
        }

        private static void ApplyAppIcon()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Art", "AppIcon", "AppIcon.png")))
            {
                Debug.LogWarning("TopDog: AppIcon.png missing at " + AppIconPath);
                return;
            }

            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(AppIconPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (tex == null)
            {
                Debug.LogError("TopDog: failed to load AppIcon texture");
                return;
            }

            try
            {
                FillIcons(NamedBuildTarget.Standalone, IconKind.Application, tex);
                FillIcons(NamedBuildTarget.Android, IconKind.Application, tex);
                ApplyAndroidPlatformIcons(tex);
                Debug.Log("TopDog: applied AppIcon for Standalone + Android");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TopDog: SetIcons failed, fallback: " + e.Message);
#pragma warning disable CS0618
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new[] { tex });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { tex });
#pragma warning restore CS0618
            }
        }

        private static void FillIcons(NamedBuildTarget target, IconKind kind, Texture2D tex)
        {
            var sizes = PlayerSettings.GetIconSizes(target, kind);
            if (sizes == null || sizes.Length == 0)
            {
                PlayerSettings.SetIcons(target, new[] { tex }, kind);
                return;
            }

            var arr = new Texture2D[sizes.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = tex;
            }

            PlayerSettings.SetIcons(target, arr, kind);
        }

        private static void ApplyAndroidPlatformIcons(Texture2D tex)
        {
            try
            {
                var kinds = new[]
                {
                    UnityEditor.Android.AndroidPlatformIconKind.Adaptive,
                    UnityEditor.Android.AndroidPlatformIconKind.Round,
                    UnityEditor.Android.AndroidPlatformIconKind.Legacy,
                };
                foreach (var kind in kinds)
                {
                    var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                    if (icons == null || icons.Length == 0)
                    {
                        continue;
                    }

                    foreach (var icon in icons)
                    {
                        icon.SetTextures(new[] { tex });
                    }

                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TopDog: Android platform icons skipped: " + e.Message);
            }
        }

        private static void EnsureUiResourcesForPlayer()
        {
            D3d12DeviceFilterBootstrap.EnsureForPlayer();
            EnsureCombatSkyboxShadersAlwaysIncluded();
            // GenerateAll before scene repair: HybridCLR/il2cpp prep must not invalidate
            // MonoScript refs that RepairAllScenes just wrote (caused level1 corrupted).
            TryHybridClrGenerateAll();
            var panel = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(
                "Assets/Settings/DefaultPanelSettings.asset");
            TopDog.Client.Editor.ProjectScaffold.SyncRuntimeUiResources(panel);
            AssetDatabase.Refresh();
            TopDog.Client.Editor.ProjectScaffold.RepairAllScenes();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Android/IL2CPP strips Shader.Find-only refs; keep combat skybox shaders in player.
        /// </summary>
        private static void EnsureCombatSkyboxShadersAlwaysIncluded()
        {
            var names = new[]
            {
                "TopDog/CombatSkyboxInterior",
                "TopDog/CombatSkyboxEquirectInterior",
                "Skybox/Cubemap",
            };
            var graphicsSettingsObj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettingsObj == null || graphicsSettingsObj.Length == 0)
            {
                Debug.LogWarning("TopDog: GraphicsSettings.asset missing — cannot pin skybox shaders");
                return;
            }

            var so = new UnityEditor.SerializedObject(graphicsSettingsObj[0]);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null || !arr.isArray)
            {
                Debug.LogWarning("TopDog: m_AlwaysIncludedShaders missing");
                return;
            }

            var added = 0;
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning("TopDog: AlwaysIncluded skip missing shader " + name);
                    continue;
                }

                var found = false;
                for (var i = 0; i < arr.arraySize; i++)
                {
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
                added++;
            }

            if (added > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("TopDog: AlwaysIncludedShaders +=" + added + " (combat skybox)");
            }
        }

        private static void TryHybridClrGenerateAll()
        {
            try
            {
                HybridClrSettingsBootstrap.Apply();
                if (!HybridClrSettingsBootstrap.EnableHybridClrPlayer)
                {
                    // Clear redirect so stock Unity IL2CPP is used (e.g. mismatched Editor vs HC branch).
                    Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", null);
                    Debug.Log("HybridCLR player disabled — using stock IL2CPP (see HybridClrSettingsBootstrap)");
                    return;
                }

                var c = new HybridCLR.Editor.Installer.InstallerController();
                if (!c.HasInstalledHybridCLR())
                {
                    Debug.LogWarning("HybridCLR not installed — skipping GenerateAll");
                    return;
                }

                HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
                Debug.Log("HybridCLR GenerateAll OK before player build");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HybridCLR GenerateAll skipped: " + e.Message);
            }
        }

        public static void BuildWindows()
        {
            ApplyReleasePlayerSettings();
            EnsureUiResourcesForPlayer();
            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "builds", "windows", "TopDog.exe"));
            var outDir = Path.GetDirectoryName(outPath)!;
            Directory.CreateDirectory(outDir);
            // Avoid "project previously built with Mono2x" when switching scripting backend
            CleanPlayerOutputDir(outDir, keepLogName: "build_windows.log");
            Debug.Log("BatchBuild.BuildWindows -> " + outPath);
            var report = BuildPipeline.BuildPlayer(DefaultScenes, outPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Windows build failed: " + report.summary.result);
                EditorApplication.Exit(1);
                return;
            }

            TryPatchWindowsExeIcon(outPath);
            Debug.Log("Windows build ok -> " + outPath);
            EditorApplication.Exit(0);
        }

        private static void TryPatchWindowsExeIcon(string exePath)
        {
            var ico = Path.GetFullPath(Path.Combine(Application.dataPath, "Art", "AppIcon", "AppIcon.ico"));
            if (!File.Exists(ico) || !File.Exists(exePath))
            {
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "rcedit-x64.exe",
                    Arguments = "\"" + exePath + "\" --set-icon \"" + ico + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit(15000);
                if (p is { ExitCode: 0 })
                {
                    Debug.Log("TopDog: rcedit set exe icon");
                }
            }
            catch
            {
                // PlayerSettings icons are enough when rcedit is absent.
            }
        }

        public static void BuildAndroid()
        {
            ApplyReleasePlayerSettings();
            EnsureUiResourcesForPlayer();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.topdogdevelop.topdog");
            // API 26+：已覆盖 2020 年后全部 Android 正式版（见 RELEASE_AND_HOTUPDATE §2.6）
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // 2=ARM64 only；改为 ARMv7|ARM64 以覆盖更多 2020 前后机型
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "builds", "android", "TopDog.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            DeleteBuildOutputIfExists(outPath);
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            var report = BuildPipeline.BuildPlayer(DefaultScenes, outPath, BuildTarget.Android, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Android build failed: " + report.summary.result);
                EditorApplication.Exit(1);
                return;
            }

            if (!File.Exists(outPath) || Directory.Exists(outPath))
            {
                Debug.LogError("Android build did not produce APK file at " + outPath +
                               " (exportAsGoogleAndroidProject may be on)");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Android build ok -> " + outPath + " (" + new FileInfo(outPath).Length + " bytes)");
            EditorApplication.Exit(0);
        }

        private static void DeleteBuildOutputIfExists(string outPath)
        {
            try
            {
                if (Directory.Exists(outPath))
                {
                    Directory.Delete(outPath, true);
                    Debug.Log("BatchBuild: removed previous directory " + outPath);
                }
                else if (File.Exists(outPath))
                {
                    File.Delete(outPath);
                    Debug.Log("BatchBuild: removed previous " + outPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("BatchBuild: could not delete " + outPath + ": " + e.Message);
            }
        }

        private static void CleanPlayerOutputDir(string outDir, string keepLogName)
        {
            if (!Directory.Exists(outDir))
            {
                return;
            }

            foreach (var path in Directory.GetFileSystemEntries(outDir))
            {
                var name = Path.GetFileName(path);
                if (string.Equals(name, keepLogName, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                    else
                    {
                        File.Delete(path);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("BatchBuild: clean skipped " + path + ": " + e.Message);
                }
            }
        }
    }
}
