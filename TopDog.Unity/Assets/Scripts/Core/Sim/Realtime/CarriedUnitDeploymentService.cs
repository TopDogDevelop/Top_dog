using TopDog.Content.Modules;
using TopDog.Content.Ships;

namespace TopDog.Sim.Realtime;

/// <summary>模板子单位与真实舰实例共用的载舰部署入口。</summary>
public static class CarriedUnitDeploymentService
{
    public const int MaxCarryDepth = 8;
    private const float SpawnOffsetM = 350f;

    public static void DeployAvailable(
        BattlefieldState battlefield,
        BattlefieldUnit parent,
        ModuleRegistry modules,
        ShipRegistry ships,
        Random random)
    {
        if (parent.unitId == null)
        {
            return;
        }

        foreach (var pair in parent.fittedModules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!pair.Key.StartsWith("tube_", StringComparison.Ordinal))
            {
                continue;
            }
            var module = modules.Resolve(pair.Value);
            if (module == null || !IsCarriedUnitBay(module))
            {
                continue;
            }
            if (!parent.bayStates.TryGetValue(pair.Key, out var bay))
            {
                bay = new BayRuntimeState();
                parent.bayStates[pair.Key] = bay;
            }
            if (bay.state is not CarriedUnitLifecycle.Stored and not CarriedUnitLifecycle.Lost)
            {
                continue;
            }
            if (battlefield.units.Any(unit =>
                    parent.unitId.Equals(unit.parentUnitId, StringComparison.Ordinal)
                    && pair.Key.Equals(unit.carriedSourceSlot, StringComparison.Ordinal)
                    && !unit.IsDestroyed()))
            {
                bay.state = CarriedUnitLifecycle.Deployed;
                continue;
            }
            if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(battlefield))
            {
                return;
            }

            var child = CreateChild(parent, pair.Key, module, ships, modules, random);
            if (child == null || WouldExceedDepthOrCycle(battlefield, parent, child.hullId))
            {
                continue;
            }
            bay.state = CarriedUnitLifecycle.Deployed;
            bay.childUnitId = child.unitId;
            bay.shipInstanceId = child.shipInstanceId;
            bay.reservedCapacity = 1;
            battlefield.units.Add(child);
            LaunchTubeStateService.OnWingLaunched(parent, pair.Key);
            CombatTelemetryLog.LogSpawn("carried-unit", child.unitId!, parent.unitId);
        }
    }

    public static bool IsCarriedUnitBay(ModuleDef module) =>
        "carried_unit_bay".Equals(module.moduleKind, StringComparison.Ordinal)
        || "logic_carried_unit_bay".Equals(module.logicId, StringComparison.Ordinal);

    public static void OnParentDestroyed(
        BattlefieldState battlefield,
        BattlefieldUnit parent,
        ModuleRegistry modules)
    {
        foreach (var child in battlefield.units
                     .Where(unit => parent.unitId != null
                                    && parent.unitId.Equals(unit.parentUnitId, StringComparison.Ordinal))
                     .ToArray())
        {
            var module = child.carriedSourceSlot != null
                         && parent.fittedModules.TryGetValue(child.carriedSourceSlot, out var moduleId)
                ? modules.Resolve(moduleId)
                : null;
            var policy = module?.onParentDestroyed
                         ?? (module != null ? ModuleParam.Text(module, "onParentDestroyed", "DESPAWN") : "DESPAWN");
            if ("REMAIN".Equals(policy, StringComparison.Ordinal))
            {
                child.parentUnitId = null;
                child.rootCarrierUnitId = null;
                child.carriedSourceSlot = null;
            }
            else
            {
                child.alive = false;
                child.structureHp = 0f;
            }
        }
    }

    private static BattlefieldUnit? CreateChild(
        BattlefieldUnit parent,
        string sourceSlot,
        ModuleDef bayModule,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random random)
    {
        var payloadMode = bayModule.payloadMode
                          ?? ModuleParam.Text(bayModule, "payloadMode", "TEMPLATE_SPAWN");
        CarriedShipPayload? payload = null;
        var carriedHullId = bayModule.carriedHullId;
        if (string.IsNullOrWhiteSpace(carriedHullId))
        {
            carriedHullId = ModuleParam.Text(bayModule, "carriedHullId");
        }
        if ("SHIP_INSTANCE".Equals(payloadMode, StringComparison.Ordinal))
        {
            if (!parent.carriedShipsBySlot.TryGetValue(sourceSlot, out payload)
                || payload.destroyed)
            {
                return null;
            }
            carriedHullId = payload.hullId;
        }
        var hull = ships.FindHull(carriedHullId);
        if (hull == null || !hull.canBeCarried)
        {
            return null;
        }

        var angle = (float)random.NextDouble() * MathF.PI * 2f;
        var child = new BattlefieldUnit
        {
            unitId = "carried-" + Guid.NewGuid().ToString("N")[..8],
            parentUnitId = parent.unitId,
            rootCarrierUnitId = parent.rootCarrierUnitId ?? parent.unitId,
            carriedSourceSlot = sourceSlot,
            payloadMode = payloadMode,
            shipInstanceId = payload?.shipInstanceId,
            displayName = hull.displayName,
            hullId = hull.hullId,
            tonnageClass = hull.tonnageClass,
            side = parent.side,
            combatFactionId = parent.combatFactionId,
            memberId = payload?.operatorMemberId ?? parent.memberId,
            legionId = parent.legionId,
            arrivalAtSec = parent.arrivalAtSec,
            x = parent.x + MathF.Cos(angle) * SpawnOffsetM,
            y = parent.y + MathF.Sin(angle) * SpawnOffsetM,
            z = parent.z,
            facingRad = parent.facingRad,
            alive = true,
        };
        if (payload != null)
        {
            child.fittedModules = new Dictionary<string, string>(
                payload.fittedModules, StringComparer.Ordinal);
            child.carriedShipsBySlot = payload.carriedShipsBySlot;
        }
        else if (hull.defaultFittedModules != null)
        {
            child.fittedModules = new Dictionary<string, string>(
                hull.defaultFittedModules, StringComparer.Ordinal);
        }
        ModuleRuntime.ApplyToUnit(child, hull, modules);
        if (payload != null)
        {
            if (payload.shieldHp >= 0f)
            {
                child.shieldHp = Math.Min(child.shieldMax, payload.shieldHp);
            }
            if (payload.armorHp >= 0f)
            {
                child.armorHp = Math.Min(child.armorMax, payload.armorHp);
            }
            if (payload.structureHp >= 0f)
            {
                child.structureHp = Math.Min(child.structureMax, payload.structureHp);
            }
        }
        return child;
    }

    private static bool WouldExceedDepthOrCycle(
        BattlefieldState battlefield,
        BattlefieldUnit parent,
        string? childHullId)
    {
        var depth = 1;
        var current = parent;
        var seenHulls = new HashSet<string>(StringComparer.Ordinal);
        if (childHullId != null)
        {
            seenHulls.Add(childHullId);
        }
        while (current.parentUnitId != null)
        {
            if (current.hullId != null && !seenHulls.Add(current.hullId))
            {
                return true;
            }
            depth++;
            if (depth > MaxCarryDepth)
            {
                return true;
            }
            var next = BattlefieldSystem.FindUnit(battlefield, current.parentUnitId);
            if (next == null || ReferenceEquals(next, current))
            {
                break;
            }
            current = next;
        }
        return parent.hullId != null && parent.hullId.Equals(childHullId, StringComparison.Ordinal);
    }
}
