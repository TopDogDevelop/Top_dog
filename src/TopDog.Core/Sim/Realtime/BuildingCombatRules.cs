using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class BuildingCombatRules
{
    public const float PersonalFortStructure = 40_000f;
    public const float LegionFortStructure = 500_000f;
    public const float DefendNoAttackSec = 900f;
    public const float DamageCapPctPerSec = 0.01f;

    public static float StructureMaxForType(string? buildingType) =>
        string.Equals(buildingType, "LEGION_FORTRESS", StringComparison.Ordinal)
            ? LegionFortStructure
            : PersonalFortStructure;

    public static void SpawnBuildingUnit(BattlefieldState bf, BuildingState building)
    {
        if (building.buildingId == null)
        {
            return;
        }

        var max = StructureMaxForType(building.buildingType);
        var u = new BattlefieldUnit
        {
            unitId = "bld-" + building.buildingId,
            buildingId = building.buildingId,
            displayName = building.displayName ?? building.buildingId,
            tonnageClass = "BUILDING",
            side = building.playerOwned ? UnitSide.FRIENDLY : UnitSide.ENEMY,
            isBuilding = true,
            structureMax = max,
            structureHp = max,
            x = 0f,
            y = 0f,
            arrivalAtSec = 0f,
            alive = true,
        };
        bf.units.Add(u);
    }

    public static void TickBuildingWin(BattlefieldState bf, BuildingState? building)
    {
        if (building == null || bf.targetBuildingId == null || bf.finished)
        {
            return;
        }

        var bUnit = FindBuildingUnit(bf, bf.targetBuildingId);
        if (bUnit == null)
        {
            return;
        }

        if (bUnit.structureHp <= bUnit.structureMax * 0.5f)
        {
            var wasFragile = building.fragile
                || string.Equals(building.status, "FRAGILE", StringComparison.Ordinal);
            building.fragile = true;
            building.status = "FRAGILE";
            if (!wasFragile && !bf.finished)
            {
                bf.finished = true;
                bf.winnerSide = UnitSide.ENEMY;
                bf.winReason = "building_fragile";
            }
        }

        var sinceAttack = bf.lastBuildingDamagedAtSec < 0f
            ? bf.timeSec
            : bf.timeSec - bf.lastBuildingDamagedAtSec;
        if (sinceAttack >= DefendNoAttackSec)
        {
            bf.finished = true;
            bf.winnerSide = bUnit.side;
            bf.winReason = "defend_no_attack_15m";
        }
    }

    public static float ClampBuildingDamage(BattlefieldState bf, BattlefieldUnit buildingUnit, float dmg, float dtSec)
    {
        bf.buildingDamageAccumSec += dtSec;
        if (bf.buildingDamageAccumSec >= 1f)
        {
            bf.buildingDamageAccumSec = 0f;
            bf.buildingDamageThisSecond = 0f;
        }

        var cap = buildingUnit.structureMax * DamageCapPctPerSec * dtSec;
        var allowed = Math.Max(0f, cap - bf.buildingDamageThisSecond);
        var applied = Math.Min(dmg, allowed);
        bf.buildingDamageThisSecond += applied;
        if (applied > 0f)
        {
            bf.lastBuildingDamagedAtSec = bf.timeSec;
        }
        return applied;
    }

    public static bool TryFinishBuildingDestroyed(
        BattlefieldState bf, BuildingState? building, BattlefieldUnit bUnit)
    {
        if (bUnit.structureHp > 0f)
        {
            return false;
        }

        bf.finished = true;
        if (bUnit.side == UnitSide.FRIENDLY)
        {
            bf.winnerSide = UnitSide.ENEMY;
            bf.winReason = "building_destroyed";
            return true;
        }

        if (building != null
            && string.Equals(building.buildingType, "LEGION_FORTRESS", StringComparison.Ordinal)
            && string.Equals(building.status, "NORMAL", StringComparison.Ordinal))
        {
            bUnit.structureHp = 0f;
            bUnit.alive = true;
            bf.winnerSide = UnitSide.ENEMY;
            bf.winReason = "legion_fort_phase_end";
            return true;
        }

        bf.winnerSide = UnitSide.ENEMY;
        bf.winReason = "building_destroyed";
        return true;
    }

    public static BattlefieldUnit? FindBuildingUnit(BattlefieldState bf, string buildingId)
    {
        foreach (var u in bf.units)
        {
            if (buildingId.Equals(u.buildingId, StringComparison.Ordinal))
            {
                return u;
            }
        }
        return null;
    }
}
