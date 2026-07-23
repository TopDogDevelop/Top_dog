/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md · docs/MAP_SPEC.md §4.3
 * 本文件: WorkerCapacityBootstrap.cs — 进程级并行容量（Jobs / ThreadPool）
 * 【机制要点】
 * · JobWorkerCount = JobWorkerMaximumCount × 75%（留主线程与 OS）
 * · ThreadPool 最小工作线程同步拉到同口径，避免 CLR 线程池过冷
 * · 不替代主线程 UITK/Sim；仅解除「工作线程躺平」
 * 【关联】BattlefieldScalePolicy · GameAppBootstrap
 * ══
 */

using System;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace TopDog.Client;

public static class WorkerCapacityBootstrap
{
    public const float TargetWorkerFraction = 0.75f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        try
        {
            var maxWorkers = JobsUtility.JobWorkerMaximumCount;
            if (maxWorkers <= 0)
            {
                maxWorkers = Math.Max(1, Environment.ProcessorCount - 1);
            }

            var target = Math.Max(1, (int)Math.Floor(maxWorkers * TargetWorkerFraction));
            target = Math.Min(target, maxWorkers);
            JobsUtility.JobWorkerCount = target;

            ThreadPool.GetMinThreads(out var minWorker, out var minIo);
            ThreadPool.GetMaxThreads(out var maxWorker, out var maxIo);
            var wantWorker = Math.Max(minWorker, Math.Min(maxWorker, target));
            ThreadPool.SetMinThreads(wantWorker, minIo);

            Debug.Log(
                $"[TopDog] WorkerCapacity: JobWorkerCount={JobsUtility.JobWorkerCount}/{maxWorkers} "
                + $"(target {TargetWorkerFraction:P0}); ThreadPool min workers≥{wantWorker}");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TopDog] WorkerCapacityBootstrap skipped: " + e.Message);
        }
    }
}
