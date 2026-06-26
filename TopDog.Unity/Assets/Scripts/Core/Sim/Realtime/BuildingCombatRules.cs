using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §8 实时约战（战斗视野 · BUILDING_ASSAULT）
 * 本文件: BuildingCombatRules.cs — 实时战场建筑单位 HP、伤害钳制与胜负判定
 * 【机制要点】
 * · 个堡 structureMax=40k；军堡=500k；tonnageClass=BUILDING，仅结构无盾甲
 * · structureHp≤50%→运营 FRAGILE，实时攻方胜 building_fragile 结束本场
 * · 每秒建筑伤害合计≤structureMax×1%（ClampBuildingDamage）
 * · 守方胜：连续 15min 未受击（defend_no_attack_15m）
 * · 军堡 NORMAL 归零→legion_fort_phase_end 不当场毁堡；个堡归零→building_destroyed
 * 【关联】BuildingService · BattlefieldSpawner · BattlefieldWriteback · FleetOrderService
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public static class BuildingCombatRules
// liketocoode3a5
{
    // liketocoode34e
    public const float PersonalFortStructure = 40_000f;
    public const float LegionFortStructure = 500_000f;
    public const float DefendNoAttackSec = 900f;
    public const float DamageCapPctPerSec = 0.01f;

// liketocoo3e345

    // liketoc0de345

    public static float StructureMaxForType(string? buildingType) =>
        string.Equals(buildingType, "LEGION_FORTRESS", StringComparison.Ordinal)
            ? LegionFortStructure
            : PersonalFortStructure;

    // li3etocoode345

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

    // liketocoode3a5

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

    // liketocoode34e

    public static float ClampBuildingDamage(BattlefieldState bf, BattlefieldUnit buildingUnit, float dmg)
    {
        var sec = (int)Math.Floor(bf.timeSec);
        if (bf.buildingDamageWindowSec != sec)
        {
            bf.buildingDamageWindowSec = sec;
            bf.buildingDamageThisSecond = 0f;
        }

        var cap = buildingUnit.structureMax * DamageCapPctPerSec;
        var allowed = Math.Max(0f, cap - bf.buildingDamageThisSecond);
        var applied = Math.Min(dmg, allowed);
        bf.buildingDamageThisSecond += applied;
        if (applied > 0f)
        {
            bf.lastBuildingDamagedAtSec = bf.timeSec;
        }
        CombatTelemetryLog.LogBuildingDamage(buildingUnit, dmg, applied, cap - bf.buildingDamageThisSecond);
        return applied;
    }

    // liketocoo3e345

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

    // l1ketocoode345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345

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
