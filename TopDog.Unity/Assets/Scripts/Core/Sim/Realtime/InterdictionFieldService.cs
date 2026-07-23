using TopDog.Content.Modules;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/INTERDICTION_FIELD.md · 数值策划.md §9.1
 * 本文件: InterdictionFieldService.cs — 区域拦截发射源 1Hz
 * 【机制要点】
 * · 跃迁舰主动查询源；无代承/无挡火弹道
 * · 自动连开：effectiveAutoInterdiction → 到期后 SetSlotEnabled(true)
 * ══
 */

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

    public static void TickOneHz(GameState state, BattlefieldState battlefield, ModuleRegistry modules)
    {
        battlefield.interdictionSources.RemoveAll(source =>
            source.expiresAtSec <= battlefield.timeSec
            || source.mobile && (FindOwner(battlefield, source.ownerUnitId) is not { } owner || owner.IsDestroyed()));

        TryAutoReenableInterdictionSlots(state, battlefield, modules);

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

    private static void TryAutoReenableInterdictionSlots(
        GameState state,
        BattlefieldState battlefield,
        ModuleRegistry modules)
    {
        foreach (var owner in battlefield.units)
        {
            if (owner.IsDestroyed() || owner.isBuilding)
            {
                continue;
            }

            if (!FleetOrderService.EffectiveAutoInterdiction(state, owner))
            {
                continue;
            }

            foreach (var pair in owner.fittedModules)
            {
                var module = modules.Resolve(pair.Value);
                if (module?.logicId is not ("logic_interdiction_field_fixed" or "logic_interdiction_field_mobile"))
                {
                    continue;
                }

                if (CombatModuleEnableService.IsSlotEnabled(owner, pair.Key))
                {
                    continue;
                }

                // 玩家手关：不自动重开
                if (owner.playerDisabledModuleSlots.Contains(pair.Key))
                {
                    continue;
                }

                if (owner.modulePulseNextSec.TryGetValue(pair.Key, out var until)
                    && battlefield.timeSec < until)
                {
                    continue;
                }

                owner.modulePulseNextSec.Remove(pair.Key);
                CombatModuleEnableService.SetSlotEnabled(owner, pair.Key, true);
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
        float destX,
        float destY,
        float destZ,
        out float landX,
        out float landY,
        out float landZ)
    {
        landX = destX;
        landY = destY;
        landZ = destZ;
        var bestT = float.MaxValue;
        InterdictionFieldSource? hit = null;
        foreach (var source in battlefield.interdictionSources)
        {
            if (source.expiresAtSec <= battlefield.timeSec)
            {
                continue;
            }

            if (!TryRaySphereFirstHit(
                    originX, originY, originZ,
                    destX, destY, destZ,
                    source.x, source.y, source.z,
                    source.radiusM,
                    out var t)
                || t >= bestT)
            {
                continue;
            }

            bestT = t;
            hit = source;
        }

        if (hit == null)
        {
            return false;
        }

        var dx = destX - originX;
        var dy = destY - originY;
        var dz = destZ - originZ;
        var len = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (len < 1e-3f)
        {
            return false;
        }

        dx /= len;
        dy /= len;
        dz /= len;
        var hx = originX + dx * bestT * len;
        var hy = originY + dy * bestT * len;
        var hz = originZ + dz * bestT * len;
        var ox = hx - hit.x;
        var oy = hy - hit.y;
        var oz = hz - hit.z;
        var ol = MathF.Sqrt(ox * ox + oy * oy + oz * oz);
        if (ol < 1e-3f)
        {
            ox = dx;
            oy = dy;
            oz = dz;
            ol = 1f;
        }

        ox /= ol;
        oy /= ol;
        oz /= ol;
        var edge = hit.radiusM + LandingSafetyOffsetM;
        landX = hit.x + ox * edge;
        landY = hit.y + oy * edge;
        landZ = hit.z + oz * edge;
        return true;
    }

    private static BattlefieldUnit? FindOwner(BattlefieldState bf, string? ownerUnitId) =>
        ownerUnitId == null ? null : bf.units.FirstOrDefault(u => ownerUnitId.Equals(u.unitId, StringComparison.Ordinal));

    private static float DistanceSquared(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return dx * dx + dy * dy + dz * dz;
    }

    private static bool TryRaySphereFirstHit(
        float ox, float oy, float oz,
        float dxEnd, float dyEnd, float dzEnd,
        float cx, float cy, float cz,
        float radius,
        out float t01)
    {
        t01 = 0f;
        var dx = dxEnd - ox;
        var dy = dyEnd - oy;
        var dz = dzEnd - oz;
        var fx = ox - cx;
        var fy = oy - cy;
        var fz = oz - cz;
        var a = dx * dx + dy * dy + dz * dz;
        if (a < 1e-8f)
        {
            return false;
        }

        var b = 2f * (fx * dx + fy * dy + fz * dz);
        var c = fx * fx + fy * fy + fz * fz - radius * radius;
        var disc = b * b - 4f * a * c;
        if (disc < 0f)
        {
            return false;
        }

        var s = MathF.Sqrt(disc);
        var t0 = (-b - s) / (2f * a);
        var t1 = (-b + s) / (2f * a);
        var t = t0 >= 0f && t0 <= 1f ? t0 : t1 >= 0f && t1 <= 1f ? t1 : -1f;
        if (t < 0f)
        {
            return false;
        }

        t01 = t;
        return true;
    }
}
