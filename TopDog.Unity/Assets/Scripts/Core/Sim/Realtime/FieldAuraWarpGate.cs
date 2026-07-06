using TopDog.Content.Modules;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §1.8
 * 本文件: FieldAuraWarpGate.cs — 跃迁前关场离场
 * 【关联】FleetOrderService.OrderWarp · TacticalWarpService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class FieldAuraWarpGate
{
    public static void PrepareHolderForWarp(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit holder,
        ModuleRegistry modules)
    {
        if (!HasActiveField(holder, modules, bf))
        {
            return;
        }

        FieldAuraService.DisableFieldAndSettleAll(holder, bf, modules);
    }

    public static void PrepareHolderForWarp(BattlefieldState bf, BattlefieldUnit holder) =>
        PrepareHolderForWarp(null!, bf, holder, ModuleRegistry.LoadDefault());

    public static bool HasActiveField(BattlefieldUnit holder, ModuleRegistry modules, BattlefieldState bf)
    {
        if (holder.fieldAuraCollapseCooldownSec > bf.timeSec)
        {
            return false;
        }

        return FieldAuraService.FindFieldModule(holder, modules, "shield_fusion_field") != null
               || FieldAuraService.FindFieldModule(holder, modules, "armor_link_field") != null;
    }

    public static string? WarpBlockedReason(
        BattlefieldUnit holder,
        ModuleRegistry modules,
        BattlefieldState bf)
    {
        if (!HasActiveField(holder, modules, bf))
        {
            return null;
        }

        if (holder.fieldAuraDominant
            && (FieldAuraService.FindFieldModule(holder, modules, "shield_fusion_field") != null
                || FieldAuraService.FindFieldModule(holder, modules, "armor_link_field") != null))
        {
            return "开庇护场时须先关场再跃迁";
        }

        return null;
    }
}
