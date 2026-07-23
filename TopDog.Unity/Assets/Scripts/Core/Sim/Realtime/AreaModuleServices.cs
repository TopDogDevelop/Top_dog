using System.Text.Json;
using TopDog.Content.Modules;

namespace TopDog.Sim.Realtime;

internal static class ModuleParam
{
    public static string Text(ModuleDef module, string key, string fallback = "")
    {
        if (module.@params != null
            && module.@params.TryGetValue(key, out var value)
            && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? fallback;
        }
        return fallback;
    }

    public static float Number(ModuleDef module, string key, float fallback = 0f)
    {
        if (module.@params != null
            && module.@params.TryGetValue(key, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetSingle(out var number))
        {
            return number;
        }
        return fallback;
    }

    public static bool Bool(ModuleDef module, string key, bool fallback = false)
    {
        if (module.@params != null
            && module.@params.TryGetValue(key, out var value)
            && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
        {
            return value.GetBoolean();
        }
        return fallback;
    }
}

public static class AreaModuleRuntimeService
{
    public static void TickOneHz(BattlefieldState battlefield, ModuleRegistry modules)
    {
        foreach (var source in battlefield.units.OrderBy(unit => unit.unitId, StringComparer.Ordinal))
        {
            if (source.IsDestroyed() || source.isBuilding || source.IsBallisticMissile())
            {
                continue;
            }

            foreach (var pair in source.fittedModules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!CombatModuleEnableService.IsSlotEnabled(source, pair.Key))
                {
                    continue;
                }
                var module = modules.Resolve(pair.Value);
                if (module?.resolvedLogic == null)
                {
                    continue;
                }

                switch (module.logicId)
                {
                    case "logic_command_wave":
                        CommandWaveService.TickModule(battlefield, source, pair.Key, module);
                        break;
                    case "logic_group_repair":
                        GroupRepairService.TickModule(battlefield, source, pair.Key, module);
                        break;
                    case "logic_area_energy_pulse":
                        AreaEnergyPulseService.TickModule(battlefield, source, pair.Key, module);
                        break;
                    case "logic_remote_energy_support":
                        RemoteEnergySupportService.TickModule(battlefield, source, pair.Key, module);
                        break;
                }
            }
        }
    }
}

public static class CommandWaveService
{
    public static void TickModule(
        BattlefieldState battlefield,
        BattlefieldUnit source,
        string slotKey,
        ModuleDef module)
    {
        var radius = ModuleParam.Number(module, "radiusM");
        var stackKey = ModuleParam.Text(module, "stackKey", module.moduleId ?? slotKey);
        var stat = ModuleParam.Text(module, "stat");
        var value = ModuleParam.Number(module, "value");
        if (radius <= 0f || value == 0f)
        {
            return;
        }

        var kind = stat switch
        {
            "maxSpeed" => RuntimeEffectKind.MaxSpeedPct,
            "shieldResist" => RuntimeEffectKind.ShieldResistPct,
            "armorResist" => RuntimeEffectKind.ArmorResistPct,
            "quota" => RuntimeEffectKind.QuotaDelta,
            _ => (RuntimeEffectKind?)null,
        };
        if (kind == null)
        {
            return;
        }

        var query = SphereQuery(
            battlefield, source, radius, includeSource: true,
            target => SameLegion(source, target));
        var sourceKey = $"{source.unitId}:{slotKey}";
        foreach (var hit in query.Hits)
        {
            RuntimeEffectService.ApplyOrRefresh(
                hit.Target,
                sourceKey,
                stackKey,
                kind.Value,
                value,
                battlefield.timeSec + 1.1f,
                hit.DistanceM,
                source.fieldAuraEnabledAtSec);
        }
    }

    internal static AoeQueryResult SphereQuery(
        BattlefieldState battlefield,
        BattlefieldUnit source,
        float radiusM,
        bool includeSource,
        Func<BattlefieldUnit, bool> filter) =>
        AoeQueryService.Query(
            battlefield,
            battlefield.spatialHash,
            new AoeTransform(
                new AoeVector3(source.x, source.y, source.z),
                AoeVector3.Forward,
                AoeVector3.Up),
            AoeShape.Sphere(radiusM),
            source,
            includeSource,
            (target, _) => !target.IsBallisticMissile() && filter(target));

