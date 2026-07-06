using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md · 战术场域计划 Phase 3.2
 * 本文件: DamageMitigationService.cs — 反射弧格挡与磐石减伤
 * 【机制要点】
 * · reflex_shield_block：盾≥70% 每 10s 耗当前盾 1% 换 1 层；单层吸收 shieldMax 1%；0.1s 锁血
 * · bedrock_armor_flat：抗性后每次 -250
 * 【关联】BattlefieldSystem.ApplyDamage · CombatDamageContext
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class DamageMitigationService
{
    public const float ReflexTickSec = 10f;
    public const float ReflexThresholdPct = 0.70f;
    public const float DefaultBlockLockSec = 0.1f;
    public const float DefaultFlatReduction = 250f;

    public static void Tick(BattlefieldUnit unit, ModuleRegistry modules, float dtSec)
    {
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod == null || !"reflex_shield_block".Equals(mod.damageMitigationKind, StringComparison.Ordinal))
            {
                continue;
            }

            if (unit.shieldMax <= 0f || unit.shieldHp <= 0f)
            {
                continue;
            }

            if (unit.shieldHp < unit.shieldMax * ReflexThresholdPct)
            {
                continue;
            }

            unit.reflexArcTimerSec -= dtSec;
            if (unit.reflexArcTimerSec > 0f)
            {
                continue;
            }

            var pct = mod.blockShieldPctOfMax > 0f ? mod.blockShieldPctOfMax : 0.01f;
            var cost = unit.shieldHp * pct;
            unit.shieldHp = Math.Max(0f, unit.shieldHp - cost);
            unit.blockShieldLayers++;
            unit.reflexArcTimerSec = ReflexTickSec;
        }
    }

    public static CombatDamageContext ApplyMitigation(
        CombatDamageContext ctx,
        ModuleRegistry modules)
    {
        if (ctx.isRepair || ctx.skipMitigation || ctx.rawDamage <= 0f)
        {
            return ctx;
        }

        var target = ctx.target;
        if (target.blockLockSec > 0f)
        {
            ctx.shieldDamage = 0f;
            ctx.armorDamage = 0f;
            ctx.structureDamage = 0f;
            return ctx;
        }

        var remainingShield = ctx.shieldDamage;
        var remainingArmor = ctx.armorDamage;
        var remainingStructure = ctx.structureDamage;

        if (target.blockShieldLayers > 0 && remainingShield > 0f)
        {
            var blockCap = target.shieldMax * 0.01f;
            var absorbed = Math.Min(remainingShield, blockCap);
            remainingShield -= absorbed;
            if (absorbed >= blockCap - 0.01f)
            {
                target.blockShieldLayers--;
                target.blockLockSec = ResolveBlockLockSec(modules, target);
            }
            else
            {
                remainingShield = 0f;
            }
        }

        var flat = SumFlatReduction(target, modules);
        if (flat > 0f)
        {
            remainingArmor = Math.Max(0f, remainingArmor - flat);
            remainingStructure = Math.Max(0f, remainingStructure - flat);
        }

        ctx.shieldDamage = remainingShield;
        ctx.armorDamage = remainingArmor;
        ctx.structureDamage = remainingStructure;
        return ctx;
    }

    public static void TickBlockLock(BattlefieldUnit unit, float dtSec)
    {
        if (unit.blockLockSec > 0f)
        {
            unit.blockLockSec = Math.Max(0f, unit.blockLockSec - dtSec);
        }
    }

    private static float ResolveBlockLockSec(ModuleRegistry modules, BattlefieldUnit unit)
    {
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod != null
                && "reflex_shield_block".Equals(mod.damageMitigationKind, StringComparison.Ordinal)
                && mod.blockLockSec > 0f)
            {
                return mod.blockLockSec;
            }
        }

        return DefaultBlockLockSec;
    }

    private static float SumFlatReduction(BattlefieldUnit unit, ModuleRegistry modules)
    {
        var total = 0f;
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod == null || !"bedrock_armor_flat".Equals(mod.damageMitigationKind, StringComparison.Ordinal))
            {
                continue;
            }

            total += mod.flatDamageReduction > 0f ? mod.flatDamageReduction : DefaultFlatReduction;
        }

        return total;
    }
}
