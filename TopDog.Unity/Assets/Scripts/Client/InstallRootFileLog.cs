using System;
using System.IO;
using System.Text;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §诊断日志
 * 本文件: InstallRootFileLog — PC/移动端安装侧根目录全量日志（1MB 上限）
 * 【机制要点】
 * · SubsystemRegistration 最早挂接 Application.logMessageReceivedThreaded
 * · PC：exe 同目录；移动端：APK 目录只读 → persistentDataPath（应用可写根）
 * · 单文件 TopDog.log，超过 1MB 保留较新半段后继续追加
 * 【关联】GameAppBootstrap · TopDogPlayModeBootstrap
 * ══
 */

namespace TopDog.Client;

/// <summary>
/// Mirrors all Unity log callbacks into a single capped file under the install-side root.
/// </summary>
public static class InstallRootFileLog
{
    public const string FileName = "TopDog.log";
    public const long MaxBytes = 1L * 1024 * 1024;

    private static readonly object Gate = new();
    private static StreamWriter? _writer;
    private static FileStream? _stream;
    private static string? _path;
    private static bool _started;
    private static bool _writing;
    private static int _linesSinceFlush;

    /// <summary>Absolute path of the active log file, or null if not started.</summary>
    public static string? Path => _path;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        Start();
    }

    /// <summary>Idempotent; safe to call from tests or late boot.</summary>
    public static void Start()
    {
        lock (Gate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            try
            {
                var root = ResolveInstallRoot();
                Directory.CreateDirectory(root);
                _path = System.IO.Path.Combine(root, FileName);
                OpenWriter(append: File.Exists(_path));
                WriteLineUnlocked(
                    "=== TopDog file log start "
                    + DateTime.Now.ToString("o")
                    + " platform="
                    + Application.platform
                    + " root="
                    + root
                    + " ===");
                Application.logMessageReceivedThreaded += OnLogThreaded;
                Application.quitting += OnQuitting;
            }
            catch (Exception e)
            {
                _started = false;
                CloseWriterUnlocked();
                // Avoid Debug.Log here (would recurse if hook partially applied).
                try
                {
                    Console.Error.WriteLine("InstallRootFileLog failed: " + e);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    /// <summary>
    /// PC standalone: folder containing the player executable.
    /// Editor: project folder (parent of Assets).
    /// Mobile: APK/install package tree is read-only → <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public static string ResolveInstallRoot()
    {
#if UNITY_EDITOR
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
#elif UNITY_ANDROID || UNITY_IOS
        return Application.persistentDataPath;
#else
        var data = Application.dataPath;
        if (string.IsNullOrEmpty(data))
        {
            return Application.persistentDataPath;
        }

        var parent = System.IO.Path.GetDirectoryName(data);
        return string.IsNullOrEmpty(parent) ? Application.persistentDataPath : parent;
#endif
    }

    private static void OnLogThreaded(string condition, string stackTrace, LogType type)
    {
        if (_writing)
        {
            return;
        }

        lock (Gate)
        {
            if (_writer == null)
            {
                return;
            }

            _writing = true;
            try
            {
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                WriteLineUnlocked(stamp + " [" + type + "] " + condition);
                _linesSinceFlush++;
                if (type is LogType.Error or LogType.Exception or LogType.Assert)
                {
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        WriteLineUnlocked(stackTrace.TrimEnd());
                    }

                    _writer.Flush();
                    _linesSinceFlush = 0;
                }
                else if (_linesSinceFlush >= 48)
                {
                    _writer.Flush();
                    _linesSinceFlush = 0;
                }

                EnforceSizeLimitUnlocked();
            }
            catch
            {
                // Swallow I/O errors so logging never crashes the player.
            }
            finally
            {
                _writing = false;
            }
        }
    }

    private static void OnQuitting()
    {
        lock (Gate)
        {
            try
            {
                WriteLineUnlocked("=== TopDog file log end " + DateTime.Now.ToString("o") + " ===");
                _writer?.Flush();
            }
            catch
            {
                // ignored
            }

            Application.logMessageReceivedThreaded -= OnLogThreaded;
            Application.quitting -= OnQuitting;
            CloseWriterUnlocked();
            _started = false;
        }
    }

    private static void WriteLineUnlocked(string line)
    {
        if (_writer == null)
        {
            return;
        }

        _writer.WriteLine(line);
    }

    private static void OpenWriter(bool append)
    {
        CloseWriterUnlocked();
        _stream = new FileStream(
            _path!,
            append ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite);
        _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            // 禁止逐行 Flush：游戏进程不得阻塞等日志 I/O
            AutoFlush = false,
        };
    }

    private static void CloseWriterUnlocked()
    {
        try
        {
            _writer?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _stream?.Dispose();
        }
        catch
        {
            // ignored
        }

        _writer = null;
        _stream = null;
    }

    /// <summary>When over MaxBytes, keep the newer half so recent crashes stay visible.</summary>
    private static void EnforceSizeLimitUnlocked()
    {
        if (_stream == null || _path == null || _stream.Length <= MaxBytes)
        {
            return;
        }

        _writer!.Flush();
        CloseWriterUnlocked();

        try
        {
            var bytes = File.ReadAllBytes(_path);
            var keep = (int)(MaxBytes / 2);
            var start = Math.Max(0, bytes.Length - keep);
            while (start < bytes.Length && bytes[start] != (byte)'\n')
            {
                start++;
            }

            if (start < bytes.Length)
            {
                start++;
            }

            var keptLen = bytes.Length - start;
            var header = Encoding.UTF8.GetBytes(
                "=== TopDog log truncated to last ~"
                + keep
                + " bytes @ "
                + DateTime.Now.ToString("o")
                + " ===\n");
            var rebuilt = new byte[header.Length + keptLen];
            Buffer.BlockCopy(header, 0, rebuilt, 0, header.Length);
            Buffer.BlockCopy(bytes, start, rebuilt, header.Length, keptLen);
            File.WriteAllBytes(_path, rebuilt);
        }
        catch
        {
            try
            {
                File.WriteAllText(
                    _path,
                    "=== TopDog log reset after size limit @ " + DateTime.Now.ToString("o") + " ===\n",
                    new UTF8Encoding(false));
            }
            catch
            {
                // ignored
            }
        }

        OpenWriter(append: true);
    }
}
