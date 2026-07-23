using TopDog.Content.Modules;
using TopDog.Content.Ships;

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
        ShipRegistry? ships = null,
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

        var shipsReg = ships ?? ShipRegistry.LoadDefault();
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
                var hull = shipsReg.FindHull(shieldHost.hullId);
                var radius = FieldAuraService.ResolveFieldRadiusM(shieldHost, shieldMod, hull);
                // 边界外：严格 <，避免 50km 出生贴边被当成球内绕过盾融
                attackerInsideShield = attacker != null
                    && FieldAuraService.DistanceM(attacker, shieldHost) < radius;
                if ((attacker == null || !attackerInsideShield) && shieldHost.shieldHp > 0f)
                {
                    ctx.shieldDamage += remaining;
                    remaining = 0f;
                }

                // #region agent log
                try
                {
                    var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                    var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"G\",\"location\":\"FieldAuraDamageRouter.Route\",\"message\":\"shield-radius\",\"data\":{"
                               + "\"host\":\"" + (shieldHost.unitId ?? "") + "\""
                               + ",\"radiusM\":" + radius.ToString("F0")
                               + ",\"hullMult\":" + (hull?.hullShieldFusionRadiusMult ?? 1f).ToString("F2")
                               + ",\"atkDist\":" + (attacker != null
                                   ? FieldAuraService.DistanceM(attacker, shieldHost).ToString("F0")
                                   : "-1")
                               + ",\"inside\":" + (attackerInsideShield ? "true" : "false")
                               + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(path, line);
                }
                catch
                {
                }
                // #endregion
            }
        }

        if (remaining <= 0f)
        {
            LogRoute(target, ctx, attackerInsideShield, bf);
            return ctx;
        }

        if (attackerInsideShield)
        {
            if (remaining > 0f && target.armorFieldHostUnitId != null)
            {
                var armorHostInside = BattlefieldSystem.FindUnit(bf, target.armorFieldHostUnitId);
                var armorModInside = armorHostInside != null
                    ? FieldAuraService.FindFieldModule(armorHostInside, modules, "armor_link_field")
                    : null;
                if (armorHostInside != null && armorModInside != null && armorHostInside.armorHp > 0f)
                {
                    ctx.armorDamage = remaining;
                    LogRoute(target, ctx, attackerInsideShield, bf);
                    return ctx;
                }
            }

            ctx.armorDamage = remaining;
            LogRoute(target, ctx, attackerInsideShield, bf);
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
                ctx.armorDamage += remaining;
                remaining = 0f;
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

        LogRoute(target, ctx, attackerInsideShield, bf);
        return ctx;
    }

    public static void ApplyRoutedDamage(
        BattlefieldState bf,
        CombatDamageContext ctx,
        ModuleRegistry modules,
        ShipRegistry? ships = null)
    {
        var target = ctx.target;
        var shipsReg = ships ?? ShipRegistry.LoadDefault();
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
                var hull = shipsReg.FindHull(shieldHost.hullId);
                var radius = FieldAuraService.ResolveFieldRadiusM(shieldHost, shieldMod, hull);
                var inside = ctx.attacker != null
                    && FieldAuraService.DistanceM(ctx.attacker, shieldHost) < radius;
                if (ctx.attacker == null || !inside)
                {
                    var hostShield = Math.Min(ctx.shieldDamage, shieldHost.shieldHp);
                    if (hostShield > 0f && target.unitId != null && shieldHost.unitId != null)
                    {
                        var bindOnly = shieldHost.shieldMax <= 0f
                            || !FieldAuraService.EligibleForShieldFusion(target, shieldHost, hull);
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
                var pool = ctx.armorDamage + ctx.structureDamage;
                ctx.structureDamage = 0f;
                var hostArmor = Math.Min(pool, armorHost.armorHp);
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
                pool -= hostArmor;
                ctx.armorDamage = 0f;
                if (pool > 0f)
                {
                    ctx.structureDamage = pool;
                }

                FieldAuraCollapse.CheckAfterDamage(bf, armorHost, modules);
            }
        }

        if (ctx.armorDamage > 0f && target.armorHp > 0f)
        {
            armorOnTarget = Math.Min(ctx.armorDamage, target.armorHp);
            target.armorHp -= armorOnTarget;
            ctx.armorDamage -= armorOnTarget;
        }

        if (ctx.armorDamage > 0f)
        {
            ctx.structureDamage += ctx.armorDamage;
            ctx.armorDamage = 0f;
        }

        if (ctx.structureDamage > 0f)
        {
            target.structureHp -= ctx.structureDamage;
        }

        // #region agent log
        try
        {
            if (target.armorFieldHostUnitId != null)
            {
                var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                var armorHost = BattlefieldSystem.FindUnit(bf, target.armorFieldHostUnitId);
                var line = "{\"sessionId\":\"85a1e0\",\"runId\":\"post-fix\",\"hypothesisId\":\"G\",\"location\":\"FieldAuraDamageRouter.ApplyRoutedDamage\",\"message\":\"apply\",\"data\":{"
                           + "\"target\":\"" + (target.unitId ?? "") + "\""
                           + ",\"armorOnTarget\":" + armorOnTarget.ToString("F1")
                           + ",\"structHit\":" + ctx.structureDamage.ToString("F1")
                           + ",\"tStructAfter\":" + target.structureHp.ToString("F1")
                           + ",\"alive\":" + (!target.IsDestroyed() ? "true" : "false")
                           + ",\"hostArmorAfter\":" + (armorHost?.armorHp ?? -1f).ToString("F1")
                           + ",\"hostArmorZero\":" + ((armorHost == null || armorHost.armorHp <= 0f) ? "true" : "false")
                           + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                System.IO.File.AppendAllText(path, line);
            }
        }
        catch
        {
        }
        // #endregion

        if (target.shieldFieldHostUnitId != null)
        {
            var host = BattlefieldSystem.FindUnit(bf, target.shieldFieldHostUnitId);
            if (host != null)
            {
                FieldAuraCollapse.CheckAfterDamage(bf, host, modules);
            }
        }

        _ = shieldOnTarget;
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

    private static void LogRoute(
        BattlefieldUnit target,
        CombatDamageContext ctx,
        bool attackerInsideShield,
        BattlefieldState? bf)
    {
        // #region agent log
        try
        {
            if (target.armorFieldHostUnitId == null && target.shieldFieldHostUnitId == null)
            {
                return;
            }

            var armorHost = target.armorFieldHostUnitId != null && bf != null
                ? BattlefieldSystem.FindUnit(bf, target.armorFieldHostUnitId)
                : null;
            var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
            var line = "{\"sessionId\":\"85a1e0\",\"runId\":\"post-fix\",\"hypothesisId\":\"G\",\"location\":\"FieldAuraDamageRouter.Route\",\"message\":\"route\",\"data\":{"
                       + "\"target\":\"" + (target.unitId ?? "") + "\""
                       + ",\"armorHostId\":\"" + (target.armorFieldHostUnitId ?? "") + "\""
                       + ",\"shieldHostId\":\"" + (target.shieldFieldHostUnitId ?? "") + "\""
                       + ",\"hostArmor\":" + (armorHost?.armorHp ?? -1f).ToString("F1")
                       + ",\"hostArmorMax\":" + (armorHost?.armorMax ?? -1f).ToString("F1")
                       + ",\"raw\":" + ctx.rawDamage.ToString("F1")
                       + ",\"toShield\":" + ctx.shieldDamage.ToString("F1")
                       + ",\"toArmor\":" + ctx.armorDamage.ToString("F1")
                       + ",\"toStruct\":" + ctx.structureDamage.ToString("F1")
                       + ",\"tShield\":" + target.shieldHp.ToString("F1")
                       + ",\"tArmor\":" + target.armorHp.ToString("F1")
                       + ",\"tStruct\":" + target.structureHp.ToString("F1")
                       + ",\"atkInsideShield\":" + (attackerInsideShield ? "true" : "false")
                       + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(path, line);
        }
        catch
        {
        }
        // #endregion
    }
}
