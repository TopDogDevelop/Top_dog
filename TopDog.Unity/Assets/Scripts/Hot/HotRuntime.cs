using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md
 * 本文件: HotRuntime.cs — HybridCLR 热更程序集入口
 * ══
 */

namespace TopDog.Hot
{
    /// <summary>Entry invoked after hot assemblies are loaded (or Editor domain already has them).</summary>
    public static class HotRuntime
    {
        public const string AssemblyName = "TopDog.Hot";

        public static void NotifyBootReady(string contentVersion)
        {
            Debug.Log("TopDog.Hot: boot ready @ content " + contentVersion);
        }
    }
}
