using TopDog.Content.Modules;
using TopDog.Content.Ships;

namespace TopDog.Sim.Realtime;

public enum RuntimeEffectKind
{
    MaxSpeedPct,
    ShieldResistPct,
    ArmorResistPct,
    QuotaDelta,
}

public sealed class RuntimeEffectRecord
{
    public string sourceKey = "";
    public string stackKey = "";
    public RuntimeEffectKind kind;
    public float value;
    public float expiresAtSec = float.PositiveInfinity;
    public float sourceDistanceM;
    public float startedAtSec;
}

/// <summary>来源化效果与有效属性；从基线重算，避免反复改写产生漂移。</summary>
public static class RuntimeEffectService
{
    public static void ApplyOrRefresh(
        BattlefieldUnit target,
        string sourceKey,
        string stackKey,
        RuntimeEffectKind kind,
        float value,
        float expiresAtSec,
        float sourceDistanceM,
        float startedAtSec)
    {
        var key = EffectKey(sourceKey, stackKey, kind);
        target.runtimeEffects[key] = new RuntimeEffectRecord
        {
            sourceKey = sourceKey,
            stackKey = stackKey,
            kind = kind,
            value = value,
            expiresAtSec = expiresAtSec,
            sourceDistanceM = sourceDistanceM,
            startedAtSec = startedAtSec,
        };
    }

    public static void RemoveSource(BattlefieldUnit target, string sourceKey)
    {
        foreach (var key in target.runtimeEffects
                     .Where(pair => pair.Value.sourceKey.Equals(sourceKey, StringComparison.Ordinal))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            target.runtimeEffects.Remove(key);
        }
    }

    public static void Expire(BattlefieldUnit target, float nowSec)
    {
        foreach (var key in target.runtimeEffects
                     .Where(pair => pair.Value.expiresAtSec <= nowSec)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            target.runtimeEffects.Remove(key);
        }
    }

    public static float Sum(BattlefieldUnit target, RuntimeEffectKind kind) =>
        SelectActiveByStackKey(target, kind).Sum(effect => effect.value);

    public static float DamageMultiplier(BattlefieldUnit target, RuntimeEffectKind resistKind)
    {
        var multiplier = 1f;
        foreach (var effect in SelectActiveByStackKey(target, resistKind))
        {
            multiplier *= 1f - Math.Clamp(effect.value, 0f, 0.99f);
        }
        return Math.Clamp(multiplier, 0.01f, 1f);
    }

    public static float EffectiveLayerDamageMultiplier(
        BattlefieldUnit target,
        RuntimeEffectKind resistKind,
        ModuleRegistry modules)
    {
        var multiplier = resistKind == RuntimeEffectKind.ArmorResistPct ? 0.9f : 1f;
        foreach (var pair in target.fittedModules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!CombatModuleEnableService.IsSlotEnabled(target, pair.Key))
            {
                continue;
            }
            var module = modules.Resolve(pair.Value);
            if (module == null)
            {
                continue;
            }
            var resistPct = resistKind == RuntimeEffectKind.ShieldResistPct
                ? module.shieldResistPct
                : module.armorResistPct;
            multiplier *= 1f - Math.Clamp(resistPct / 100f, 0f, 0.99f);
        }
        return Math.Clamp(multiplier * DamageMultiplier(target, resistKind), 0.01f, 1f);
    }

    public static void RecomputeEffectiveAttributes(
        BattlefieldUnit unit,
        HullDef hull,
        ModuleRegistry modules)
    {
        var baseSpeed = Math.Max(80f, ResolveModuleSpeed(unit, hull, modules));
        unit.baseMaxSpeedMps = baseSpeed;
        unit.maxSpeedMps = Math.Max(0f, baseSpeed * (1f + Sum(unit, RuntimeEffectKind.MaxSpeedPct)));
    }

    private static float ResolveModuleSpeed(
        BattlefieldUnit unit,
        HullDef hull,
        ModuleRegistry modules)
    {
        var speed = hull.baseSpeedMps;
        foreach (var pair in unit.fittedModules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!CombatModuleEnableService.IsSlotEnabled(unit, pair.Key))
            {
                continue;
            }
            var module = modules.Resolve(pair.Value);
            if (module == null)
            {
                continue;
            }
            speed += module.speedBonusMps;
            if (module.appliesToPropulsion)
            {
                speed += hull.baseSpeedMps * module.speedBonusPctWhenEnabled;
            }
        }
        return speed;
    }

    private static IEnumerable<RuntimeEffectRecord> SelectActiveByStackKey(
        BattlefieldUnit target,
        RuntimeEffectKind kind)
    {
        return target.runtimeEffects.Values
            .Where(effect => effect.kind == kind)
            .GroupBy(effect => effect.stackKey, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(effect => effect.sourceDistanceM)
                .ThenBy(effect => effect.startedAtSec)
                .ThenBy(effect => effect.sourceKey, StringComparer.Ordinal)
                .First());
    }

    private static string EffectKey(string sourceKey, string stackKey, RuntimeEffectKind kind) =>
        $"{sourceKey}\u001f{stackKey}\u001f{kind}";
}

