using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public static class SkirmishBuildingRules
{
    public static bool IsSkirmish(GameState state) =>
        state.worldline.type == WorldlineType.LEGION_SKIRMISH;

    public static bool CanDamageBuilding(
        GameState state,
        BattlefieldUnit attacker,
        BuildingState building,
        BattlefieldUnit buildingUnit)
    {
        if (!IsSkirmish(state) || building.buildingId == null)
        {
            return true;
        }

        if (attacker.side == buildingUnit.side)
        {
            return false;
        }

        if (string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
        {
            var defenderLegion = building.legionId;
            if (defenderLegion == null)
            {
                return false;
            }

            var destroyed = state.skirmish?.enemyPersonalFortsDestroyed.GetValueOrDefault(defenderLegion) ?? 0;
            return destroyed >= 2;
        }

        return true;
    }

    public static void OnBuildingStructureZero(GameState state, BuildingState building)
    {
        if (!IsSkirmish(state) || state.skirmish == null || building.legionId == null)
        {
            return;
        }

        if (string.Equals(building.buildingType, BuildingService.PersonalFortress, StringComparison.Ordinal))
        {
            if (!state.skirmish.enemyPersonalFortsDestroyed.ContainsKey(building.legionId))
            {
                state.skirmish.enemyPersonalFortsDestroyed[building.legionId] = 0;
            }

            state.skirmish.enemyPersonalFortsDestroyed[building.legionId]++;
            return;
        }

        if (string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
        {
            SkirmishMatchEndService.EndImmediate(state, building.legionId, "军堡被摧毁");
        }
    }

    public static void SpawnBuildingUnit(GameState state, BattlefieldState bf, BuildingState building)
    {
        if (building.buildingId == null)
        {
            return;
        }

        var scale = state.skirmish?.scale ?? 100;
        var max = SkirmishMapGenerator.StructureHpFor(building.buildingType, scale);
        var isFriendly = state.legions.Exists(l =>
            l.isLocal && building.legionId != null && building.legionId.Equals(l.legionId, StringComparison.Ordinal));
        var u = new BattlefieldUnit
        {
            unitId = "bld-" + building.buildingId,
            buildingId = building.buildingId,
            displayName = building.displayName ?? building.buildingId,
            tonnageClass = "BUILDING",
            side = isFriendly ? UnitSide.FRIENDLY : UnitSide.ENEMY,
            isBuilding = true,
            structureMax = max,
            structureHp = max,
            shieldHp = 0f,
            armorHp = 0f,
            arrivalAtSec = 0f,
            alive = true,
        };
        bf.units.Add(u);
    }
}
