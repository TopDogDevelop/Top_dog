/*
 * Design: docs/REALTIME_COMBAT_UNIFORM.md section 4
 * RealtimeInteractionFramePolicy — interaction-first frame budget
 */

using System.Diagnostics;
using UnityEngine;

namespace TopDog.Client;

/// <summary>Keep select / orders / list scroll alive when sim or batch is heavy.</summary>
public static class RealtimeInteractionFramePolicy
{
    public const float MaxSimWallMsPerFrame = 6f;
    public const float MaxHeavyUiWallMsPerFrame = 6f;
    public const float DenseHeavyRefreshIntervalSec = 0.5f;
    public const float SparseHeavyRefreshIntervalSec = 0.12f;

    private static int _simSkipFrames;

    public static bool ShouldSkipSimTick => _simSkipFrames > 0;
    public static int PendingSimSkipFrames => _simSkipFrames;

    public static float BeginStamp() => (float)Stopwatch.GetTimestamp();

    public static float ElapsedMs(float beginStamp) =>
        (Stopwatch.GetTimestamp() - beginStamp) * 1000f / Stopwatch.Frequency;

    public static bool Exceeded(float beginStamp, float budgetMs) =>
        ElapsedMs(beginStamp) > budgetMs;

    public static void NoteSimSliceMs(float elapsedMs)
    {
        if (elapsedMs > MaxSimWallMsPerFrame)
        {
            _simSkipFrames = Mathf.Clamp(
                1 + (int)(elapsedMs / MaxSimWallMsPerFrame),
                1,
                8);
            return;
        }

        if (_simSkipFrames > 0)
        {
            _simSkipFrames--;
        }
    }

    public static void ConsumeSimSkipFrame()
    {
        if (_simSkipFrames > 0)
        {
            _simSkipFrames--;
        }
    }
}
