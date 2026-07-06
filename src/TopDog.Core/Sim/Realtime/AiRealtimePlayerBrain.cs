using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/AI_REALTIME_PLAYER.md · docs/TACTICAL_VIEW.md §AI 对手 · docs/BUILDINGS.md §8 约战
 * 本文件: AiRealtimePlayerBrain.cs — 敌方/AI 守军实时战术 AI
 * 【机制要点】
 * · 攻方 ENEMY 侧始终 tick；约战守方（建筑同侧且 AI 军团）也 tick
 * · 友方有激活场域（守卫模块）→ 各舰环绕最近守卫舰；否则 75% 射程环绕攻击目标
 * 【关联】FleetOrderService · FieldAuraService · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class AiRealtimePlayerBrain
{
    private const float RetargetIntervalSec = 30f;
    private const float AttackOrbitRangeFactor = 0.75f;
    private const float GuardOrbitRadiusFactor = 0.85f;

    public static void Tick(
        GameState state,
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec)
    {
        if (ShouldTickSide(state, bf, UnitSide.ENEMY))
        {
            TickSide(state, bf, UnitSide.ENEMY, modules, ships, dtSec);
        }

        if (ShouldTickSide(state, bf, UnitSide.FRIENDLY))
        {
            TickSide(state, bf, UnitSide.FRIENDLY, modules, ships, dtSec);
        }
    }

    private static bool ShouldTickSide(GameState state, BattlefieldState bf, UnitSide side)
    {
        if (side == UnitSide.ENEMY)
        {
            return HasMovableUnits(bf, UnitSide.ENEMY);
        }

        if (bf.combatSubtype != CombatSubtype.BUILDING_ASSAULT || bf.targetBuildingId == null)
        {
            return false;
        }

        var building = BuildingCombatRules.FindBuildingUnit(bf, bf.targetBuildingId);
        if (building == null || building.side != side)
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (u.side != side || u.IsDestroyed() || u.isBuilding || u.IsBallisticMissile())
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(u.memberId))
            {
                return true;
            }

            var member = FindMember(state, u.memberId);
            if (member != null && CombatHullPrepService.IsAiMember(state, member))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMovableUnits(BattlefieldState bf, UnitSide side)
    {
        foreach (var u in bf.units)
        {
            if (u.side == side && !u.IsDestroyed() && !u.isBuilding && !u.IsBallisticMissile())
            {
                return true;
            }
        }

        return false;
    }

    private static MemberState? FindMember(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }

        return null;
    }

    private static void TickSide(
        GameState state,
        BattlefieldState bf,
        UnitSide side,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec)
    {
        var cdKey = (bf.battlefieldId ?? "") + ":" + (int)side;
        if (!state.aiRetargetCooldownSec.TryGetValue(cdKey, out var cd))
        {
            cd = 0f;
        }

        cd -= dtSec;
        var refreshTargets = false;
        if (cd <= 0f)
        {
            refreshTargets = true;
            cd = RetargetIntervalSec;
        }

        state.aiRetargetCooldownSec[cdKey] = cd;

        var guards = CollectActiveFieldGuards(bf, side, modules, ships);
        foreach (var u in bf.units)
        {
            if (u.side != side || u.IsDestroyed() || u.isBuilding || u.IsBallisticMissile()
                || !u.Arrived(bf.timeSec) || BattlefieldSceneProxyService.IsSceneProxy(u)
                || u.aiOrder == UnitAiOrder.MANUAL
                || u.aiOrder == UnitAiOrder.RECALL)
            {
                continue;
            }

            BattlefieldUnit? fireTarget = null;
            if (refreshTargets)
            {
                fireTarget = FindNearestOpponent(bf, u);
            }
            else if (u.targetUnitId != null)
            {
                fireTarget = BattlefieldSystem.FindUnit(bf, u.targetUnitId);
            }

            if (fireTarget == null || fireTarget.IsDestroyed() || fireTarget.side == side)
            {
                fireTarget = FindNearestOpponent(bf, u);
            }

            if (fireTarget != null)
            {
                u.targetUnitId = fireTarget.unitId;
                u.explicitFocus = true;
            }

            if (guards.Count > 0 && !IsActiveFieldGuard(u, bf, modules))
            {
                var guard = FindNearestUnit(u, guards);
                if (guard != null)
                {
                    var orbitRadius = ResolveGuardOrbitRadiusM(guard, modules, ships);
                    AssignOrbit(u, guard, orbitRadius, fireTarget);
                    continue;
                }
            }

            if (fireTarget != null)
            {
                AssignAttackStandoff(u, fireTarget);
            }
        }
    }

    private static void AssignAttackStandoff(BattlefieldUnit u, BattlefieldUnit fireTarget)
    {
        var standoff = MathF.Max(400f, u.attackRangeM * AttackOrbitRangeFactor);
        var dist = FieldAuraService.DistanceM(u, fireTarget);
        u.targetUnitId = fireTarget.unitId;
        u.explicitFocus = true;
        u.approachTargetUnitId = fireTarget.unitId;
        u.commandMaintainDistM = 0f;
        u.approachHeadingTimerSec = 0f;

        if (dist > u.attackRangeM * 0.95f || dist > standoff + MaintainDeadbandM)
        {
            u.aiOrder = UnitAiOrder.APPROACH;
        }
        else if (dist < standoff - MaintainDeadbandM && dist > u.attackRangeM * 0.35f)
        {
            u.aiOrder = UnitAiOrder.AWAY;
        }
        else
        {
            u.aiOrder = UnitAiOrder.STOP;
        }

        u.throttleOn = u.aiOrder != UnitAiOrder.STOP;
    }

    private const float MaintainDeadbandM = 200f;

    private static void AssignOrbit(
        BattlefieldUnit u,
        BattlefieldUnit orbitCenter,
        float orbitRadiusM,
        BattlefieldUnit? fireTarget)
    {
        u.aiOrder = UnitAiOrder.ORBIT;
        u.orbitTargetUnitId = orbitCenter.unitId;
        u.orbitRadiusM = orbitRadiusM;
        u.orbitPhase = OrbitEntryResolver.OrbitPhaseSeek;
        if (fireTarget?.unitId != null)
        {
            u.targetUnitId = fireTarget.unitId;
            u.explicitFocus = true;
        }

        u.throttleOn = true;
    }

    private static float ResolveGuardOrbitRadiusM(
        BattlefieldUnit guard,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        var hull = guard.hullId != null ? ships.FindHull(guard.hullId) : null;
        var bestRadius = 4000f;
        foreach (var kv in guard.fittedModules)
        {
            if (!CombatModuleEnableService.IsSlotEnabled(guard, kv.Key))
            {
                continue;
            }

            var mod = modules.Resolve(kv.Value);
            if (mod == null || !IsFieldModuleKind(mod.moduleKind))
            {
                continue;
            }

            var radius = FieldAuraService.ResolveFieldRadiusM(guard, mod, hull);
            bestRadius = MathF.Max(bestRadius, radius * GuardOrbitRadiusFactor);
        }

        return MathF.Max(400f, bestRadius);
    }

    private static List<BattlefieldUnit> CollectActiveFieldGuards(
        BattlefieldState bf,
        UnitSide side,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        var guards = new List<BattlefieldUnit>();
        foreach (var u in bf.units)
        {
            if (u.side == side && IsActiveFieldGuard(u, bf, modules))
            {
                guards.Add(u);
            }
        }

        return guards;
    }

    private static bool IsActiveFieldGuard(
        BattlefieldUnit u,
        BattlefieldState bf,
        ModuleRegistry modules)
    {
        if (u.IsDestroyed() || u.isBuilding || u.fieldAuraEnabledAtSec <= 0f)
        {
            return false;
        }

        if (u.fieldAuraCollapseCooldownSec > bf.timeSec)
        {
            return false;
        }

        foreach (var kv in u.fittedModules)
        {
            if (!CombatModuleEnableService.IsSlotEnabled(u, kv.Key))
            {
                continue;
            }

            var mod = modules.Resolve(kv.Value);
            if (mod == null || !IsFieldModuleKind(mod.moduleKind))
            {
                continue;
            }

            if (ModuleActivationService.IsFunctionModuleActive(u, kv.Key, mod))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFieldModuleKind(string? moduleKind) =>
        "shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal)
        || "armor_link_field".Equals(moduleKind, StringComparison.Ordinal);

    private static BattlefieldUnit? FindNearestUnit(BattlefieldUnit self, IReadOnlyList<BattlefieldUnit> candidates)
    {
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var other in candidates)
        {
            if (other.IsDestroyed() || ReferenceEquals(other, self))
            {
                continue;
            }

            var d = FieldAuraService.DistanceM(self, other);
            if (d < bestDist)
            {
                bestDist = d;
                best = other;
            }
        }

        return best;
    }

    private static BattlefieldUnit? FindNearestOpponent(BattlefieldState bf, BattlefieldUnit self)
    {
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var other in bf.units)
        {
            if (other.side == self.side || other.IsDestroyed() || other.isBuilding
                || !other.Arrived(bf.timeSec) || other.IsBallisticMissile()
                || BattlefieldSceneProxyService.IsSceneProxy(other))
            {
                continue;
            }

            var d = FieldAuraService.DistanceM(self, other);
            if (d < bestDist)
            {
                bestDist = d;
                best = other;
            }
        }

        return best;
    }
}
