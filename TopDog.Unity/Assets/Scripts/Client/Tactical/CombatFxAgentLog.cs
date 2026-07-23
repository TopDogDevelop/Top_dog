using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

/*
 * Debug-mode NDJSON logger (session 85a1e0). Non-blocking: queue + background flush.
 */
namespace TopDog.Client.Tactical;

internal static class CombatFxAgentLog
{
    private const string SessionId = "85a1e0";
    private static readonly string LogPath = Path.Combine(@"h:\", "debug-85a1e0.log");
    private static readonly ConcurrentQueue<string> Queue = new();
    private static int _flushScheduled;

    public static void Write(string hypothesisId, string location, string message, string dataJson = "{}")
    {
        try
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var line = "{\"sessionId\":\"" + SessionId
                       + "\",\"hypothesisId\":\"" + Escape(hypothesisId)
                       + "\",\"location\":\"" + Escape(location)
                       + "\",\"message\":\"" + Escape(message)
                       + "\",\"data\":" + dataJson
                       + ",\"timestamp\":" + ts
                       + "}\n";
            Queue.Enqueue(line);
            if (Interlocked.CompareExchange(ref _flushScheduled, 1, 0) == 0)
            {
                ThreadPool.QueueUserWorkItem(_ => Flush());
            }
        }
        catch
        {
            // never throw into game
        }
    }

    private static void Flush()
    {
        try
        {
            var sb = new StringBuilder(4096);
            while (Queue.TryDequeue(out var line))
            {
                sb.Append(line);
            }

            if (sb.Length > 0)
            {
                File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // drop logs only
        }
        finally
        {
            Interlocked.Exchange(ref _flushScheduled, 0);
            if (!Queue.IsEmpty && Interlocked.CompareExchange(ref _flushScheduled, 1, 0) == 0)
            {
                ThreadPool.QueueUserWorkItem(_ => Flush());
            }
        }
    }

    private static string Escape(string s) =>
        (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