public static class DynamicModuleQuotaService
{
    public static void Tick(
        BattlefieldState battlefield,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        foreach (var unit in battlefield.units)
        {
            if (unit.IsDestroyed() || unit.isBuilding || unit.IsBallisticMissile())
            {
                continue;
            }

            RuntimeEffectService.Expire(unit, battlefield.timeSec);
            var hull = ships.FindHull(unit.hullId);
            if (hull == null)
            {
                continue;
            }

            ApplyQuota(unit, hull, modules);
            RuntimeEffectService.RecomputeEffectiveAttributes(unit, hull, modules);
            SalvoProfileService.ApplyToUnit(unit, hull, modules);
        }
    }

    public static void ApplyQuota(
        BattlefieldUnit unit,
        HullDef hull,
        ModuleRegistry modules)
    {
        var managed = unit.fittedModules
            .Select(pair => new SlotModule(pair.Key, modules.Resolve(pair.Value)))
            .Where(pair => pair.Module != null && IsQuotaManaged(pair.Module))
            .OrderBy(pair => Priority(pair.Module!))
            .ThenBy(pair => pair.SlotKey, StringComparer.Ordinal)
            .ToArray();
        var baseQuota = hull.simultaneousEnableLimit <= 0
            ? managed.Length
            : Math.Min(hull.simultaneousEnableLimit, managed.Length);
        var effectDelta = (int)MathF.Round(RuntimeEffectService.Sum(unit, RuntimeEffectKind.QuotaDelta));
        var effectiveQuota = Math.Clamp(baseQuota + effectDelta, 0, managed.Length);
        unit.effectiveModuleQuota = effectiveQuota;

        var systemForced = unit.disabledModuleSlots
            .Where(slot => !unit.playerDisabledModuleSlots.Contains(slot)
                           && !unit.quotaForcedDisabledSlots.Contains(slot))
            .ToArray();
        unit.quotaForcedDisabledSlots.Clear();
        var kept = 0;
        foreach (var entry in managed)
        {
            if (unit.playerDisabledModuleSlots.Contains(entry.SlotKey))
            {
                continue;
            }
            if (kept < effectiveQuota)
            {
                kept++;
            }
            else
            {
                unit.quotaForcedDisabledSlots.Add(entry.SlotKey);
            }
        }

        unit.disabledModuleSlots.Clear();
        unit.disabledModuleSlots.UnionWith(systemForced);
        unit.disabledModuleSlots.UnionWith(unit.playerDisabledModuleSlots);
        unit.disabledModuleSlots.UnionWith(unit.quotaForcedDisabledSlots);
    }

    private static bool IsQuotaManaged(ModuleDef module) =>
        !string.Equals(module.slotCategory, "LAUNCH_TUBE", StringComparison.Ordinal)
        && !string.Equals(module.slotCategory, "PASSIVE", StringComparison.Ordinal);

    private static int Priority(ModuleDef module)
    {
        if (module.appliesToPropulsion)
        {
            return 0;
        }
        return module.slotCategory switch
        {
            "DEFENSE" => 1,
            "ATTACK" => 2,
            "FUNCTION" => 3,
            _ => 4,
        };
    }

    private readonly record struct SlotModule(string SlotKey, ModuleDef? Module);
}
