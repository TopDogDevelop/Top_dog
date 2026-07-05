using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TopDog.AgentDiag;

/// <summary>Debug-mode NDJSON logger for agent session 73c765.</summary>
public static class AgentSessionDebugLog
{
    private const string LogPath = @"e:\debug-73c765.log";
    private const string SessionId = "73c765";

    public static void Write(string hypothesisId, string location, string message, object? data = null)
    {
        // #region agent log
        if (!ShouldEmit(hypothesisId, message, data))
        {
            return;
        }

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["sessionId"] = SessionId,
                ["hypothesisId"] = hypothesisId,
                ["location"] = location,
                ["message"] = message,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
            if (data != null)
            {
                payload["data"] = data;
            }

            File.AppendAllText(LogPath, JsonSerializer.Serialize(payload) + "\n");
        }
        catch
        {
            // ignore IO errors in play mode
        }
        // #endregion
    }

    /// <summary>开启后输出高频/辅助类日志（默认仅保留跃迁与异常路径）。</summary>
    public static bool Verbose { get; set; }

    private static bool ShouldEmit(string hypothesisId, string message, object? data)
    {
        if (Verbose)
        {
            return true;
        }

        if (hypothesisId.StartsWith("W5", StringComparison.Ordinal)
            || hypothesisId.StartsWith("W6", StringComparison.Ordinal)
            || hypothesisId.StartsWith("H4", StringComparison.Ordinal))
        {
            return true;
        }

        if (message is "landed" or "burst_timeout" or "snap_landing" or "target_bf_missing"
            or "intra_scene_arrived" or "collinear_stack")
        {
            return true;
        }

        return data != null
            && (message.Contains("fail", StringComparison.OrdinalIgnoreCase)
                || message.Contains("error", StringComparison.OrdinalIgnoreCase)
                || message.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }
}
