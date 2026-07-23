/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §4c 集火顺序开火
 * 本文件: FocusFireSequencer.cs — 显式集火每 tick 至多一成功伤害
 * 【机制要点】
 * · OrderFocus → Register：队列 = 下令舰 unitId 稳定序；cursor=0
 * · TryClaimVolleySlot：同目标同 tick 仅一发；cursor 指到本舰才允许出伤
 * · 跳过已毁/无对舰火力舰（主炮 salvo 或威慑等专用伤害）；目标死亡 / Cease → Clear
 * · 专用武器与主炮共享槽；非 explicitFocus 自火不经本序列
 * · 威慑拒开火仅由射程/衰减等开火门控决定，不因「无主炮」被顺序槽永久跳过
 * 【关联】FleetOrderService.OrderFocus · BattlefieldSystem.TryFireSalvo · SpecializedSalvoService
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>显式集火顺序开火（TACTICAL_WARP §4c）。</summary>
public static class FocusFireSequencer
{
    private static float _lastNoEligLogSimSec = float.NegativeInfinity;

    public static void Register(BattlefieldState bf, string focusTargetId, IEnumerable<string?> firerUnitIds)
    {
        if (string.IsNullOrEmpty(focusTargetId))
        {
            Clear(bf);
            return;
        }

        var queue = firerUnitIds
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        bf.focusFireTargetId = focusTargetId;
        bf.focusFireQueue = queue;
        bf.focusFireCursor = 0;
        bf.focusFireLastVolleySimSec = float.NegativeInfinity;
        SkipIneligible(bf);
    }

    public static void Clear(BattlefieldState bf)
    {
        bf.focusFireTargetId = null;
        bf.focusFireQueue = null;
        bf.focusFireCursor = 0;
        bf.focusFireLastVolleySimSec = float.NegativeInfinity;
    }

    /// <summary>目标被击毁时清队列并解除全体对该目标的显式集火。</summary>
    public static void OnTargetDestroyed(BattlefieldState bf, string destroyedUnitId)
    {
        if (bf.focusFireTargetId == null
            || !destroyedUnitId.Equals(bf.focusFireTargetId, StringComparison.Ordinal))
        {
            return;
        }

        foreach (var u in bf.units)
        {
            if (u.explicitFocus
                && destroyedUnitId.Equals(u.targetUnitId, StringComparison.Ordinal))
            {
                u.explicitFocus = false;
                u.targetUnitId = null;
                if (u.aiOrder == UnitAiOrder.FOCUS)
                {
                    u.aiOrder = UnitAiOrder.STOP;
                }
            }
        }

        Clear(bf);
    }

    /// <summary>
    /// 显式集火同目标：本 tick 至多一发成功伤害。非本序列管辖（无 explicitFocus / 目标不符）返回 true。
    /// </summary>
    public static bool TryClaimVolleySlot(BattlefieldState bf, BattlefieldUnit firer)
    {
        if (!firer.explicitFocus
            || string.IsNullOrEmpty(firer.targetUnitId)
            || bf.focusFireTargetId == null
            || !firer.targetUnitId.Equals(bf.focusFireTargetId, StringComparison.Ordinal)
            || bf.focusFireQueue == null
            || bf.focusFireQueue.Count == 0)
        {
            return true;
        }

        if (!(bf.timeSec > bf.focusFireLastVolleySimSec))
        {
            return false;
        }

        SkipIneligible(bf);
        if (bf.focusFireQueue == null || bf.focusFireQueue.Count == 0)
        {
            return false;
        }

        var idx = Math.Clamp(bf.focusFireCursor, 0, bf.focusFireQueue.Count - 1);
        var expected = bf.focusFireQueue[idx];
        if (firer.unitId == null || !firer.unitId.Equals(expected, StringComparison.Ordinal))
        {
            return false;
        }

        bf.focusFireLastVolleySimSec = bf.timeSec;
        bf.focusFireCursor = (idx + 1) % bf.focusFireQueue.Count;
        SkipIneligible(bf);
        return true;
    }

    private static void SkipIneligible(BattlefieldState bf)
    {
        var queue = bf.focusFireQueue;
        if (queue == null || queue.Count == 0)
        {
            return;
        }

        for (var n = 0; n < queue.Count; n++)
        {
            var idx = Math.Clamp(bf.focusFireCursor, 0, queue.Count - 1);
            var id = queue[idx];
            var u = BattlefieldSystem.FindUnit(bf, id);
            if (u != null && !u.IsDestroyed() && u.Arrived(bf.timeSec) && CanFireVolley(u))
            {
                bf.focusFireCursor = idx;
                return;
            }

            bf.focusFireCursor = (idx + 1) % queue.Count;
        }

        // #region agent log
        if (bf.timeSec >= _lastNoEligLogSimSec + 1f)
        {
            _lastNoEligLogSimSec = bf.timeSec;
            try
            {
                var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                var newElig = 0;
                foreach (var id in queue)
                {
                    var u = BattlefieldSystem.FindUnit(bf, id);
                    if (u == null || u.IsDestroyed() || !u.Arrived(bf.timeSec))
                    {
                        continue;
                    }

                    if (SalvoProfileService.CanClaimFocusVolley(u))
                    {
                        newElig++;
                    }
                }

                // 仅在确实无人可领槽时记录（避免威慑已合格时误导）
                if (newElig == 0)
                {
                    var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"K\",\"location\":\"FocusFireSequencer.SkipIneligible\",\"message\":\"no-eligible\",\"data\":{"
                               + "\"focus\":\"" + (bf.focusFireTargetId ?? "") + "\""
                               + ",\"queueN\":" + queue.Count
                               + ",\"newElig\":0"
                               + ",\"cursor\":" + bf.focusFireCursor
                               + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(path, line);
                }
            }
            catch { }
        }
        // #endregion
    }

    private static bool CanFireVolley(BattlefieldUnit u) =>
        SalvoProfileService.CanClaimFocusVolley(u);
}
