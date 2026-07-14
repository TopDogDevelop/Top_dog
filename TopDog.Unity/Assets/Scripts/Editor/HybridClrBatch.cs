using System.IO;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §2.7 · §5
 * 本文件: HybridClrBatch.cs — 批处理安装 / Generate All；zlib→zlib-unity 联接
 * ══
 */

namespace TopDog.Editor
{
    public static class HybridClrBatch
    {
        [MenuItem("TopDog/HybridCLR/Install Default")]
        public static void InstallDefaultMenu() => InstallDefault();

        [MenuItem("TopDog/HybridCLR/Generate All")]
        public static void GenerateAllMenu() => GenerateAll();

        /// <summary>Unity -executeMethod TopDog.Editor.HybridClrBatch.InstallDefault</summary>
        public static void InstallDefault()
        {
            var c = new InstallerController();
            if (c.GetCompatibleType() == InstallerController.CompatibleType.Incompatible)
            {
                Debug.LogError("HybridCLR incompatible with this Unity version");
                EditorApplication.Exit(2);
                return;
            }

            Debug.Log("HybridCLR InstallDefault starting…");
            c.InstallDefaultHybridCLR();
            Debug.Log("HybridCLR InstallDefault done. HasInstalled=" + c.HasInstalledHybridCLR());
            EnsureZlibJunctionForUnity6000();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(c.HasInstalledHybridCLR() ? 0 : 1);
            }
        }

        /// <summary>
        /// Unity 6000.x ships external/zlib-unity; HybridCLR libil2cpp still #includes external/zlib.
        /// </summary>
        private static void EnsureZlibJunctionForUnity6000()
        {
            var il2cpp = HybridCLR.Editor.SettingsUtil.LocalIl2CppDir;
            var zlibUnity = Path.Combine(il2cpp, "external", "zlib-unity");
            var zlib = Path.Combine(il2cpp, "external", "zlib");
            if (!Directory.Exists(zlibUnity))
            {
                return;
            }

            if (Directory.Exists(zlib) || File.Exists(zlib))
            {
                return;
            }

            try
            {
                // Directory junction (no admin required on same volume)
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c mklink /J \"" + zlib + "\" \"" + zlibUnity + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit(10000);
                Debug.Log("HybridCLR: linked external/zlib -> zlib-unity (exit=" + (p?.ExitCode ?? -1) + ")");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("HybridCLR: zlib junction failed: " + e.Message);
            }
        }

        /// <summary>Unity -executeMethod TopDog.Editor.HybridClrBatch.GenerateAll</summary>
        public static void GenerateAll()
        {
            HybridClrSettingsBootstrap.Apply();
            Debug.Log("HybridCLR GenerateAll starting…");
            PrebuildCommand.GenerateAll();
            Debug.Log("HybridCLR GenerateAll done");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Install if needed then GenerateAll. -executeMethod TopDog.Editor.HybridClrBatch.InstallThenGenerate</summary>
        public static void InstallThenGenerate()
        {
            HybridClrSettingsBootstrap.Apply();
            var c = new InstallerController();
            if (!c.HasInstalledHybridCLR())
            {
                c.InstallDefaultHybridCLR();
            }

            EnsureZlibJunctionForUnity6000();
            PrebuildCommand.GenerateAll();
            AssetDatabase.SaveAssets();
            Debug.Log("HybridCLR InstallThenGenerate done. HasInstalled=" + c.HasInstalledHybridCLR());
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(c.HasInstalledHybridCLR() ? 0 : 1);
            }
        }
    }
}
