using System.Collections.Generic;
using TopDog.Content.Ships;
using TopDog.Sim.Member;

namespace TopDog.Sim.Ship;

/// <summary>Equipped vs slot count vs simultaneous-enable capacity for fitting UI.</summary>
public static class FittingEnableSummary
{
    public readonly struct Snapshot
    {
        public readonly int SlotCount;
        public readonly int EquippedCount;
        public readonly int SimultaneousEnableLimit;

        public Snapshot(int slotCount, int equippedCount, int simultaneousEnableLimit)
        {
            SlotCount = slotCount;
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
