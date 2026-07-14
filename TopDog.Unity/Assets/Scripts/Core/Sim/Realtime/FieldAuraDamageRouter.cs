using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §2.4 · §3.3 · §1.6
 * 本文件: FieldAuraDamageRouter.cs — 场域伤害代承路由
 * 【关联】BattlefieldSystem.ApplyDamage · DamageMitigationService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class FieldAuraDamageRouter
{
    public static CombatDamageContext Route(
        BattlefieldState? bf,
        BattlefieldUnit target,
        float dmg,
        BattlefieldUnit? attacker,
        ModuleRegistry modules,
        bool structureOnly = false)
    {
        var ctx = new CombatDamageContext
        {
            battlefield = bf,
            target = target,
            attacker = attacker,
            rawDamage = dmg,
            structureOnly = structureOnly,
        };

        if (dmg <= 0f || target.isBuilding || bf == null)
        {
            ctx.shieldDamage = dmg;
            return ctx;
        }

        if (structureOnly)
        {
            ctx.structureDamage = dmg;
            return ctx;
        }

        var remaining = dmg;
        var attackerInsideShield = false;

        if (target.shieldFieldHostUnitId != null)
        {
            var shieldHost = BattlefieldSystem.FindUnit(bf, target.shieldFieldHostUnitId);
            var shieldMod = shieldHost != null
                ? FieldAuraService.FindFieldModule(shieldHost, modules, "shield_fusion_field")
                : null;
            if (shieldHost != null && shieldMod != null)
            {
                var radius = FieldAuraService.ResolveFieldRadiusM(shieldHost, shieldMod, null);
                attackerInsideShield = attacker != null
                    && FieldAuraService.DistanceM(attacker, shieldHost) <= radius;
                if ((attacker == null || !attackerInsideShield) && shieldHost.shieldHp > 0f)
                {
                    ctx.shieldDamage += remaining;
                    remaining = 0f;
                }
            }
        }

        if (remaining <= 0f)
        {
            return ctx;
        }

        if (attackerInsideShield)
        {
            ctx.armorDamage = remaining;
            return ctx;
        }

        if (target.shieldHp > 0f)
        {
            var toShield = Math.Min(remaining, target.shieldHp);
            ctx.shieldDamage += toShield;
            remaining -= toShield;
        }

        if (remaining > 0f && target.armorFieldHostUnitId != null)
        {
            var armorHost = BattlefieldSystem.FindUnit(bf, target.armorFieldHostUnitId);
            var armorMod = armorHost != null
                ? FieldAuraService.FindFieldModule(armorHost, modules, "armor_link_field")
                : null;
            if (armorHost != null && armorMod != null && armorHost.armorHp > 0f)
            {
                var toArmor = Math.Min(remaining, armorHost.armorHp);
                ctx.armorDamage += toArmor;
                remaining -= toArmor;
            }
        }
        else if (remaining > 0f && target.armorHp > 0f)
        {
            var toArmor = Math.Min(remaining, target.armorHp);
            ctx.armorDamage += toArmor;
            remaining -= toArmor;
        }

        if (remaining > 0f)
        {
            ctx.structureDamage = remaining;
        }

        return ctx;
    }

    public static void ApplyRoutedDamage(
        BattlefieldState bf,
        CombatDamageContext ctx,
        ModuleRegistry modules)
    {
        var target = ctx.target;
        var shieldOnTarget = 0f;
        var armorOnTarget = 0f;

        if (target.shieldFieldHostUnitId != null)
        {
            var shieldHost = BattlefieldSystem.FindUnit(bf, target.shieldFieldHostUnitId);
            var shieldMod = shieldHost != null
                ? FieldAuraService.FindFieldModule(shieldHost, modules, "shield_fusion_field")
                : null;
            if (shieldHost != null && shieldMod != null)
            {
                var radius = FieldAuraService.ResolveFieldRadiusM(shieldHost, shieldMod, null);
                var inside = ctx.attacker != null
                    && FieldAuraService.DistanceM(ctx.attacker, shieldHost) <= radius;
                if (ctx.attacker == null || !inside)
                {
                    var hostShield = Math.Min(ctx.shieldDamage, shieldHost.shieldHp);
                    if (hostShield > 0f && target.unitId != null && shieldHost.unitId != null)
                    {
                        var bindOnly = shieldHost.shieldMax <= 0f
                            || !FieldAuraService.EligibleForShieldFusion(target, shieldHost, null);
                        CombatTelemetryLog.LogFieldRoute(
                            target.unitId,
                            shieldHost.unitId,
                            "shield",
                            hostShield,
                            bindOnly);
                    }
                    shieldHost.shieldHp -= hostShield;
                    ctx.shieldDamage -= hostShield;
                }
            }
        }

        if (ctx.shieldDamage > 0f && target.shieldHp > 0f)
        {
            shieldOnTarget = Math.Min(ctx.shieldDamage, target.shieldHp);
            target.shieldHp -= shieldOnTarget;
            ctx.shieldDamage -= shieldOnTarget;
        }

        if (ctx.armorDamage > 0f && target.armorFieldHostUnitId != null)
        {
            var armorHost = BattlefieldSystem.FindUnit(bf, target.armorFieldHostUnitId);
            if (armorHost != null && armorHost.armorHp > 0f)
            {
                var hostArmor = Math.Min(ctx.armorDamage, armorHost.armorHp);
                if (hostArmor > 0f && target.unitId != null && armorHost.unitId != null)
                {
                    CombatTelemetryLog.LogFieldRoute(
                        target.unitId,
                        armorHost.unitId,
                        "armor",
                        hostArmor,
                        bindOnly: false);
                }
                armorHost.armorHp -= hostArmor;
                ctx.armorDamage -= hostArmor;
                FieldAuraCollapse.CheckAfterDamage(bf, armorHost, modules);
            }
        }

        if (ctx.armorDamage > 0f && target.armorHp > 0f)
        {
            armorOnTarget = Math.Min(ctx.armorDamage, target.armorHp);
            target.armorHp -= armorOnTarget;
            ctx.armorDamage -= armorOnTarget;
        }

        if (ctx.structureDamage > 0f)
        {
            target.structureHp -= ctx.structureDamage;
        }

        if (target.shieldFieldHostUnitId != null)
        {
            var host = BattlefieldSystem.FindUnit(bf, target.shieldFieldHostUnitId);
            if (host != null)
            {
                FieldAuraCollapse.CheckAfterDamage(bf, host, modules);
            }
        }
    }

    public static void AfterDamageTick(
        BattlefieldState bf,
        BattlefieldUnit target,
        ModuleRegistry modules)
    {
        if (target.shieldFieldHostUnitId != null)
        {
            var host = BattlefieldSystem.FindUnit(bf, target.shieldFieldHostUnitId);
            if (host != null)
            {
                FieldAuraCollapse.CheckAfterDamage(bf, host, modules);
            }
        }

        if (target.armorFieldHostUnitId != null)
        {
            var host = BattlefieldSystem.FindUnit(bf, target.armorFieldHostUnitId);
            if (host != null)
            {
                FieldAuraCollapse.CheckAfterDamage(bf, host, modules);
            }
        }
    }
}
