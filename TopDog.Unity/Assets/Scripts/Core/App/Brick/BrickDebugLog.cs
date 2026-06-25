using System.Collections.Generic;
using System.Text;

namespace TopDog.App.Brick;

/// <summary>Per-brick debug scaffold: ring-buffer log for sim diagnostics.</summary>
public static class BrickDebugLog
{
    public const int MaxEntries = 128;
    private static readonly Queue<string> Ring = new();
    private static readonly Dictionary<string, string> LastTickByBrick = new();

    public static bool Enabled { get; set; } = true;

    public static void Log(string brickId, string message)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(brickId))
        {
            return;
        }

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
