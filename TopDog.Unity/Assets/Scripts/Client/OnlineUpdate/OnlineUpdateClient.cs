using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TopDog.Net.Lan;
using UnityEngine;
using UnityEngine.Networking;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §倒Y · §2 · docs/ONLINE_UPDATE.md
 * 本文件: OnlineUpdateClient.cs — HF 内容热更下载与应用
 * 【机制要点】
 * · 仅下载缺失或 sha256 不同的文件；跳过已匹配本地/包内
 * · 倒Y：hotupdate/ 仅拉本端平台目录（windows-x64|android），对端 DLL 不分发到本机
 * · 不删除 manifest 未列出的本地文件（废弃资源保留）
 * 【关联】OnlineUpdateBootstrap · ContentRootBootstrap · HybridClrHotLoader · ContentVersionGate
 * ══
 */

namespace TopDog.Client.OnlineUpdate;

[Serializable]
public sealed class OnlineVersionDto
{
    public string version = "";
    public string publishedAt = "";
    public string baseUrl = "";
    public string notes = "";
}

[Serializable]
public sealed class OnlineManifestDto
{
    public string version = "";
    public OnlineManifestFileDto[] files = Array.Empty<OnlineManifestFileDto>();
}

[Serializable]
public sealed class OnlineManifestFileDto
{
    public string path = "";
    public string sha256 = "";
    public long size;
}

/// <summary>Fetches version/manifest/files from GitHub Pages and applies into persistentDataPath.</summary>
public static class OnlineUpdateClient
{
    public sealed class CheckResult
    {
        public bool Ok;
        public bool NeedsUpdate;
        public string LocalVersion = "";
        public string RemoteVersion = "";
        public string Message = "";
        public OnlineVersionDto? Remote;
    }

    public sealed class ApplyResult
    {
        public bool Ok;
        public string Message = "";
        public int Downloaded;
        public int Skipped;
        public long BytesDownloaded;
        public long BytesTotal;
    }

    /// <summary>Byte-level download progress for HUD progress bars.</summary>
    public sealed class ProgressInfo
    {
        public string Status = "";
        public long BytesDone;
        public long BytesTotal;
        public float Fraction =>
            BytesTotal > 0 ? Math.Min(1f, (float)BytesDone / BytesTotal) : 0f;
    }

    public static string OnlineUpdateRoot =>
        Path.Combine(Application.persistentDataPath, OnlineUpdateConfig.OnlineUpdateDirName);

    public static string ContentRuntimeRoot =>
        Path.Combine(Application.persistentDataPath, OnlineUpdateConfig.ContentRuntimeDirName);

    public static string AppliedVersionPath =>
        Path.Combine(OnlineUpdateRoot, OnlineUpdateConfig.AppliedVersionFile);

