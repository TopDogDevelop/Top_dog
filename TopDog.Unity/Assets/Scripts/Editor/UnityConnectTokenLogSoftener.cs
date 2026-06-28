#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TopDog.Client.Editor;

/// <summary>
/// Unity Hub / Unity Connect / AI Toolkit 的 Token 交换失败会以 Exception 刷红。
/// 本脚本在控制台将其替换为黄色 Warning（不影响 TopDog 本地玩法）。
/// </summary>
[InitializeOnLoad]
internal static class UnityConnectTokenLogSoftener
{
    private const string PrefEnabled = "TopDog.UnityConnectLogSoftener.Enabled";

    private static bool _handling;
    private static bool _canReadConsole;
    private static bool _canDeleteConsole;
    private static readonly HashSet<string> SoftenedKeys = new(StringComparer.Ordinal);
    private static Type? _logEntriesType;
    private static Type? _logEntryType;
    private static MethodInfo? _startGettingEntries;
    private static MethodInfo? _endGettingEntries;
    private static MethodInfo? _getCount;
    private static MethodInfo? _getEntryInternal;
    private static MethodInfo? _deleteEntry;
    private static MethodInfo? _removeLogEntriesByMode;
    private static FieldInfo? _messageField;
    private static FieldInfo? _modeField;

    static UnityConnectTokenLogSoftener()
    {
        if (!EditorPrefs.GetBool(PrefEnabled, true))
        {
            return;
        }

        InitReflection();
        Application.logMessageReceived += OnLog;
        EditorApplication.delayCall += () => ScanConsoleWithRetries(0);
    }

    [MenuItem("TopDog/Editor/Scan Unity Cloud Token Console Now")]
    private static void ScanConsoleNow()
    {
        ScanAndSoftenConsole(forceRescan: true);
    }

    [MenuItem("TopDog/Editor/Soften Unity Cloud Token Errors", true)]
    private static bool ToggleSoftenerValidate() => true;

    [MenuItem("TopDog/Editor/Soften Unity Cloud Token Errors")]
    private static void ToggleSoftener()
    {
        var enabled = !EditorPrefs.GetBool(PrefEnabled, true);
        EditorPrefs.SetBool(PrefEnabled, enabled);
        Debug.Log(enabled
            ? "TopDog: 已开启 Unity Cloud Token 日志软化（红色 Exception → 黄色 Warning）。重开 Console 后生效。"
            : "TopDog: 已关闭 Unity Cloud Token 日志软化。");
        if (enabled)
        {
            InitReflection();
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.delayCall += () => ScanConsoleWithRetries(0);
        }
    }

