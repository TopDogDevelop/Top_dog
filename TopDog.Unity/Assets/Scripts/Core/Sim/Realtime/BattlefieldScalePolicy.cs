/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §1–§3 · docs/MAP_SPEC.md §4.3
 * 本文件: BattlefieldScalePolicy.cs — 舰队规模门控（全场景通用）
 * 【机制要点】
 * · 按场上战斗舰数量，不按机制测 ID
 * · 密舰队每 tick **固定上限**轮转（默认 120）；禁止按比例吃满主线程（万舰×75%会卡到秒级帧）
 * · TiDi 仍用 entityBudget/tickBudgetMs
 * 【关联】BattlefieldSystem · SceneSimBudget · TacticalIconGpuLayer · WorkerCapacityBootstrap
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>大舰队同一套 LOD / UI 门控；机制压力关只是用来快速凑数。</summary>
public static class BattlefieldScalePolicy
{
    /// <summary>与客户端批画门槛对齐。</summary>
    public const int DenseUnitThreshold = 256;

    /// <summary>密舰队：每 tick 最多处理战斗单位槽数（轮转）；与 FLEET_SCALE_10K §1 对齐。</summary>
    public const int DenseUnitProcessCap = 120;

    /// <summary>密舰队：飘字同时存活上限（全场景）。</summary>
    public const int DenseFloatingTextCap = 24;

    public static int CountCombatShips(BattlefieldState? bf)
    {
        if (bf == null)
        {
            return 0;
        }

        var n = 0;
        foreach (var u in bf.units)
        {
            if (u == null || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u)
                || u.IsBallisticMissile())
            {
                continue;
            }

            n++;
        }

        return n;
    }

    public static bool IsDense(BattlefieldState? bf) =>
        CountCombatShips(bf) >= DenseUnitThreshold;

    /// <summary>每 tick 最多处理多少个 units 槽（轮转）；稀疏=全量，密=固定 cap。</summary>
    public static int ResolveUnitProcessBudget(BattlefieldState bf)
    {
        var n = bf.units.Count;
        if (n <= 0)
        {
            return 0;
        }

        if (!IsDense(bf))
        {
            return n;
        }

        return Math.Min(n, DenseUnitProcessCap);
    }
}
