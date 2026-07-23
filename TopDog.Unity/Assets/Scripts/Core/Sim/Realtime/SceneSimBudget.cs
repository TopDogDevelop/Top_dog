/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §1 · docs/MAP_SPEC.md §4.3 · docs/REALTIME_COMBAT_UNIFORM.md §4
 * 本文件: SceneSimBudget.cs — 场景 tick 预算 → timeDilation；中途可问询是否已超时
 * ══
 */

using System.Diagnostics;

namespace TopDog.Sim.Realtime;

public static class SceneSimBudget
{
    /// <summary>
    /// 客户端硬墙钟（ms）。即使 JSON 把 tickBudgetMs 配大，也不许单 tick 吃掉交互帧。
    /// </summary>
    public const float HardClientWallMs = 6f;

    /// <summary>sim.tidi 落盘最小间隔（墙钟秒）；禁止每 tick 同步刷盘。</summary>
    public const float TidiLogMinIntervalSec = 1f;

    private static float _nextTidiLogUnscaled = -1f;

    public static float Begin() => (float)Stopwatch.GetTimestamp();

    public static float ElapsedMs(float beginTimestamp) =>
        (Stopwatch.GetTimestamp() - beginTimestamp) * 1000f / Stopwatch.Frequency;

    public static float EffectiveBudgetMs(BattlefieldState? bf)
    {
        if (bf == null || bf.tickBudgetMs <= 0f)
        {
            return HardClientWallMs;
        }

        return Math.Min(bf.tickBudgetMs, HardClientWallMs);
    }

    /// <summary>本 tick 墙钟是否已超过有效预算（战场 tickBudgetMs ∩ 客户端硬墙）。</summary>
    public static bool IsOverBudget(BattlefieldState? bf, float beginTimestamp)
    {
        if (bf == null)
        {
            return false;
        }

        return ElapsedMs(beginTimestamp) > EffectiveBudgetMs(bf);
    }

    public static void EndAndApply(BattlefieldState bf, float beginTimestamp)
    {
        if (bf == null)
        {
            return;
        }

        var elapsedMs = ElapsedMs(beginTimestamp);
        var budgetMs = EffectiveBudgetMs(bf);
        var entityOver = bf.units.Count > bf.entityBudget;
        var tickOver = elapsedMs > budgetMs;
        if (!entityOver && !tickOver)
        {
            bf.timeDilation = 1f;
            return;
        }

        var ratio = budgetMs / Math.Max(elapsedMs, 0.001f);
        if (entityOver)
        {
            ratio = Math.Min(ratio, (float)bf.entityBudget / Math.Max(bf.units.Count, 1));
        }

        bf.timeDilation = Math.Clamp(ratio, bf.minTimeDilation, 1f);

        // 节流：热路径不得每 tick 写文件（AutoFlush 曾把墙钟 tick 拉到秒级）
        var now = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        if (now >= _nextTidiLogUnscaled)
        {
            _nextTidiLogUnscaled = now + TidiLogMinIntervalSec;
            CombatTelemetryLog.Log(
                "sim.tidi",
                $"bf={bf.battlefieldId} dil={bf.timeDilation:0.###} tickMs={elapsedMs:0.0} entities={bf.units.Count}");
        }
    }
}
