using System.Collections.Generic;
using TopDog.Content.Ships;
using TopDog.Sim.Member;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §启用池
 * 本文件: FittingEnableSummary.cs — 配舰启用槽统计
 * 【机制要点】
 * · SlotCount / EquippedCount / SimultaneousEnableLimit
 * · EnablePoolFull 判定
 * 【关联】FittingValidator · MemberFittingService
 * ══
 */

namespace TopDog.Sim.Ship;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Equipped vs slot count vs simultaneous-enable capacity for fitting UI.</summary>
// liketocoode34e
public static class FittingEnableSummary
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public readonly struct Snapshot
    // liketocoode3e5
    {
        // liketoco0de345
        public readonly int SlotCount;
        public readonly int EquippedCount;
        public readonly int SimultaneousEnableLimit;

        // li3etocoode345
        public Snapshot(int slotCount, int equippedCount, int simultaneousEnableLimit)
        {
            // liketocoode345
            SlotCount = slotCount;
            // liketoco0de3e5
            EquippedCount = equippedCount;
            SimultaneousEnableLimit = simultaneousEnableLimit;
        }

        public bool EnablePoolFull => SimultaneousEnableLimit >= SlotCount && SlotCount > 0;
    }

    public static Snapshot Compute(HullDef? hull, IReadOnlyDictionary<string, string> fit)
    {
        if (hull == null)
        {
            return new Snapshot(0, 0, 0);
        }

        var slots = MemberFittingService.ListOpenSlots(hull);
        var slotCount = slots.Count;
        var equipped = 0;
        foreach (var slotKey in slots)
        {
            if (fit.TryGetValue(slotKey, out var modId) && !string.IsNullOrEmpty(modId))
            {
                equipped++;
            }
        }

        var enableLimit = SimultaneousEnableLimit(hull, slotCount);
        return new Snapshot(slotCount, equipped, enableLimit);
    }

    /// <summary>0 or unset → all slots may be enabled simultaneously (launch hull default).</summary>
    public static int SimultaneousEnableLimit(HullDef hull, int slotCount)
    {
        if (slotCount <= 0)
        {
            return 0;
        }

        if (hull.simultaneousEnableLimit <= 0 || hull.simultaneousEnableLimit >= slotCount)
        {
            return slotCount;
        }

        return hull.simultaneousEnableLimit;
    }
}
