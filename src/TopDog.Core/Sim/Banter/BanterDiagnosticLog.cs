using System.Text;

namespace TopDog.Sim.Banter;

/// <summary>伴聊诊断环形缓冲（开发排障；不进 companionLog / 战斗 telemetry）。</summary>
public static class BanterDiagnosticLog
{
    public const int MaxEntries = 128;
    private static readonly object Gate = new();
    private static readonly List<string> Entries = new();

    public static bool Enabled { get; set; } = true;

    public static void Clear()
    {
        lock (Gate)
        {
            Entries.Clear();
        }
    }

    public static void Log(string message)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var line = $"[banter] {message}";
        lock (Gate)
        {
            Entries.Add(line);
            if (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }
    }

    public static IReadOnlyList<string> Snapshot()
    {
        lock (Gate)
        {
            return Entries.ToArray();
        }
    }

    public static string DumpRecent(int maxLines = 64)
    {
        lock (Gate)
        {
            var sb = new StringBuilder();
            var start = Math.Max(0, Entries.Count - maxLines);
            for (var i = start; i < Entries.Count; i++)
            {
                sb.AppendLine(Entries[i]);
            }

            return sb.ToString();
        }
    }
}
