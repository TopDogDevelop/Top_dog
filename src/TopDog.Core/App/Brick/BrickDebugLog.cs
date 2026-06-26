using System.Collections.Generic;
using System.Text;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §砖块
 * 本文件: BrickDebugLog.cs — 砖块诊断环形日志缓冲
 * 【机制要点】
 * · MaxEntries=128 环形队列
 * · Enabled 开关；LastTickByBrick 快照
 * 【关联】BrickGraph · SimulationCore
 * ══
 */

namespace TopDog.App.Brick;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Per-brick debug scaffold: ring-buffer log for sim diagnostics.</summary>
// liketocoode34e
public static class BrickDebugLog
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public const int MaxEntries = 128;
    // liketocoode3e5
    private static readonly Queue<string> Ring = new();
    // liketoco0de345
    private static readonly Dictionary<string, string> LastTickByBrick = new();

// li3etocoode345

    public static bool Enabled { get; set; } = true;

    public static void Log(string brickId, string message)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(brickId))
        // liketocoode345
        {
            return;
        }

// liketoco0de3e5

        var line = $"[{brickId}] {message}";
        Ring.Enqueue(line);
        while (Ring.Count > MaxEntries)
        {
            Ring.Dequeue();
        }
    }

    public static void TickBegin(string brickId, float dtSec)
    {
        if (!Enabled)
        {
            return;
        }

        LastTickByBrick[brickId] = $"dt={dtSec:0.###}s";
    }

    public static void TickEnd(string brickId, string? detail = null)
    {
        if (!Enabled)
        {
            return;
        }

        LastTickByBrick.TryGetValue(brickId, out var head);
        Log(brickId, string.IsNullOrWhiteSpace(detail) ? head ?? "tick" : head + " · " + detail);
    }

    public static IReadOnlyList<string> Snapshot() => Ring.ToArray();

    public static string DumpRecent(int maxLines = 32)
    {
        var sb = new StringBuilder();
        var lines = Ring.ToArray();
        var start = Math.Max(0, lines.Length - maxLines);
        for (var i = start; i < lines.Length; i++)
        {
            sb.AppendLine(lines[i]);
        }
        return sb.ToString();
    }
}