    [MenuItem("TopDog/Editor/Disable Unity Connect Startup")]
    private static void DisableUnityConnectStartup()
    {
        UnityConnectProjectSettings.DisableStartup();
        Debug.Log("TopDog: 已关闭 ProjectSettings 中 Unity Connect / Analytics 的 InitializeOnStartup。");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (_handling || type is not (LogType.Error or LogType.Exception))
        {
            return;
        }

        if (!IsUnityCloudTokenNoise(condition, stackTrace))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (_handling)
            {
                return;
            }

            _handling = true;
            try
            {
                if (_canDeleteConsole)
                {
                    RemoveMatchingConsoleEntries(condition);
                }

                var key = condition.Split('\n')[0].Trim();
                if (SoftenedKeys.Add(key))
                {
                    EmitSoftWarning(condition);
                }
            }
            finally
            {
                _handling = false;
            }
        };
    }

    private static void ScanConsoleWithRetries(int attempt)
    {
        if (!EditorPrefs.GetBool(PrefEnabled, true))
        {
            return;
        }

        ScanAndSoftenConsole(forceRescan: attempt == 0);

        if (attempt >= 5)
        {
            return;
        }

        EditorApplication.delayCall += () => ScanConsoleWithRetries(attempt + 1);
    }

    private static void ScanAndSoftenConsole(bool forceRescan)
    {
        if (!EditorPrefs.GetBool(PrefEnabled, true))
        {
            return;
        }

        if (!_canReadConsole)
        {
            if (forceRescan)
            {
                Debug.LogWarning("TopDog: Unity Cloud 日志软化已启用，但当前 Unity 版本无法读取 Console（请手动忽略 Token 红错）。");
            }

            return;
        }

        var softened = SoftenMatchingEntries(deleteEntries: _canDeleteConsole);
        if (softened > 0)
        {
            Debug.Log("TopDog: 已将 " + softened + " 条 Unity Cloud Token 红错转为黄色 Warning。");
            TryRemoveEditorConnectErrors();
        }
        else if (forceRescan && !_canDeleteConsole)
        {
            Debug.LogWarning("TopDog: 无法逐条删除 Console 红条（Unity 6 API 限制），仍会追加黄色 Warning 提示。");
        }
    }

    private static void TryRemoveEditorConnectErrors()
    {
        if (_removeLogEntriesByMode == null)
        {
            return;
        }

        // Unity Connect / Account API 红错多由 Editor 原生层写入（常见 mode=2）。
        foreach (var mode in new[] { 2, 131072, 1 })
        {
            try
            {
                _removeLogEntriesByMode.Invoke(null, new object[] { mode });
            }
            catch
            {
                // ignored — internal API varies by Unity version
            }
        }
    }
    private static int SoftenMatchingEntries(bool deleteEntries)
    {
        if (!_canReadConsole || _logEntryType == null)
        {
            return 0;
        }

        var softened = 0;

        try
        {
            _startGettingEntries?.Invoke(null, null);
            var entry = Activator.CreateInstance(_logEntryType);
            var countObj = _getCount!.Invoke(null, null);
            var count = countObj is int c ? c : 0;

            for (var row = count - 1; row >= 0; row--)
            {
                _getEntryInternal!.Invoke(null, new[] { row, entry });
                var message = _messageField!.GetValue(entry) as string ?? "";
                if (!IsUnityCloudTokenNoise(message, message))
                {
                    continue;
                }

                var key = message.Split('\n')[0].Trim();
                if (!SoftenedKeys.Add(key))
                {
                    continue;
                }

                if (deleteEntries && _deleteEntry != null)
                {
                    _deleteEntry.Invoke(null, new object[] { row });
                }

                EmitSoftWarning(message);
                softened++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("TopDog: 扫描 Console 时失败：" + ex.Message);
        }
        finally
        {
            _endGettingEntries?.Invoke(null, null);
        }

        return softened;
    }

    private static void EmitSoftWarning(string condition)
    {
        var firstLine = condition.Split('\n')[0].Trim();
        Debug.LogWarning(
            "Unity 云端 Token/登录不可用（可忽略，不影响 TopDog 本地开发）："
            + firstLine
            + "\n如需彻底关闭：TopDog → Editor → Disable Unity Connect Startup");
    }

    private static bool IsUnityCloudTokenNoise(string condition, string stackTrace)
    {
        if (condition.Contains("Token Exchange failed", StringComparison.OrdinalIgnoreCase)
            || condition.Contains("UnityConnectWebRequestException", StringComparison.OrdinalIgnoreCase)
            || condition.Contains("Unable to access Unity services", StringComparison.OrdinalIgnoreCase)
            || condition.Contains("Account API did not become accessible", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return stackTrace.Contains("UnityEditor.Connect.", StringComparison.Ordinal)
               || stackTrace.Contains("Unity.AI.Toolkit.Accounts", StringComparison.Ordinal);
    }

    private static void RemoveMatchingConsoleEntries(string condition)
    {
        if (!_canDeleteConsole || _deleteEntry == null)
        {
            return;
        }

        try
        {
            _startGettingEntries?.Invoke(null, null);
            var entry = Activator.CreateInstance(_logEntryType!);
            var countObj = _getCount!.Invoke(null, null);
            var count = countObj is int c ? c : 0;
            var firstLine = condition.Split('\n')[0].Trim();

            for (var row = count - 1; row >= 0; row--)
            {
                _getEntryInternal!.Invoke(null, new[] { row, entry });
                var message = _messageField!.GetValue(entry) as string ?? "";
                if (!message.Contains(firstLine, StringComparison.Ordinal)
                    && !IsUnityCloudTokenNoise(message, message))
                {
                    continue;
                }

                _deleteEntry.Invoke(null, new object[] { row });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("TopDog: 无法移除 Unity Cloud 红色日志（Unity 版本 API 差异）：" + ex.Message);
        }
        finally
        {
            _endGettingEntries?.Invoke(null, null);
        }
    }

    private static void InitReflection()
    {
        try
        {
            var editorAsm = typeof(UnityEditor.Editor).Assembly;
            _logEntriesType = editorAsm.GetType("UnityEditor.LogEntries");
            _logEntryType = editorAsm.GetType("UnityEditor.LogEntry");
            if (_logEntriesType == null || _logEntryType == null)
            {
                return;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _startGettingEntries = _logEntriesType.GetMethod("StartGettingEntries", flags);
            _endGettingEntries = _logEntriesType.GetMethod("EndGettingEntries", flags);
            _getCount = _logEntriesType.GetMethod("GetCount", flags);
            _getEntryInternal = _logEntriesType.GetMethod("GetEntryInternal", flags);
            _deleteEntry = _logEntriesType.GetMethod("DeleteEntry", flags)
                           ?? _logEntriesType.GetMethod("DeleteEntryInternal", flags);
            _removeLogEntriesByMode = _logEntryType.GetMethod("RemoveLogEntriesByMode", flags)
                                      ?? _logEntriesType.GetMethod("RemoveLogEntriesByMode", flags);
            _messageField = _logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
            _modeField = _logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
            _canReadConsole = _getCount != null && _getEntryInternal != null && _messageField != null;
            _canDeleteConsole = _deleteEntry != null;
        }
        catch
        {
            _canReadConsole = false;
            _canDeleteConsole = false;
        }
    }
}

internal static class UnityConnectProjectSettings
{
    private const string AssetPath = "ProjectSettings/UnityConnectSettings.asset";

    public static void DisableStartup()
    {
        var path = System.IO.Path.Combine(Application.dataPath, "..", AssetPath);
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("TopDog: 未找到 " + AssetPath);
            return;
        }

        var text = System.IO.File.ReadAllText(path);
        text = text.Replace("m_CaptureEditorExceptions: 1", "m_CaptureEditorExceptions: 0");
        text = text.Replace("m_InitializeOnStartup: 1", "m_InitializeOnStartup: 0");
        if (!text.Contains("m_Enabled: 0"))
        {
            text = text.Replace("m_Enabled: 1", "m_Enabled: 0");
        }

        System.IO.File.WriteAllText(path, text);
        AssetDatabase.Refresh();
    }
}
#endif