    internal static bool SameLegion(BattlefieldUnit a, BattlefieldUnit b) =>
        !string.IsNullOrWhiteSpace(a.legionId)
            ? a.legionId.Equals(b.legionId, StringComparison.Ordinal)
            : a.side == b.side;
}

public static class GroupRepairService
{
    public static void TickModule(
        BattlefieldState battlefield,
        BattlefieldUnit source,
        string slotKey,
        ModuleDef module)
    {
        var cycle = ModuleParam.Number(module, "cycleSec", 10f);
        if (!PulseDue(battlefield, source, slotKey, cycle))
        {
            return;
        }
        var radius = ModuleParam.Number(module, "radiusM");
        var amount = ModuleParam.Number(module, "amount");
        var layer = ModuleParam.Text(module, "layer");
        var query = CommandWaveService.SphereQuery(
            battlefield, source, radius, includeSource: true,
            target => CommandWaveService.SameLegion(source, target));
        foreach (var hit in query.Hits)
        {
            BattlefieldSystem.ApplyLayerRepair(battlefield, hit.Target, layer, amount);
        }
    }

    internal static bool PulseDue(
        BattlefieldState battlefield,
        BattlefieldUnit source,
        string slotKey,
        float cycleSec)
    {
        if (source.modulePulseNextSec.TryGetValue(slotKey, out var next)
            && battlefield.timeSec < next)
        {
            return false;
        }
        source.modulePulseNextSec[slotKey] = battlefield.timeSec + Math.Max(0.1f, cycleSec);
        return true;
    }
}

public static class AreaEnergyPulseService
{
    public static void TickModule(
        BattlefieldState battlefield,
        BattlefieldUnit source,
        string slotKey,
        ModuleDef module)
    {
        var cycle = ModuleParam.Number(module, "cycleSec", 10f);
        if (!GroupRepairService.PulseDue(battlefield, source, slotKey, cycle))
        {
            return;
        }
        var radius = ModuleParam.Number(module, "radiusM");
        var duration = ModuleParam.Number(module, "durationSec", cycle);
        var quotaDelta = ModuleParam.Number(module, "quotaDelta", -1f);
        var query = CommandWaveService.SphereQuery(
            battlefield, source, radius, includeSource: false, _ => true);
        var sourceKey = $"{source.unitId}:{slotKey}";
        foreach (var hit in query.Hits)
        {
            RuntimeEffectService.ApplyOrRefresh(
                hit.Target,
                sourceKey,
                sourceKey,
                RuntimeEffectKind.QuotaDelta,
                quotaDelta,
                battlefield.timeSec + duration,
                hit.DistanceM,
                battlefield.timeSec);
        }
    }
}

public static class RemoteEnergySupportService
{
    public static void TickModule(
        BattlefieldState battlefield,
        BattlefieldUnit source,
        string slotKey,
        ModuleDef module)
    {
        var cycle = ModuleParam.Number(module, "cycleSec", 10f);
        if (!GroupRepairService.PulseDue(battlefield, source, slotKey, cycle))
        {
            return;
        }
        var target = BattlefieldSystem.FindUnit(battlefield, source.targetUnitId);
        if (target == null
            || ReferenceEquals(target, source)
            || target.IsDestroyed()
            || !CommandWaveService.SameLegion(source, target))
        {
            return;
        }
        var range = ModuleParam.Number(module, "rangeM");
        var dx = target.x - source.x;
        var dy = target.y - source.y;
        var dz = target.z - source.z;
        var distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distance > range)
        {
            return;
        }

        RuntimeEffectService.ApplyOrRefresh(
            target,
            $"{source.unitId}:{slotKey}",
            ModuleParam.Text(module, "stackKey", module.moduleId ?? "remote_energy_support"),
            RuntimeEffectKind.QuotaDelta,
            ModuleParam.Number(module, "quotaDelta", 5f),
            battlefield.timeSec + ModuleParam.Number(module, "durationSec", cycle),
            distance,
            battlefield.timeSec);
    }
}
