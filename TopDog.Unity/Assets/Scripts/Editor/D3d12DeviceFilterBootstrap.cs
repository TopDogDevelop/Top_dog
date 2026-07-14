using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §1.5 · Unity D3D12 Device Filter
 * 本文件: D3d12DeviceFilterBootstrap.cs — 允许弱核集显走 D3D12，避免默认 Deny 掉回 D3D11 后闪退
 * 【机制要点】
 * · Unity 默认 Deny：内核 ≤4 且 Integrated → 禁 D3D12（SSH 测试机 Intel HD 530 命中）
 * · 空 Deny + Allow Intel Integrated → Prefer D3D12 FL12.1；写入 PlayerSettings
 * · 由 BatchBuild.EnsureUiResourcesForPlayer 调用
 * 【关联】BatchBuild · ProjectSettings.d3d12DeviceFilterListAsset
 * ══
 */

namespace TopDog.Editor
{
    public static class D3d12DeviceFilterBootstrap
    {
        private const string AssetPath = "Assets/Settings/TopDog_D3D12DeviceFilter.asset";

        public static void EnsureForPlayer()
        {
            try
            {
                var lists = AssetDatabase.LoadAssetAtPath<D3D12DeviceFilterLists>(AssetPath);
                if (lists == null)
                {
                    lists = new D3D12DeviceFilterLists();
                    AssetDatabase.CreateAsset(lists, AssetPath);
                }

                // Clear Unity-default denials (≤4 cores / weak integrated) that force D3D11 on the SSH box.
                lists.d3D12DeviceDenyFilters = Array.Empty<D3D12DeviceFilterData>();
                // Explicitly allow Intel integrated (HD 530 etc.) to use D3D12 (Allow overrides Deny).
                lists.d3D12DeviceAllowFilters = new[]
                {
                    new D3D12DeviceFilterData
                    {
                        vendorName = "Intel",
                        deviceType = D3D12GraphicsDeviceType.Integrated,
                    },
                };
                lists.EnsureValidOrThrow();
                EditorUtility.SetDirty(lists);

                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
                PlayerSettings.SetGraphicsAPIs(
                    BuildTarget.StandaloneWindows64,
                    new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });

                var projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (projectSettings != null && projectSettings.Length > 0)
                {
                    var so = new SerializedObject(projectSettings[0]);
                    var prop = so.FindProperty("d3d12DeviceFilterListAsset");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = lists;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    else
                    {
                        Debug.LogWarning("TopDog: d3d12DeviceFilterListAsset property missing");
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log("TopDog: D3D12 device filter — empty Deny + Allow Intel integrated; Standalone APIs D3D12→D3D11");
            }
            catch (Exception e)
            {
                Debug.LogWarning("TopDog: D3d12DeviceFilterBootstrap failed: " + e.Message);
            }
        }
    }
}