    public static string ReadLocalVersion()
    {
        try
        {
            if (File.Exists(AppliedVersionPath))
            {
                var json = File.ReadAllText(AppliedVersionPath, Encoding.UTF8);
                var dto = JsonUtility.FromJson<OnlineVersionDto>(json);
                if (!string.IsNullOrWhiteSpace(dto?.version))
                {
                    return dto.version.Trim();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("OnlineUpdate read local version: " + e.Message);
        }

        return OnlineUpdateConfig.FallbackLocalVersion;
    }

    public static void SyncGateFromDisk()
    {
        ContentVersionGate.Set(ReadLocalVersion());
    }

    public static async Task<CheckResult> CheckAsync(int timeoutMs = 8000)
    {
        var local = ReadLocalVersion();
        ContentVersionGate.Set(local);
        var result = new CheckResult { LocalVersion = local, RemoteVersion = local };
        try
        {
            using var req = UnityWebRequest.Get(OnlineUpdateConfig.VersionUrl);
            req.timeout = Math.Max(3, timeoutMs / 1000);
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                result.Ok = false;
                result.Message = "无法检查更新: " + req.error;
                return result;
            }

            var remote = JsonUtility.FromJson<OnlineVersionDto>(req.downloadHandler.text);
            if (remote == null || string.IsNullOrWhiteSpace(remote.version))
            {
                result.Ok = false;
                result.Message = "远端 version.json 无效";
                return result;
            }

            OnlineUpdateConfig.ApplyRemoteBaseUrl(remote.baseUrl);
            result.Ok = true;
            result.Remote = remote;
            result.RemoteVersion = remote.version.Trim();
            var cmp = ContentVersionGate.Compare(result.RemoteVersion, local);
            if (cmp > 0)
            {
                result.NeedsUpdate = true;
                result.Message = $"远端 {result.RemoteVersion} / 本地 {local}，是否更新？";
            }
            else if (cmp < 0)
            {
                result.NeedsUpdate = false;
                result.Message = $"远端 {result.RemoteVersion} 落后于本机 {local}，跳过下载";
            }
            else
            {
                result.NeedsUpdate = false;
                result.Message = "已是最新内容版 " + local;
            }

            return result;
        }
        catch (Exception e)
        {
            result.Ok = false;
            result.Message = "无法检查更新: " + e.Message;
            return result;
        }
    }

    public static async Task<ApplyResult> ApplyRemoteAsync(
        OnlineVersionDto remote,
        Action<string>? status = null,
        Action<ProgressInfo>? progress = null,
        int timeoutMs = 30000)
    {
        var apply = new ApplyResult();
        void Report(string text, long done, long total)
        {
            status?.Invoke(text);
            progress?.Invoke(new ProgressInfo
            {
                Status = text,
                BytesDone = done,
                BytesTotal = total,
            });
        }

        try
        {
            OnlineUpdateConfig.ApplyRemoteBaseUrl(remote?.baseUrl);
            Report("下载清单…", 0, 0);
            using var manReq = UnityWebRequest.Get(OnlineUpdateConfig.ManifestUrl);
            manReq.timeout = Math.Max(5, timeoutMs / 1000);
            var manOp = manReq.SendWebRequest();
            while (!manOp.isDone)
            {
                await Task.Yield();
            }

            if (manReq.result != UnityWebRequest.Result.Success)
            {
                apply.Message = "下载 manifest 失败: " + manReq.error;
                return apply;
            }

            var manifest = JsonUtility.FromJson<OnlineManifestDto>(manReq.downloadHandler.text);
            if (manifest?.files == null)
            {
                apply.Message = "manifest 无效";
                return apply;
            }

            if (!string.Equals(manifest.version?.Trim(), remote.version?.Trim(), StringComparison.Ordinal))
            {
                apply.Message = "manifest 版号与 version.json 不一致";
                return apply;
            }

            Directory.CreateDirectory(ContentRuntimeRoot);
            // 首次：用包内 StreamingAssets 填齐缺失文件（不删已有/废弃文件）
            EnsureRuntimeFilledFromStreamingMissingOnly();

            // First pass: decide which files need network download and sum byte totals.
            var plan = new List<(OnlineManifestFileDto file, string rel, string dest, bool needNet)>();
            long bytesTotal = 0;
            foreach (var file in manifest.files)
            {
                if (file == null || string.IsNullOrWhiteSpace(file.path))
                {
                    continue;
                }

                var rel = file.path.Replace('\\', '/').TrimStart('/');
                if (!IsManifestPathWantedForThisPlatform(rel))
                {
                    continue;
                }

                var dest = Path.Combine(ContentRuntimeRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                if (LocalFileMatchesSha256(dest, file.sha256))
                {
                    plan.Add((file, rel, dest, false));
                    continue;
                }

                var streamingCandidate = Path.Combine(
                    Application.streamingAssetsPath,
                    rel.Replace('/', Path.DirectorySeparatorChar));
                if (LocalFileMatchesSha256(streamingCandidate, file.sha256))
                {
                    plan.Add((file, rel, dest, false));
                    continue;
                }

                plan.Add((file, rel, dest, true));
                bytesTotal += Math.Max(0, file.size);
            }

            apply.BytesTotal = bytesTotal;
            long bytesDone = 0;
            var n = 0;
            var total = plan.Count;
            foreach (var item in plan)
            {
                n++;
                var file = item.file;
                var rel = item.rel;
                var dest = item.dest;

                if (!item.needNet)
                {
                    if (LocalFileMatchesSha256(dest, file.sha256))
                    {
                        apply.Skipped++;
                        Report(
                            $"跳过 {n}/{total}: {rel} · {FormatBytes(bytesDone)}/{FormatBytes(bytesTotal)}",
                            bytesDone,
                            bytesTotal);
                        continue;
                    }

                    var streamingCandidate = Path.Combine(
                        Application.streamingAssetsPath,
                        rel.Replace('/', Path.DirectorySeparatorChar));
                    if (LocalFileMatchesSha256(streamingCandidate, file.sha256))
                    {
                        var destDir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        File.Copy(streamingCandidate, dest, true);
                        apply.Skipped++;
                        Report(
                            $"复用包内 {n}/{total}: {rel} · {FormatBytes(bytesDone)}/{FormatBytes(bytesTotal)}",
                            bytesDone,
                            bytesTotal);
                        continue;
                    }
                }

                var expected = Math.Max(0, file.size);
                Report(
                    $"下载 {n}/{total}: {rel} · {FormatBytes(bytesDone)}/{FormatBytes(bytesTotal)}",
                    bytesDone,
                    bytesTotal);
                var url = OnlineUpdateConfig.Combine(OnlineUpdateConfig.BaseUrl, rel);
                using var fileReq = UnityWebRequest.Get(url);
                fileReq.timeout = Math.Max(10, timeoutMs / 1000);
                var fileOp = fileReq.SendWebRequest();
                while (!fileOp.isDone)
                {
                    var partial = Math.Min(expected, (long)fileReq.downloadedBytes);
                    Report(
                        $"下载 {n}/{total}: {rel} · {FormatBytes(bytesDone + partial)}/{FormatBytes(bytesTotal)}",
                        bytesDone + partial,
                        bytesTotal);
                    await Task.Yield();
                }

                if (fileReq.result != UnityWebRequest.Result.Success)
                {
                    apply.Message = "下载失败 " + rel + ": " + fileReq.error;
                    return apply;
                }

                var bytes = fileReq.downloadHandler.data;
                if (!string.IsNullOrWhiteSpace(file.sha256))
                {
                    var hash = Sha256Hex(bytes);
                    if (!string.Equals(hash, file.sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        apply.Message = "校验失败 " + rel;
                        return apply;
                    }
                }

                var outDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                File.WriteAllBytes(dest, bytes);
                apply.Downloaded++;
                var got = bytes?.Length ?? 0;
                bytesDone += got > 0 ? got : expected;
                apply.BytesDownloaded = bytesDone;
                Report(
                    $"下载 {n}/{total}: {rel} · {FormatBytes(bytesDone)}/{FormatBytes(bytesTotal)}",
                    bytesDone,
                    bytesTotal);
            }

            // 故意不删除 manifest 未列出的本地文件（废弃资源保留）
            Report("应用更新…", bytesDone, bytesTotal);
            WriteAppliedVersion(remote);
            ContentVersionGate.Set(remote.version);
            apply.Ok = true;
            apply.BytesDownloaded = bytesDone;
            apply.Message = "已更新到 " + remote.version
                + "（下载 " + apply.Downloaded + " / 跳过 " + apply.Skipped
                + " · " + FormatBytes(bytesDone) + "）";
            return apply;
        }
        catch (Exception e)
        {
            apply.Message = "更新失败: " + e.Message;
            return apply;
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            bytes = 0;
        }

        if (bytes < 1024)
        {
            return bytes + " B";
        }

        double kb = bytes / 1024.0;
        if (kb < 1024)
        {
            return kb.ToString("0.#") + " KB";
        }

        double mb = kb / 1024.0;
        if (mb < 1024)
        {
            return mb.ToString("0.##") + " MB";
        }

        return (mb / 1024.0).ToString("0.##") + " GB";
    }

    /// <summary>
    /// 倒Y：共享 content/art/maps；hotupdate 仅本端 <c>hotupdate/{platform}/</c>。
    /// 其它 hotupdate 路径（对端或旧平铺根文件）不进入本机 content_runtime。
    /// </summary>
    public static bool IsManifestPathWantedForThisPlatform(string relativePath)
    {
        var rel = relativePath.Replace('\\', '/').TrimStart('/');
        if (!rel.StartsWith("hotupdate/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var platformPrefix = "hotupdate/" + HybridClrHotLoader.PlatformFolderName + "/";
        if (rel.StartsWith(platformPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 旧平铺 hotupdate/TopDog.Hot.dll：仅无平台子目录时由 HybridClrHotLoader 回退加载；
        // 新桶已分端后不再下载平铺根，避免 Win/Android DLL 互踩。
        return false;
    }

    /// <summary>
    /// 仅把 StreamingAssets 中「运行时还不存在」的文件拷入 content_runtime；
    /// 不覆盖已有文件，不删除废弃文件。
    /// </summary>
    public static void EnsureRuntimeFilledFromStreamingMissingOnly()
    {
        var streaming = Application.streamingAssetsPath;
        if (string.IsNullOrEmpty(streaming) || !Directory.Exists(streaming))
        {
            return;
        }

        foreach (var sub in new[] { "content", "maps", "art" })
        {
            var srcRoot = Path.Combine(streaming, sub);
            if (!Directory.Exists(srcRoot))
            {
                continue;
            }

            var dstRoot = Path.Combine(ContentRuntimeRoot, sub);
            foreach (var file in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(srcRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var dest = Path.Combine(dstRoot, rel);
                if (File.Exists(dest))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(file, dest, false);
            }
        }
    }

    public static bool LocalFileMatchesSha256(string path, string? expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            return string.Equals(Sha256Hex(bytes), expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void MaterializeRuntimeFromStreamingAndOverlay(string? overlayRoot)
    {
        // 保留 API：首次/修复用。合并拷贝，不删除目标中多余文件。
        var runtime = ContentRuntimeRoot;
        Directory.CreateDirectory(runtime);
        var streaming = Application.streamingAssetsPath;
        var srcContent = Path.Combine(streaming, "content");
        var dstContent = Path.Combine(runtime, "content");
        if (Directory.Exists(srcContent))
        {
            CopyDirectory(srcContent, dstContent);
        }

        var srcMaps = Path.Combine(streaming, "maps");
        var dstMaps = Path.Combine(runtime, "maps");
        if (Directory.Exists(srcMaps))
        {
            CopyDirectory(srcMaps, dstMaps);
        }

        var srcArt = Path.Combine(streaming, "art");
        var dstArt = Path.Combine(runtime, "art");
        if (Directory.Exists(srcArt))
        {
            CopyDirectory(srcArt, dstArt);
        }

        if (!string.IsNullOrEmpty(overlayRoot) && Directory.Exists(overlayRoot))
        {
            var overlayContent = Path.Combine(overlayRoot, "content");
            if (Directory.Exists(overlayContent))
            {
                CopyDirectory(overlayContent, dstContent);
            }
            else
            {
                CopyDirectory(overlayRoot, runtime);
            }
        }
    }

    public static void WriteAppliedVersion(OnlineVersionDto remote)
    {
        Directory.CreateDirectory(OnlineUpdateRoot);
        var json = JsonUtility.ToJson(remote, true);
        File.WriteAllText(AppliedVersionPath, json, Encoding.UTF8);
    }

    public static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    public static string FormatByteSize(long bytes)
    {
        if (bytes < 0)
        {
            bytes = 0;
        }

        const double kb = 1024d;
        const double mb = kb * 1024d;
        if (bytes >= mb)
        {
            return (bytes / mb).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        }

        if (bytes >= kb)
        {
            return (bytes / kb).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " KB";
        }

        return bytes + " B";
    }

    private static string FormatSizeSuffix(long size) =>
        size > 0 ? " (" + FormatByteSize(size) + ")" : "";

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var dest = Path.Combine(destDir, rel);
            var dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.Copy(file, dest, true);
        }
    }
}
