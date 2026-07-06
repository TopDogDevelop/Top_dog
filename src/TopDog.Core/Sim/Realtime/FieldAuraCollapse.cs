using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §1.7 · §2.6 · §3.5
 * 本文件: FieldAuraCollapse.cs — 场域崩溃与 30s 冷却
 * 【关联】FieldAuraService · BattlefieldSystem.ApplyDamage
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class FieldAuraCollapse
{
    public static void CheckAfterDamage(
        BattlefieldState bf,
        BattlefieldUnit holder,
        ModuleRegistry modules)
    {
        if (holder.IsDestroyed())
        {
            return;
        }

        var shieldMod = FieldAuraService.FindFieldModule(holder, modules, "shield_fusion_field");
        if (shieldMod != null && holder.shieldHp <= 0f && holder.fieldAuraCollapseCooldownSec <= bf.timeSec)
        {
            Collapse(holder, bf, modules, "shield_fusion_field");
        }

        var armorMod = FieldAuraService.FindFieldModule(holder, modules, "armor_link_field");
        if (armorMod != null && holder.armorHp <= 0f && holder.fieldAuraCollapseCooldownSec <= bf.timeSec)
        {
            Collapse(holder, bf, modules, "armor_link_field");
        }
    }

    public static void Collapse(
        BattlefieldUnit holder,
        BattlefieldState bf,
        ModuleRegistry modules,
        string moduleKind)
    {
        FieldAuraService.SettleAllProteges(holder, bf, moduleKind, collapse: true);
        holder.fieldAuraEnabledAtSec = 0f;
        holder.fieldAuraDominant = false;
        holder.fieldAuraSuppressed = false;
        holder.fieldAuraCollapseCooldownSec = bf.timeSec + FieldAuraService.FieldCollapseCooldownSec;
        FieldAuraService.RefreshDominantField(bf, modules, moduleKind);
        CombatTelemetryLog.LogFieldCollapse(holder.unitId!, moduleKind);
    }
}
