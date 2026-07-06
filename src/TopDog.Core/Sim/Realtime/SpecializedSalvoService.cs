using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §自开火 · docs/AI_REALTIME_PLAYER.md
 * 本文件: SpecializedSalvoService.cs — 反导弹/威慑炮/标记射线等按模块开火
 * 【机制要点】
 * · ResolveHostileTarget：autoFireEnabled=false 且无显式 targetUnitId 时不自动选敌
 * · 威慑炮/标记射线等专用武器走本服务，与 TryFireSalvo 自火门控一致
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class SpecializedSalvoService
{
    public static void Tick(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit u,
        float dtSec,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        if (u.IsDestroyed() || u.isBuilding || u.IsBallisticMissile() || u.parentUnitId != null)
        {
            return;
        }

        foreach (var kv in u.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod == null)
            {
                continue;
            }

            if (!TryGetCooldown(u, kv.Key, out var cd))
            {
                cd = 0f;
            }

            cd -= dtSec;
            SetCooldown(u, kv.Key, cd);

            if (cd > 0f)
            {
                continue;
            }

            if ("missile_only".Equals(mod.targetFilter, StringComparison.Ordinal))
            {
                if (TryFireAntiMissile(bf, state, u, mod, kv.Key, ships, modules))
                {
                    SetCooldown(u, kv.Key, mod.fireCycleSec > 0.01f ? mod.fireCycleSec : 1f);
                }

                continue;
            }

            if (mod.targetMinTonnageRank > 0 && mod.damagePerTick > 1000f)
            {
                if (TryFireDeterrence(bf, state, u, mod, kv.Key, ships, modules))
                {
                    SetCooldown(u, kv.Key, mod.fireCycleSec > 0.01f ? mod.fireCycleSec : 30f);
                }

                continue;
            }

            if (mod.markDurationSec > 0f && mod.incomingDamageMult > 1f)
            {
                if (TryFireMarkRay(bf, state, u, mod, kv.Key, incoming: true))
                {
                    SetCooldown(u, kv.Key, mod.fireCycleSec > 0.01f ? mod.fireCycleSec : 10f);
                }

                continue;
            }

            if (mod.markDurationSec > 0f && mod.outgoingRepairMult > 0f && mod.outgoingRepairMult < 1f)
            {
                if (TryFireMarkRay(bf, state, u, mod, kv.Key, incoming: false))
                {
                    SetCooldown(u, kv.Key, mod.fireCycleSec > 0.01f ? mod.fireCycleSec : 10f);
                }
            }
        }
    }

    private static bool TryFireAntiMissile(
        BattlefieldState bf,
        GameState state,
        BattlefieldUnit u,
        ModuleDef mod,
        string slotKey,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var rangeM = mod.attackRangeM > 0f ? mod.attackRangeM : 30_000f;
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var t in bf.units)
        {
            if (!"MISSILE".Equals(t.tonnageClass, StringComparison.Ordinal)
                || t.IsDestroyed()
                || t.side == u.side)
            {
                continue;
            }

            var d = FieldAuraService.DistanceM(u, t);
            if (d > rangeM || d >= bestDist)
            {
                continue;
            }

            bestDist = d;
            best = t;
        }

        if (best == null)
        {
            return false;
        }

        var dmg = mod.damagePerTick > 0f ? mod.damagePerTick : 100f;
        BattlefieldSystem.ApplyDamage(bf, best, dmg, u, state, ships, modules);
        CombatTelemetryLog.LogSalvo(u, best, dmg, mod.fireCycleSec, dmg);
        return true;
    }

    private static bool TryFireDeterrence(
        BattlefieldState bf,
        GameState state,
        BattlefieldUnit u,
        ModuleDef mod,
        string slotKey,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var target = ResolveHostileTarget(bf, state, u);
        if (target == null)
        {
            return false;
        }

        var rank = CombatPowerCalculator.TonnageRankOf(target.tonnageClass);
        if (rank < mod.targetMinTonnageRank)
        {
            return false;
        }

        var rangeM = mod.attackRangeM > 0f ? mod.attackRangeM : 200_000f;
        var dist = FieldAuraService.DistanceM(u, target);

        var dmg = mod.damagePerTick > 0f ? mod.damagePerTick : 30_000f;
        if (mod.rangeDamageFalloffPctPerKm > 0f && dist > rangeM)
        {
            var overKm = (dist - rangeM) / 1000f;
            dmg *= Math.Max(0f, 1f - overKm * (mod.rangeDamageFalloffPctPerKm / 100f));
        }

        if (dmg <= 0f)
        {
            return false;
        }

        BattlefieldSystem.ApplyMixedDamage(bf, target, dmg, u, state, ships, modules);
        CombatTelemetryLog.LogSalvo(u, target, dmg, mod.fireCycleSec, dmg);
        return true;
    }

    private static bool TryFireMarkRay(
        BattlefieldState bf,
        GameState state,
        BattlefieldUnit u,
        ModuleDef mod,
        string slotKey,
        bool incoming)
    {
        var target = ResolveHostileTarget(bf, state, u);
        if (target == null)
        {
            return false;
        }

        var rangeM = mod.attackRangeM > 0f ? mod.attackRangeM : 50_000f;
        if (FieldAuraService.DistanceM(u, target) > rangeM)
        {
            return false;
        }

        CombatMarkService.ApplyMarkFromSalvo(bf, u, target, mod);
        CombatTelemetryLog.Log("combat.mark", $"{u.unitId}→{target.unitId} incoming={incoming}");
        return true;
    }

    private static BattlefieldUnit? ResolveHostileTarget(
        BattlefieldState bf,
        GameState state,
        BattlefieldUnit u)
    {
        if (u.targetUnitId != null)
        {
            var picked = BattlefieldSystem.FindUnit(bf, u.targetUnitId);
            if (picked != null && !picked.IsDestroyed() && picked.side != u.side)
            {
                return picked;
            }
        }

        if (!state.autoFireEnabled)
        {
            return null;
        }

        var id = AutoFireTargetingService.FindNearestEnemyId(bf, u);
        return id != null ? BattlefieldSystem.FindUnit(bf, id) : null;
    }

    private static bool TryGetCooldown(BattlefieldUnit u, string slotKey, out float cd) =>
        u.moduleSalvoCooldownSec.TryGetValue(slotKey, out cd);

    private static void SetCooldown(BattlefieldUnit u, string slotKey, float cd) =>
        u.moduleSalvoCooldownSec[slotKey] = cd;
}
