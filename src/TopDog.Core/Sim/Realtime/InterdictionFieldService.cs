using TopDog.Content.Modules;

namespace TopDog.Sim.Realtime;

public sealed class InterdictionFieldSource
{
    public string sourceId = "";
    public string? ownerUnitId;
    public bool mobile;
    public float x;
    public float y;
    public float z;
    public float radiusM;
    public float expiresAtSec;
}

/// <summary>区域拦截只索引发射源；不向单位推送成员状态，也不受 AOE 200 候选上限约束。</summary>
public static class InterdictionFieldService
{
    public const float LandingSafetyOffsetM = 100f;

    public static void TickOneHz(BattlefieldState battlefield, ModuleRegistry modules)
    {
        battlefield.interdictionSources.RemoveAll(source =>
            source.expiresAtSec <= battlefield.timeSec
            || source.mobile && (FindOwner(battlefield, source.ownerUnitId) is not { } owner || owner.IsDestroyed()));

        foreach (var owner in battlefield.units.OrderBy(unit => unit.unitId, StringComparer.Ordinal))
        {
            if (owner.IsDestroyed() || owner.isBuilding || owner.IsBallisticMissile())
            {
                continue;
            }
            foreach (var pair in owner.fittedModules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!CombatModuleEnableService.IsSlotEnabled(owner, pair.Key))
                {
                    continue;
                }
                var module = modules.Resolve(pair.Value);
                if (module?.logicId is not ("logic_interdiction_field_fixed" or "logic_interdiction_field_mobile"))
                {
                    continue;
                }

                var sourceId = $"{owner.unitId}:{pair.Key}";
                var radius = ModuleParam.Number(module, "radiusM");
                var duration = ModuleParam.Number(module, "durationSec", 60f);
                if (module.logicId == "logic_interdiction_field_fixed")
                {
                    if (owner.modulePulseNextSec.TryGetValue(pair.Key, out var cooldownUntil)
                        && battlefield.timeSec < cooldownUntil)
                    {
                        continue;
                    }
                    if (battlefield.interdictionSources.Any(source =>
                            source.sourceId.Equals(sourceId, StringComparison.Ordinal)))
                    {
                        continue;
                    }
                    battlefield.interdictionSources.Add(new InterdictionFieldSource
                    {
                        sourceId = sourceId,
                        x = owner.x,
                        y = owner.y,
                        z = owner.z,
                        radiusM = radius,
                        expiresAtSec = battlefield.timeSec + duration,
                    });
                    owner.modulePulseNextSec[pair.Key] = battlefield.timeSec + duration;
                    owner.disabledModuleSlots.Add(pair.Key);
                    continue;
                }

                if (!owner.modulePulseNextSec.TryGetValue(pair.Key, out var activeUntil))
                {
                    activeUntil = battlefield.timeSec + duration;
                    owner.modulePulseNextSec[pair.Key] = activeUntil;
                }
                if (battlefield.timeSec >= activeUntil)
                {
                    owner.disabledModuleSlots.Add(pair.Key);
                    continue;
                }
                var mobile = battlefield.interdictionSources.FirstOrDefault(source =>
                    source.sourceId.Equals(sourceId, StringComparison.Ordinal));
                if (mobile == null)
                {
                    mobile = new InterdictionFieldSource
                    {
                        sourceId = sourceId,
                        ownerUnitId = owner.unitId,
                        mobile = true,
                        radiusM = radius,
                        expiresAtSec = activeUntil,
                    };
                    battlefield.interdictionSources.Add(mobile);
                }
                mobile.x = owner.x;
                mobile.y = owner.y;
                mobile.z = owner.z;
            }
        }
    }

    public static bool IsOriginBlocked(
        BattlefieldState battlefield,
        float x,
        float y,
        float z) =>
        battlefield.interdictionSources.Any(source =>
            source.expiresAtSec > battlefield.timeSec
            && DistanceSquared(x, y, z, source.x, source.y, source.z)
            <= source.radiusM * source.radiusM);

    public static bool TryClipRoute(
        BattlefieldState battlefield,
        float originX,
        float originY,
        float originZ,
        float destinationX,
        float destinationY,
        float destinationZ,
        out float clippedX,
        out float clippedY,
        out float clippedZ)
    {
        clippedX = destinationX;
        clippedY = destinationY;
        clippedZ = destinationZ;
        var dx = destinationX - originX;
        var dy = destinationY - originY;
        var dz = destinationZ - originZ;
        var routeLength = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (routeLength <= 0.001f)
        {
            return false;
        }

        var bestT = float.PositiveInfinity;
        foreach (var source in battlefield.interdictionSources)
        {
            if (source.expiresAtSec <= battlefield.timeSec)
            {
                continue;
            }
            if (TryFirstEntryT(
                    originX, originY, originZ,
                    dx, dy, dz,
                    source,
                    out var entryT)
                && entryT < bestT)
            {
                bestT = entryT;
            }
        }
        if (float.IsPositiveInfinity(bestT))
        {
            return false;
        }

        var outsideT = Math.Max(0f, bestT - LandingSafetyOffsetM / routeLength);
        clippedX = originX + dx * outsideT;
        clippedY = originY + dy * outsideT;
        clippedZ = originZ + dz * outsideT;
        return true;
    }

    private static bool TryFirstEntryT(
        float ox,
        float oy,
        float oz,
        float dx,
        float dy,
        float dz,
        InterdictionFieldSource source,
        out float entryT)
    {
        entryT = 0f;
        var mx = ox - source.x;
        var my = oy - source.y;
        var mz = oz - source.z;
        var a = dx * dx + dy * dy + dz * dz;
        var b = 2f * (mx * dx + my * dy + mz * dz);
        var c = mx * mx + my * my + mz * mz - source.radiusM * source.radiusM;
        if (c <= 0f)
        {
            return false;
        }
        var discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
        {
            return false;
        }
        var sqrt = MathF.Sqrt(discriminant);
        var t = (-b - sqrt) / (2f * a);
        if (t < 0f || t > 1f)
        {
            return false;
        }
        entryT = t;
        return true;
    }

    private static BattlefieldUnit? FindOwner(BattlefieldState battlefield, string? ownerUnitId) =>
        ownerUnitId == null ? null : BattlefieldSystem.FindUnit(battlefield, ownerUnitId);

    private static float DistanceSquared(
        float ax, float ay, float az,
        float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return dx * dx + dy * dy + dz * dz;
    }
}
