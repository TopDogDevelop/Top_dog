using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BOARDING_MODULE.md §2–§5
 * 本文件: BoardingModuleService.cs — 功能槽登录模块蓄力与夺舍
 * 【机制要点】
 * · boarding_module：进 2000 m 自动启用；接战态强制 0 km 接近 + 推进器配额
 * · 攻击方继承目标 hullId + fittedModules；目标舰体销毁
 * 【关联】CombatModuleEnableService · ModuleRuntime · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class BoardingModuleService
{
    public const string ModuleKind = "boarding_module";
    public const float DefaultHoldSec = 100f;
    public const float DefaultRangeM = 2000f;

    public static void Tick(
        GameState state,
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec)
    {
        foreach (var attacker in bf.units)
        {
            if (attacker.IsDestroyed()
                || !attacker.Arrived(bf.timeSec)
                || attacker.inTacticalWarp
                || BattlefieldSceneProxyService.IsSceneProxy(attacker)
                || attacker.IsBallisticMissile())
            {
                continue;
            }

            var boardingMod = FindBoardingModule(attacker, modules);
            if (boardingMod == null)
            {
                DisableBoarding(attacker, ships, modules);
                continue;
            }

            var target = ResolveTarget(bf, attacker);
            if (target == null)
            {
                DisableBoarding(attacker, ships, modules);
                continue;
            }

            var rangeM = boardingMod.attackRangeM > 0f ? boardingMod.attackRangeM : DefaultRangeM;
            if (DistanceM(attacker, target) > rangeM)
            {
                DisableBoarding(attacker, ships, modules);
                continue;
            }

            attacker.boardingModuleEnabled = true;
            TickBoardingEngage(attacker, target, ships, modules);

            if (!string.Equals(attacker.boardingChargeTargetUnitId, target.unitId, StringComparison.Ordinal))
            {
                attacker.boardingChargeTargetUnitId = target.unitId;
                attacker.boardingChargeSec = 0f;
            }

            attacker.boardingChargeSec += dtSec;
            var holdSec = boardingMod.boardingHoldSec > 0f ? boardingMod.boardingHoldSec : DefaultHoldSec;
            if (attacker.boardingChargeSec < holdSec)
            {
                continue;
            }

            ExecuteBoarding(state, bf, attacker, target, ships, modules);
            DisableBoarding(attacker, ships, modules);
        }
    }

    public static bool IsBoardingEngaged(BattlefieldUnit unit) => unit.boardingModuleEnabled;

    public static bool IsBoardingChargeActive(BattlefieldUnit unit) =>
        unit.boardingChargeSec > 0f
        && !string.IsNullOrEmpty(unit.boardingChargeTargetUnitId);

    public static bool IsBeingBoarded(BattlefieldState bf, string? victimUnitId)
    {
        if (string.IsNullOrEmpty(victimUnitId))
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (IsBoardingChargeActive(u)
                && victimUnitId.Equals(u.boardingChargeTargetUnitId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static ModuleDef? FindBoardingModule(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod != null && ModuleKind.Equals(mod.moduleKind, StringComparison.Ordinal))
            {
                return mod;
            }
        }

        return null;
    }

    private static void TickBoardingEngage(
        BattlefieldUnit attacker,
        BattlefieldUnit target,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var hull = attacker.hullId != null ? ships.FindHull(attacker.hullId) : null;
        if (hull != null)
        {
            CombatModuleEnableService.ApplyBoardingEngageQuota(attacker, hull, modules);
        }

        attacker.aiOrder = UnitAiOrder.APPROACH;
        attacker.approachTargetUnitId = target.unitId;
        attacker.orbitTargetUnitId = null;
        attacker.commandMaintainDistM = 0f;
        attacker.throttleOn = true;
    }

    private static BattlefieldUnit? ResolveTarget(BattlefieldState bf, BattlefieldUnit attacker)
    {
        if (attacker.targetUnitId == null)
        {
            return null;
        }

        var target = FindUnit(bf, attacker.targetUnitId);
        if (target == null
            || target.IsDestroyed()
            || target.side == attacker.side
            || BattlefieldSceneProxyService.IsSceneProxy(target)
            || string.IsNullOrEmpty(target.hullId))
        {
            return null;
        }

        return target;
    }

    private static void ExecuteBoarding(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit attacker,
        BattlefieldUnit victim,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var capturedHullId = victim.hullId;
        var capturedFit = new Dictionary<string, string>(victim.fittedModules);
        if (string.IsNullOrEmpty(capturedHullId))
        {
            return;
        }

        var hull = ships.FindHull(capturedHullId);
        if (hull == null)
        {
            return;
        }

        attacker.hullId = capturedHullId;
        attacker.tonnageClass = hull.tonnageClass;
        attacker.fittedModules = capturedFit;
        attacker.disabledModuleSlots.Clear();
        attacker.boardingModuleEnabled = false;
        ModuleRuntime.ApplyToUnit(attacker, hull, modules);

        DestroyVictim(bf, victim, attacker);
        if (!string.IsNullOrEmpty(victim.memberId))
        {
            state.boardingPermadeadMemberIds.Add(victim.memberId);
        }

        attacker.combatSeizedHullThisLife = true;
        PersistBoardingToMember(state, attacker, capturedHullId, capturedFit);
        SyncRosterLines(state, attacker, victim, ships, modules);

        PushBoardingAlert(state, attacker, victim, capturedHullId);
    }

    private static void DestroyVictim(BattlefieldState bf, BattlefieldUnit victim, BattlefieldUnit attacker)
    {
        victim.shieldHp = 0f;
        victim.armorHp = 0f;
        victim.structureHp = 0f;
        victim.alive = false;
        victim.targetUnitId = null;
        victim.boardingChargeTargetUnitId = null;
        victim.boardingChargeSec = 0f;
        victim.boardingModuleEnabled = false;
        CombatDamageLedger.RecordHit(bf, attacker, victim, victim.structureMax);
    }

    private static void DisableBoarding(
        BattlefieldUnit unit,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var wasEngaged = unit.boardingModuleEnabled || unit.disabledModuleSlots.Count > 0;
        unit.boardingModuleEnabled = false;
        unit.boardingChargeTargetUnitId = null;
        unit.boardingChargeSec = 0f;
        if (!wasEngaged)
        {
            return;
        }

        var hull = unit.hullId != null ? ships.FindHull(unit.hullId) : null;
        CombatModuleEnableService.RestoreAllEnabled(unit, hull, modules);
    }

    private static void PersistBoardingToMember(
        GameState state,
        BattlefieldUnit attacker,
        string hullId,
        Dictionary<string, string> fit)
    {
        if (string.IsNullOrEmpty(attacker.memberId))
        {
            return;
        }

        var member = state.members.Find(m => attacker.memberId.Equals(m.memberId, StringComparison.Ordinal));
        if (member == null)
        {
            return;
        }

        member.equippedHullId = hullId;
        var fittings = MemberFittingService.Fittings(state, member);
        fittings.Clear();
        foreach (var kv in fit)
        {
            fittings[kv.Key] = kv.Value;
        }
    }

    private static void SyncRosterLines(
        GameState state,
        BattlefieldUnit attacker,
        BattlefieldUnit victim,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        foreach (var entry in state.combatQueue)
        {
            SyncLineForUnit(state, entry.friendlyRosterLines, attacker, ships, modules, stillAlive: true);
            SyncLineForUnit(state, entry.enemyRoster, attacker, ships, modules, stillAlive: true);
            SyncLineForUnit(state, entry.friendlyRosterLines, victim, ships, modules, stillAlive: false);
            SyncLineForUnit(state, entry.enemyRoster, victim, ships, modules, stillAlive: false);
        }
    }

    private static void SyncLineForUnit(
        GameState state,
        List<CombatRosterLine> roster,
        BattlefieldUnit unit,
        ShipRegistry ships,
        ModuleRegistry modules,
        bool stillAlive)
    {
        if (string.IsNullOrEmpty(unit.memberId))
        {
            return;
        }

        foreach (var line in roster)
        {
            if (!unit.memberId.Equals(line.memberId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!stillAlive)
            {
                line.canParticipate = false;
                line.hullId = "(已毁)";
                line.fittedModules.Clear();
                line.combatPower = 0f;
                return;
            }

            line.hullId = unit.hullId;
            line.tonnageClass = unit.tonnageClass ?? "(无)";
            line.canParticipate = true;
            line.fittedModules = new Dictionary<string, string>(unit.fittedModules);
            line.combatPower = AutoCombatValuation.RosterLineValue(state, line, ships, modules);
        }
    }

    private static BattlefieldUnit? FindUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    private static float DistanceM(BattlefieldUnit a, BattlefieldUnit b)
    {
        var dx = b.x - a.x;
        var dy = b.y - a.y;
        var dz = b.z - a.z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static void PushBoardingAlert(
        GameState state,
        BattlefieldUnit attacker,
        BattlefieldUnit victim,
        string? hullId)
    {
        var msg = $"{attacker.displayName ?? attacker.unitId} 登录夺舍 {victim.displayName ?? victim.unitId}（{hullId ?? "?"}）";
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }

        CombatTelemetryLog.Log("boarding", msg);
    }
}
