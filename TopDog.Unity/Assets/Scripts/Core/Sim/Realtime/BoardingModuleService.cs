using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIP_FITTING.md §登录模块 · docs/SHIPS.md
 * 本文件: BoardingModuleService.cs — 功能槽登录模块蓄力与夺舍
 * 【机制要点】
 * · boarding_module：在 attackRangeM 内持续 boardingHoldSec 后秒杀目标
 * · 攻击方继承目标 hullId + fittedModules；目标舰体销毁
 * 【关联】ModuleRuntime · FittingValidator · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class BoardingModuleService
{
    public const string ModuleKind = "boarding_module";
    public const float DefaultHoldSec = 100f;
    public const float DefaultRangeM = 100f;

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
                ResetCharge(attacker);
                continue;
            }

            var target = ResolveTarget(bf, attacker);
            if (target == null)
            {
                ResetCharge(attacker);
                continue;
            }

            var rangeM = boardingMod.attackRangeM > 0f ? boardingMod.attackRangeM : DefaultRangeM;
            if (DistanceM(attacker, target) > rangeM)
            {
                ResetCharge(attacker);
                continue;
            }

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
            ResetCharge(attacker);
        }
    }

    public static ModuleDef? FindBoardingModule(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod != null && ModuleKind.Equals(mod.moduleKind, StringComparison.Ordinal))
            {
                return mod;
            }
        }

        return null;
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
        CombatDamageLedger.RecordHit(bf, attacker, victim, victim.structureMax);
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

    private static void ResetCharge(BattlefieldUnit u)
    {
        u.boardingChargeTargetUnitId = null;
        u.boardingChargeSec = 0f;
    }
}
