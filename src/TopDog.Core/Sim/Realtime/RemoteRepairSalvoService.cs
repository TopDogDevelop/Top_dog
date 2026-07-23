using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §5 · docs/SHIP_FITTING.md §remote_repair · docs/COMBAT_SHIP_DETAIL_HUD.md §3.2
 * 本文件: RemoteRepairSalvoService.cs — 维修轮次队列执行
 * 【机制要点】
 * · OrderRepairTarget：仅持有 remote_repair 模块的指挥舰 +1 轮 pendingRepairRounds
 * · 无玩家指令：RemoteRepairAutoTargetingService 自动瞄准最近场域持有舰并维持维修轮次
 * · CountIncomingRepairRounds：被维修方 buff 栏总轮次（多舰叠加）
 * · repairFalloffPctPerKm 距离衰减；与最长远程维修 CD 同步递减 repairRoundCooldownSec
 * 【关联】FleetOrderService.OrderRepairTarget · SalvoProfileService · UnitOrbitHudWidget
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class RemoteRepairSalvoService
{
    public const int MaxRepairRounds = 20;

    public static string OrderRepairTarget(
        GameState state,
        BattlefieldState bf,
        string? targetUnitId,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds = null,
        ModuleRegistry? modules = null)
    {
        if (targetUnitId == null || !FleetOrderService.IsCommandTarget(bf, targetUnitId, out var target))
        {
            return "无效维修目标";
        }

        var modReg = modules ?? ModuleRegistry.LoadDefault();
        var count = 0;
        foreach (var u in FleetOrderService.ResolveCommandTargets(state, bf, selectedFriendlyUnitIds))
        {
            if (!HasRemoteRepairModule(u, modReg))
            {
                continue;
            }

            u.targetUnitId = targetUnitId;
            RemoteRepairAutoTargetingService.SuppressForPlayerOrder(u);
            u.remoteRepairAutoActive = false;
            if (u.pendingRepairRounds < MaxRepairRounds)
            {
                u.pendingRepairRounds++;
            }

            count++;
        }

        // #region agent log
        try
        {
            var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
            var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"P\",\"location\":\"RemoteRepairSalvoService.OrderRepairTarget\",\"message\":\"repair-apply\",\"data\":{"
                       + "\"target\":\"" + (targetUnitId ?? "") + "\""
                       + ",\"healers\":" + count
                       + ",\"targetName\":\"" + (target!.displayName ?? target.unitId ?? "").Replace("\"", "'") + "\""
                       + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(path, line);
        }
        catch
        {
        }
        // #endregion

        return count > 0
            ? $"已下令 {count} 艘维修 {target!.displayName}（+1 轮）"
            : "无持有维修装备的舰";
    }

    public static int CountIncomingRepairRounds(BattlefieldState bf, BattlefieldUnit target)
    {
        if (target.unitId == null)
        {
            return 0;
        }

        var total = 0;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding || u.pendingRepairRounds <= 0)
            {
                continue;
            }

            if (target.unitId.Equals(u.targetUnitId, StringComparison.Ordinal)
                && u.side == target.side)
            {
                total += u.pendingRepairRounds;
            }
        }

        return total;
    }

    public static bool HasRemoteRepairModule(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod == null)
            {
                continue;
            }

            if (!"remote_repair".Equals(mod.moduleSubtype, StringComparison.Ordinal))
            {
                continue;
            }

            if (!CombatModuleEnableService.IsSlotEnabled(unit, kv.Key))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public static void Tick(
        GameState state,
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec)
    {
        foreach (var healer in bf.units)
        {
            if (healer.IsDestroyed() || healer.isBuilding || healer.pendingRepairRounds <= 0)
            {
                continue;
            }

            if (!HasRemoteRepairModule(healer, modules))
            {
                healer.pendingRepairRounds = 0;
                continue;
            }

            healer.repairRoundCooldownSec -= dtSec;
            if (healer.repairRoundCooldownSec > 0f)
            {
                continue;
            }

            var target = ResolveRepairTarget(bf, healer);
            if (target == null)
            {
                continue;
            }

            var repairMod = FindBestRepairModule(healer, modules, target, ships);
            if (repairMod == null)
            {
                continue;
            }

            var distM = FieldAuraService.DistanceM(healer, target);
            var rangeM = repairMod.repairRangeM > 0f ? repairMod.repairRangeM : healer.attackRangeM;
            if (distM > rangeM)
            {
                continue;
            }

            var amount = ComputeRepairAmount(healer, target, repairMod, distM, rangeM, ships);
            if (amount <= 0f)
            {
                continue;
            }

            ApplyRepair(bf, healer, target, repairMod, amount);
            healer.pendingRepairRounds--;
            if (healer.pendingRepairRounds <= 0
                && healer.targetUnitId != null
                && healer.targetUnitId.Equals(target.unitId, StringComparison.Ordinal))
            {
                healer.targetUnitId = null;
            }

            healer.repairRoundCooldownSec = repairMod.repairCycleSec > 0.01f
                ? repairMod.repairCycleSec
                : SalvoProfileService.DefaultFireCycleSec;
            CombatTelemetryLog.LogRepairRound(healer.unitId!, target.unitId!, amount, healer.pendingRepairRounds);
        }
    }

    public static void ApplyRepair(
        BattlefieldState bf,
        BattlefieldUnit healer,
        BattlefieldUnit target,
        ModuleDef mod,
        float amount)
    {
        amount = CombatMarkService.ScaleOutgoingRepair(target, amount);
        var layer = mod.repairLayer ?? "shield";
        if ("armor".Equals(layer, StringComparison.OrdinalIgnoreCase))
        {
            var before = target.armorHp;
            target.armorHp = Math.Min(target.armorMax, target.armorHp + amount);
            var delta = target.armorHp - before;
            if (delta > 0f)
            {
                QueueHealDelta(bf, target, 0f, delta, 0f);
            }
        }
        else
        {
            var before = target.shieldHp;
            target.shieldHp = Math.Min(target.shieldMax, target.shieldHp + amount);
            var delta = target.shieldHp - before;
            if (delta > 0f)
            {
                QueueHealDelta(bf, target, delta, 0f, 0f);
            }
        }
    }

    public static float ComputeRepairAmount(
        BattlefieldUnit healer,
        BattlefieldUnit target,
        ModuleDef mod,
        float distM,
        float rangeM,
        ShipRegistry ships)
    {
        var baseAmount = mod.repairPerSalvo > 0f
            ? mod.repairPerSalvo
            : Math.Abs(mod.damagePerTick);

        var hull = healer.hullId != null ? ships.FindHull(healer.hullId) : null;
        if (hull?.hullLargeRemoteRepairBonusPct > 0f
            && "LARGE".Equals(mod.moduleSize, StringComparison.OrdinalIgnoreCase))
        {
            baseAmount *= 1f + hull.hullLargeRemoteRepairBonusPct / 100f;
        }

        if (distM <= rangeM)
        {
            return baseAmount;
        }

        var falloffPct = mod.repairFalloffPctPerKm;
        if (falloffPct <= 0f)
        {
            return 0f;
        }

        var overKm = (distM - rangeM) / 1000f;
        var mult = 1f - overKm * (falloffPct / 100f);
        return Math.Max(0f, baseAmount * mult);
    }

    private static BattlefieldUnit? ResolveRepairTarget(BattlefieldState bf, BattlefieldUnit healer)
    {
        if (healer.targetUnitId == null)
        {
            return null;
        }

        var target = BattlefieldSystem.FindUnit(bf, healer.targetUnitId);
        if (target == null || target.IsDestroyed() || target.side != healer.side)
        {
            return null;
        }

        return target;
    }

    private static ModuleDef? FindBestRepairModule(
        BattlefieldUnit healer,
        ModuleRegistry modules,
        BattlefieldUnit target,
        ShipRegistry ships)
    {
        ModuleDef? best = null;
        var bestAmount = 0f;
        foreach (var kv in healer.fittedModules)
        {
            if (!CombatModuleEnableService.IsSlotEnabled(healer, kv.Key))
            {
                continue;
            }

            var mod = modules.Resolve(kv.Value);
            if (mod == null
                || !"remote_repair".Equals(mod.moduleSubtype, StringComparison.Ordinal))
            {
                continue;
            }

            var rangeM = mod.repairRangeM > 0f ? mod.repairRangeM : healer.attackRangeM;
            var distM = FieldAuraService.DistanceM(healer, target);
            var amount = ComputeRepairAmount(healer, target, mod, distM, rangeM, ships);
            if (amount > bestAmount)
            {
                bestAmount = amount;
                best = mod;
            }
        }

        return best;
    }

    private static void QueueHealDelta(
        BattlefieldState bf,
        BattlefieldUnit target,
        float shieldDelta,
        float armorDelta,
        float structureDelta) =>
        CombatHpDeltaQueue.Enqueue(bf, target, shieldDelta, armorDelta, structureDelta, isHeal: true);
}
