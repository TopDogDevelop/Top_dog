using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §1.7 · §2.6 · §3.5
 * 本文件: FieldAuraCollapse.cs — 场域崩溃与冷却
 * 【数值】直接改模块 JSON 的 fieldCollapseCooldownSec（当前 30）；无独立 balance 文件
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
        var wasOn = holder.fieldAuraEnabledAtSec > 0f;
        holder.fieldAuraEnabledAtSec = 0f;
        holder.fieldAuraShieldDominant = false;
        holder.fieldAuraArmorDominant = false;
        holder.fieldAuraShieldSuppressed = false;
        holder.fieldAuraArmorSuppressed = false;
        var cooldown = ResolveCollapseCooldownSec(
            FieldAuraService.FindFittedFieldModule(holder, modules, moduleKind));
        holder.fieldAuraCollapseCooldownSec = bf.timeSec + cooldown;
        if (wasOn)
        {
            holder.fieldAuraResumeAfterCooldown = true;
        }

        FieldAuraService.RefreshDominantField(bf, modules, moduleKind);
        CombatTelemetryLog.LogFieldCollapse(holder.unitId!, moduleKind);
        // #region agent log
        try
        {
            var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
            var line = "{\"sessionId\":\"85a1e0\",\"runId\":\"post-fix\",\"hypothesisId\":\"M\",\"location\":\"FieldAuraCollapse.Collapse\",\"message\":\"collapse-cd\",\"data\":{"
                       + "\"holder\":\"" + (holder.unitId ?? "") + "\""
                       + ",\"kind\":\"" + moduleKind + "\""
                       + ",\"cooldownSec\":" + cooldown.ToString("F0")
                       + ",\"until\":" + holder.fieldAuraCollapseCooldownSec.ToString("F1")
                       + ",\"now\":" + bf.timeSec.ToString("F1")
                       + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(path, line);
        }
        catch
        {
        }
        // #endregion
    }

    /// <summary>读模块 fieldCollapseCooldownSec；缺省回退 30（与设计一致）。</summary>
    public static float ResolveCollapseCooldownSec(ModuleDef? module)
    {
        if (module != null && module.fieldCollapseCooldownSec > 0f)
        {
            return module.fieldCollapseCooldownSec;
        }

        return FieldAuraService.FieldCollapseCooldownSec;
    }
}
