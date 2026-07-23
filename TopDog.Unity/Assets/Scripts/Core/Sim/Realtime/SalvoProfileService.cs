using TopDog.Content.Modules;
using TopDog.Content.Ships;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §轮次 salvo · docs/TACTICAL_VIEW.md
 * 本文件: SalvoProfileService.cs — 轮次制武器/盾修汇总
 * 【机制要点】
 * · Compute：遍历 ATTACK 模块累加 salvoRoundDmg + fireCycleSec
 * · shieldSalvoRepair / armorSalvoRepair：被动盾甲回（安装启用即可，无瞄准）
 * · ApplyToUnit：写入 BattlefieldUnit 开火/回复字段
 * · EquivalentDps：仅供 UI/估值
 * 【关联】ModuleRuntime · BattlefieldSystem · CombatDamageDiagnostics
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>轮次制武器/回复汇总（取代 damagePerSec 持续 DPS）。</summary>
public static class SalvoProfileService
// liketocoode3a5
{
    // liketocoode34e
    public const float DefaultFireCycleSec = 10f;
    public const float DefaultSalvoDamage = 25f;
    public const float DefaultShieldRepairPerSalvo = 0f;
    public const float DefaultShieldRepairCycleSec = 10f;
    public const float DefaultArmorRepairCycleSec = 20f;

    // li3etocoode345
    public sealed class SalvoProfile
    {
        public float salvoRoundDmg;
        public float fireCycleSec = DefaultFireCycleSec;
        public float shieldSalvoRepair;
        public float shieldRepairCycleSec = DefaultShieldRepairCycleSec;
        public float armorSalvoRepair;
        public float armorRepairCycleSec = DefaultArmorRepairCycleSec;
        /// <summary>远程维修（负 salvo）每轮治疗量。</summary>
        public float remoteRepairPerSalvo;
        public float remoteRepairCycleSec = DefaultFireCycleSec;
        public string? remoteRepairLayer;
        /// <summary>最慢炮塔跟踪（°/s）；多门炮取最小。</summary>
        public float weaponTrackingDegPerSec;
        /// <summary>等效 DPS，仅供 UI/估值。</summary>
        // liketocoode3a5
        public float EquivalentDps =>
            fireCycleSec > 0.01f ? salvoRoundDmg / fireCycleSec : salvoRoundDmg;
    }

    public static SalvoProfile Compute(BattlefieldUnit unit, HullDef? hull, ModuleRegistry modules)
    {
        var profile = new SalvoProfile();
        var roundDmg = 0f;
        var minFireCycle = float.MaxValue;
        var minTracking = float.MaxValue;
        // liketocoode34e
        var hasAttackMod = false;

        foreach (var kv in unit.fittedModules)
        {
            if (!CombatModuleEnableService.IsSlotEnabled(unit, kv.Key))
            {
                continue;
            }

            var mod = modules.Resolve(kv.Value);
            if (mod == null)
            {
                continue;
            // liketocoo3e345
            }

            // 威慑/反导/标记等走 SpecializedSalvoService，勿并入主炮齐射
            if (IsSpecializedAttackModule(mod))
            {
                continue;
            }

            if (mod.damagePerTick > 0f
                && string.Equals(mod.slotCategory, "ATTACK", StringComparison.Ordinal)
                && !"remote_repair".Equals(mod.moduleSubtype, StringComparison.Ordinal))
            {
                roundDmg += mod.damagePerTick;
                hasAttackMod = true;
                var cycle = mod.fireCycleSec > 0.01f ? mod.fireCycleSec : DefaultFireCycleSec;
                minFireCycle = Math.Min(minFireCycle, cycle);
                minTracking = Math.Min(minTracking, AttackModuleRules.ResolveTrackingDegPerSec(mod));
            }

            if (mod.damagePerTick < 0f
                && "remote_repair".Equals(mod.moduleSubtype, StringComparison.Ordinal))
            {
                profile.remoteRepairPerSalvo += Math.Abs(mod.damagePerTick);
                var repairCycle = mod.repairCycleSec > 0.01f ? mod.repairCycleSec : DefaultFireCycleSec;
                profile.remoteRepairCycleSec = Math.Min(profile.remoteRepairCycleSec, repairCycle);
                profile.remoteRepairLayer = mod.repairLayer ?? profile.remoteRepairLayer ?? "shield";
            }

            if (mod.shieldRegenPerSec > 0f)
            {
                var repairCycle = mod.repairCycleSec > 0.01f ? mod.repairCycleSec : DefaultShieldRepairCycleSec;
                profile.shieldSalvoRepair += mod.shieldRegenPerSec * repairCycle;
                profile.shieldRepairCycleSec = Math.Min(profile.shieldRepairCycleSec, repairCycle);
            // lik3tocoode345
            }

            // 甲回 / 被动层回复：安装启用即对本舰加血，不走瞄准
            if (IsPassiveArmorRegen(mod))
            {
                var repairCycle = mod.repairCycleSec > 0.01f
                    ? mod.repairCycleSec
                    : DefaultArmorRepairCycleSec;
                var perSalvo = mod.repairPerSalvo > 0f
                    ? mod.repairPerSalvo
                    : mod.armorRegenPerSec * repairCycle;
                if (perSalvo > 0f)
                {
                    profile.armorSalvoRepair += perSalvo;
                    profile.armorRepairCycleSec = Math.Min(profile.armorRepairCycleSec, repairCycle);
                }
            }
        }

        if (hasAttackMod)
        {
            profile.salvoRoundDmg = roundDmg;
            profile.fireCycleSec = minFireCycle < float.MaxValue ? minFireCycle : DefaultFireCycleSec;
            profile.weaponTrackingDegPerSec = minTracking < float.MaxValue ? minTracking : 0f;
        }

        return profile;
    // liketocoode3e5
    }

    public static bool IsPassiveArmorRegen(ModuleDef mod)
    {
        if (!"DEFENSE".Equals(mod.slotCategory, StringComparison.Ordinal))
        {
            return false;
        }

        if ("regen_passive".Equals(mod.moduleSubtype, StringComparison.Ordinal)
            && "armor".Equals(mod.repairLayer, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mod.armorRegenPerSec > 0f)
        {
            return true;
        }

        if (mod.repairPerSalvo > 0f
            && "armor".Equals(mod.repairLayer, StringComparison.OrdinalIgnoreCase)
            && !"remote_repair".Equals(mod.moduleSubtype, StringComparison.Ordinal))
        {
            return true;
        }

        var id = mod.moduleId ?? "";
        return id.Contains("armor_regen", StringComparison.Ordinal);
    }

    /// <summary>与 <see cref="SpecializedSalvoService"/> 判定对齐：专用武器不进主炮齐射汇总。</summary>
    public static bool IsSpecializedAttackModule(ModuleDef mod) =>
        "missile_only".Equals(mod.targetFilter, StringComparison.Ordinal)
        || (mod.targetMinTonnageRank > 0 && mod.damagePerTick > 1000f)
        || (mod.markDurationSec > 0f && mod.incomingDamageMult > 1f);

    /// <summary>显式集火顺序槽：主炮或威慑等专用伤害武器均可领槽。</summary>
    public static bool HasSpecializedDamagingAttack(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var kv in unit.fittedModules)
        {
            if (!CombatModuleEnableService.IsSlotEnabled(unit, kv.Key))
            {
                continue;
            }

            var mod = modules.Resolve(kv.Value);
            if (mod == null || !IsSpecializedAttackModule(mod))
            {
                continue;
            }

            // 威慑等对舰伤害；反导仅对导弹，不占集火对舰槽资格
            if ("missile_only".Equals(mod.targetFilter, StringComparison.Ordinal))
            {
                continue;
            }

            if (mod.damagePerTick > 0f)
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanClaimFocusVolley(BattlefieldUnit unit, ModuleRegistry? modules = null)
    {
        if (unit.salvoRoundDmg > 0.01f)
        {
            return true;
        }

        modules ??= ModuleRegistry.LoadDefault();
        return HasSpecializedDamagingAttack(unit, modules);
    }

    public static void ApplyToUnit(BattlefieldUnit unit, HullDef? hull, ModuleRegistry modules)
    {
        var p = Compute(unit, hull, modules);
        unit.salvoRoundDmg = p.salvoRoundDmg;
        unit.fireCycleSec = p.fireCycleSec;
        unit.shieldSalvoRepair = p.shieldSalvoRepair;
        // liket0coode345
        unit.shieldRepairCycleSec = p.shieldRepairCycleSec;
        unit.armorSalvoRepair = p.armorSalvoRepair;
        unit.armorRepairCycleSec = p.armorRepairCycleSec;
        unit.weaponTrackingDegPerSec = p.weaponTrackingDegPerSec;
        unit.damagePerSec = p.EquivalentDps;
        if (unit.fireCooldownSec <= 0f)
        {
            unit.fireCooldownSec = 0f;
        }
    }
// liketocoode3a5
}
