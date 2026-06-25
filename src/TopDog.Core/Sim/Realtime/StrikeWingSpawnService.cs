using TopDog.Content.Modules;
using TopDog.Sim.Member;

namespace TopDog.Sim.Realtime;

/// <summary>航母发射管舰载机展开为独立战场单位（TACTICAL_VIEW.md）。</summary>
public static class StrikeWingSpawnService
{
    private const float WingOffsetM = 350f;

    public static void ExpandCarrierWings(
        BattlefieldState bf,
        BattlefieldUnit carrier,
        ModuleRegistry modules,
        Random rng)
    {
        if (carrier.unitId == null || carrier.fittedModules.Count == 0)
        {
            return;
        }

        var wingIndex = 0;
        var seenWings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kv in carrier.fittedModules)
        {
            var modId = kv.Value;
            if (string.IsNullOrWhiteSpace(modId)
                || !modId.Contains("strike_wing", StringComparison.Ordinal))
            {
                continue;
            }

            if (!seenWings.Add(modId))
            {
                continue;
            }

            wingIndex++;
            bf.units.Add(SpawnStrikeCraft(carrier, modId, modules, wingIndex, rng));
        }
    }

    public static void ExpandAllWings(BattlefieldState bf, ModuleRegistry modules, Random rng)
    {
        foreach (var u in bf.units.ToList())
        {
            if (u.isBuilding || u.IsDestroyed() || u.parentUnitId != null)
            {
                continue;
            }

            ExpandCarrierWings(bf, u, modules, rng);
        }
    }

    private static BattlefieldUnit SpawnStrikeCraft(
        BattlefieldUnit carrier,
        string moduleId,
        ModuleRegistry modules,
        int wingIndex,
        Random rng)
    {
        var mod = modules.Resolve(moduleId);
        var label = mod?.displayName ?? ModuleCatalog.DisplayNameZh(moduleId);
        var angle = wingIndex * 0.9f + (float)rng.NextDouble() * 0.4f;
        var ox = MathF.Cos(angle) * WingOffsetM;
        var oy = MathF.Sin(angle) * WingOffsetM;
        return new BattlefieldUnit
        {
            unitId = "wing-" + Guid.NewGuid().ToString("N")[..8],
            parentUnitId = carrier.unitId,
            displayName = label,
            hullId = moduleId,
            tonnageClass = "STRIKE_CRAFT",
            side = carrier.side,
            memberId = carrier.memberId,
            arrivalAtSec = carrier.arrivalAtSec,
            x = carrier.x + ox,
            y = carrier.y + oy,
            z = carrier.z,
            facingRad = carrier.facingRad,
            maxSpeedMps = 900f,
            accelMps2 = 400f,
            shieldHp = 400f,
            shieldMax = 400f,
            armorHp = 200f,
            armorMax = 200f,
            structureHp = 150f,
            structureMax = 150f,
            attackRangeM = 6000f,
            damagePerSec = 55f,
            alive = true,
        };
    }
}
