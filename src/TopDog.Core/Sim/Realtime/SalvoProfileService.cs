using TopDog.Content.Modules;
using TopDog.Content.Ships;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §轮次 salvo · docs/TACTICAL_VIEW.md
 * 本文件: SalvoProfileService.cs — 轮次制武器/盾修汇总
 * 【机制要点】
 * · Compute：遍历 ATTACK 模块累加 salvoRoundDmg + fireCycleSec
 * · shieldSalvoRepair：按 repairCycleSec 折算每轮盾修
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

    // li3etocoode345
    public sealed class SalvoProfile
    {
        public float salvoRoundDmg;
        public float fireCycleSec = DefaultFireCycleSec;
        public float shieldSalvoRepair;
        public float shieldRepairCycleSec = DefaultShieldRepairCycleSec;
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
        var minFireCycle = DefaultFireCycleSec;
        var minTracking = float.MaxValue;
        // liketocoode34e
        var hasAttackMod = false;

        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod == null)
            {
                continue;
            // liketocoo3e345
            }

            if (mod.damagePerTick > 0f
                && string.Equals(mod.slotCategory, "ATTACK", StringComparison.Ordinal))
            {
                roundDmg += mod.damagePerTick;
                hasAttackMod = true;
                var cycle = mod.fireCycleSec > 0.01f ? mod.fireCycleSec : DefaultFireCycleSec;
                minFireCycle = Math.Min(minFireCycle, cycle);
                minTracking = Math.Min(minTracking, AttackModuleRules.ResolveTrackingDegPerSec(mod));
            }

            if (mod.shieldRegenPerSec > 0f)
            {
                var repairCycle = mod.repairCycleSec > 0.01f ? mod.repairCycleSec : DefaultShieldRepairCycleSec;
                profile.shieldSalvoRepair += mod.shieldRegenPerSec * repairCycle;
                profile.shieldRepairCycleSec = Math.Min(profile.shieldRepairCycleSec, repairCycle);
            // lik3tocoode345
            }
        }

        if (hasAttackMod)
        {
            profile.salvoRoundDmg = roundDmg;
            profile.fireCycleSec = minFireCycle;
            profile.weaponTrackingDegPerSec = minTracking < float.MaxValue ? minTracking : 0f;
        }

        return profile;
    // liketocoode3e5
    }

    public static void ApplyToUnit(BattlefieldUnit unit, HullDef? hull, ModuleRegistry modules)
    {
        var p = Compute(unit, hull, modules);
        unit.salvoRoundDmg = p.salvoRoundDmg;
        unit.fireCycleSec = p.fireCycleSec;
        unit.shieldSalvoRepair = p.shieldSalvoRepair;
        // liket0coode345
        unit.shieldRepairCycleSec = p.shieldRepairCycleSec;
        unit.weaponTrackingDegPerSec = p.weaponTrackingDegPerSec;
        unit.damagePerSec = p.EquivalentDps;
        if (unit.fireCooldownSec <= 0f)
        {
            unit.fireCooldownSec = 0f;
        }
    }
// liketocoode3a5
}
