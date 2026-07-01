using TopDog.Content.Ships;

using TopDog.Sim.Building;

using TopDog.Sim.Realtime;

using TopDog.Sim.State;



namespace TopDog.Sim.Skirmish;



/// <summary>约战人机 §8 战术脚本。</summary>

public static class SkirmishAiBrain

{

    public static void TickAll(

        GameState state,

        ShipRegistry ships,

        Content.Modules.ModuleRegistry modules,

        float dtSec,

        Random rng)

    {

        if (state.skirmish == null)

        {

            return;

        }



        foreach (var legion in state.legions)

        {

            if (!legion.isAiControlled || legion.legionId == null)

            {

                continue;

            }



            TickLegion(state, legion.legionId, ships, modules, dtSec, rng);

        }

    }



    private static void TickLegion(

        GameState state,

        string aiLegionId,

        ShipRegistry ships,

        Content.Modules.ModuleRegistry modules,

        float dtSec,

        Random rng)

    {

        var skirmish = state.skirmish!;

        var enemyLegion = state.legions.Find(l => l.legionId != null && !l.legionId.Equals(aiLegionId, StringComparison.Ordinal));

        if (enemyLegion?.legionId == null)

        {

            return;

        }



        if (!skirmish.aiOpeningWarpIssued.GetValueOrDefault(aiLegionId))

        {

            var openingFort = PickPersonalFort(state, enemyLegion.legionId, rng);

            if (openingFort?.buildingId != null && openingFort.eventRegionId != null)

            {

                skirmish.aiTargetPersonalFortBuildingId[aiLegionId] = openingFort.buildingId;

                IssueApproachBuilding(state, aiLegionId, openingFort.eventRegionId, 10_000f, rng);

                FocusLegionOnBuilding(state, aiLegionId, openingFort.buildingId);

                EnsureLeader(state, aiLegionId, rng);

            }



            skirmish.aiOpeningWarpIssued[aiLegionId] = true;

            return;

        }



        var destroyed = skirmish.enemyPersonalFortsDestroyed.GetValueOrDefault(enemyLegion.legionId);

        if (destroyed < 2)

        {

            TickPersonalFortPhase(state, aiLegionId, enemyLegion.legionId, rng);

            return;

        }



        if (!skirmish.aiLegionFortressCheckTimers.ContainsKey(aiLegionId))

        {

            skirmish.aiLegionFortressCheckTimers[aiLegionId] = 0f;

        }



        skirmish.aiLegionFortressCheckTimers[aiLegionId] += dtSec;

        if (skirmish.aiLegionFortressCheckTimers[aiLegionId] < 300f)

        {

            OrderAttackLegionFort(state, aiLegionId, enemyLegion.legionId, rng);

            return;

        }



        skirmish.aiLegionFortressCheckTimers[aiLegionId] = 0f;

        var enemyAtHome = CountEnemyShipsNearLegionFort(state, aiLegionId);

        if (enemyAtHome > 5)

        {

            OrderDefendHomeFormation(state, aiLegionId, rng);

        }

        else

        {

            OrderAttackLegionFort(state, aiLegionId, enemyLegion.legionId, rng);

        }

    }



    private static void TickPersonalFortPhase(

        GameState state,

        string aiLegionId,

        string enemyLegionId,

        Random rng)

    {

        var skirmish = state.skirmish!;

        var targetFort = PickPersonalFort(state, enemyLegionId, rng);

        if (targetFort?.buildingId == null || targetFort.eventRegionId == null)

        {

            return;

        }



        var prevId = skirmish.aiTargetPersonalFortBuildingId.GetValueOrDefault(aiLegionId);

        if (!string.Equals(prevId, targetFort.buildingId, StringComparison.Ordinal))

        {

            skirmish.aiTargetPersonalFortBuildingId[aiLegionId] = targetFort.buildingId;

            IssueApproachBuilding(state, aiLegionId, targetFort.eventRegionId, 10_000f, rng);

            FocusLegionOnBuilding(state, aiLegionId, targetFort.buildingId);

            EnsureLeader(state, aiLegionId, rng);

            return;

        }



        FocusLegionOnBuilding(state, aiLegionId, targetFort.buildingId);

        NudgeLegionShipsTowardRegion(state, aiLegionId, targetFort.eventRegionId, 10_000f, 30_000f, rng);

    }



    private static void OrderAttackLegionFort(GameState state, string aiLegionId, string enemyLegionId, Random rng)

    {

        var region = FindLegionFortressRegion(state, enemyLegionId);

        if (region == null)

        {

            return;

        }



        var dist = 10_000f + (float)rng.NextDouble() * 20_000f;

        IssueApproachBuilding(state, aiLegionId, region, dist, rng);

        var enemyFort = FindLegionFortressBuilding(state, enemyLegionId);

        if (enemyFort?.buildingId != null)

        {

            FocusLegionOnBuilding(state, aiLegionId, enemyFort.buildingId);

        }

    }



    private static void OrderDefendHomeFormation(GameState state, string aiLegionId, Random rng)

    {

        var region = FindLegionFortressRegion(state, aiLegionId);

        var fortBuilding = FindLegionFortressBuilding(state, aiLegionId);

        if (region == null || fortBuilding?.buildingId == null)

        {

            return;

        }



        IssueApproachBuilding(state, aiLegionId, region, 15_000f, rng);

        var fortUnit = FindBuildingUnit(state, fortBuilding.buildingId);

        var leaderId = EnsureLeader(state, aiLegionId, rng);

        if (fortUnit?.unitId == null || leaderId == null)

        {

            return;

        }



        var orbitRadiusM = 10_000f + (float)rng.NextDouble() * 20_000f;

        foreach (var bf in state.battlefields)

        {

            foreach (var u in bf.units)

            {

                if (u.IsDestroyed() || u.isBuilding || !aiLegionId.Equals(u.legionId, StringComparison.Ordinal))

                {

                    continue;

                }



                if (string.Equals(u.unitId, leaderId, StringComparison.Ordinal))

                {

                    u.aiOrder = UnitAiOrder.ORBIT;

                    u.orbitTargetUnitId = fortUnit.unitId;

                    u.orbitRadiusM = orbitRadiusM;

                    u.orbitPhase = OrbitEntryResolver.OrbitPhaseSeek;

                    ShipMotionIntegrator.SnapHeadingToward(u, fortUnit.x, fortUnit.y, fortUnit.z);

                }

                else

                {

                    u.aiOrder = UnitAiOrder.FOLLOW;
                    u.rallyPointUnitId = leaderId;

                }

            }

        }

        FocusNearestEnemyAtHome(state, aiLegionId);
    }

    private static void FocusNearestEnemyAtHome(GameState state, string aiLegionId)
    {
        var region = FindLegionFortressRegion(state, aiLegionId);
        var bf = state.battlefields.Find(b => region != null && region.Equals(b.eventRegionId, StringComparison.Ordinal));
        if (bf == null)
        {
            return;
        }

        BattlefieldUnit? nearest = null;
        var bestDist = float.MaxValue;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding || u.legionId == null || u.legionId.Equals(aiLegionId, StringComparison.Ordinal))
            {
                continue;
            }

            var d = u.x * u.x + u.y * u.y + u.z * u.z;
            if (d < bestDist)
            {
                bestDist = d;
                nearest = u;
            }
        }

        if (nearest?.unitId == null)
        {
            return;
        }

        foreach (var ally in bf.units)
        {
            if (ally.IsDestroyed() || ally.isBuilding || !aiLegionId.Equals(ally.legionId, StringComparison.Ordinal))
            {
                continue;
            }

            if (ally.aiOrder == UnitAiOrder.FOLLOW)
            {
                continue;
            }

            ally.aiOrder = UnitAiOrder.FOCUS;
            ally.targetUnitId = nearest.unitId;
            ally.explicitFocus = true;
        }
    }

    private static string? EnsureLeader(GameState state, string aiLegionId, Random rng)
    {

        var skirmish = state.skirmish!;

        if (skirmish.aiLeaderUnitId.TryGetValue(aiLegionId, out var existing)

            && existing != null

            && FindUnitInState(state, existing) is { } existingLeader
            && !existingLeader.IsDestroyed())

        {

            return existing;

        }



        var candidates = new List<BattlefieldUnit>();

        foreach (var bf in state.battlefields)

        {

            foreach (var u in bf.units)

            {

                if (!u.IsDestroyed() && !u.isBuilding && aiLegionId.Equals(u.legionId, StringComparison.Ordinal))

                {

                    candidates.Add(u);

                }

            }

        }



        if (candidates.Count == 0)

        {

            return null;

        }



        var picked = candidates[rng.Next(candidates.Count)];
        skirmish.aiLeaderUnitId[aiLegionId] = picked.unitId;
        return picked.unitId;

    }



    private static void FocusLegionOnBuilding(GameState state, string legionId, string buildingId)

    {

        var buildingUnit = FindBuildingUnit(state, buildingId);

        if (buildingUnit?.unitId == null)

        {

            return;

        }



        foreach (var bf in state.battlefields)

        {

            foreach (var u in bf.units)

            {

                if (u.IsDestroyed() || u.isBuilding || !legionId.Equals(u.legionId, StringComparison.Ordinal))

                {

                    continue;

                }



                u.aiOrder = UnitAiOrder.FOCUS;

                u.targetUnitId = buildingUnit.unitId;

                u.explicitFocus = true;

            }

        }

    }



    private static void NudgeLegionShipsTowardRegion(

        GameState state,

        string legionId,

        string eventRegionId,

        float minLandingM,

        float maxLandingM,

        Random rng)

    {

        var targetBf = state.battlefields.Find(b => eventRegionId.Equals(b.eventRegionId, StringComparison.Ordinal));

        if (targetBf == null)

        {

            return;

        }



        var landingM = minLandingM + (float)rng.NextDouble() * (maxLandingM - minLandingM);

        foreach (var sourceBf in state.battlefields)

        {

            foreach (var u in sourceBf.units)

            {

                if (u.IsDestroyed() || u.isBuilding || !legionId.Equals(u.legionId, StringComparison.Ordinal))

                {

                    continue;

                }



                if (u.inTacticalWarp || string.Equals(sourceBf.eventRegionId, eventRegionId, StringComparison.Ordinal))

                {

                    continue;

                }



                var hull = u.hullId != null ? ShipRegistry.LoadDefault().FindHull(u.hullId) : null;

                ShipMotionIntegrator.SnapHeadingToward(u, targetBf.anchorAu[0], targetBf.anchorAu[1], targetBf.anchorAu[2]);

                TacticalWarpService.TryBeginWarp(state, u, sourceBf, targetBf, hull, landingM);

            }

        }

    }



    private static void IssueApproachBuilding(

        GameState state,

        string legionId,

        string eventRegionId,

        float landingM,

        Random rng)

    {

        NudgeLegionShipsTowardRegion(state, legionId, eventRegionId, landingM, landingM, rng);

    }



    private static BuildingState? PickPersonalFort(GameState state, string legionId, Random rng)

    {

        var candidates = new List<BuildingState>();

        foreach (var b in state.buildings)

        {

            if (b.legionId != null

                && b.legionId.Equals(legionId, StringComparison.Ordinal)

                && string.Equals(b.buildingType, BuildingService.PersonalFortress, StringComparison.Ordinal))

            {

                var unit = FindBuildingUnit(state, b.buildingId);

                if (unit != null && !unit.IsDestroyed())

                {

                    candidates.Add(b);

                }

            }

        }



        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];

    }



    private static BuildingState? FindLegionFortressBuilding(GameState state, string legionId)

    {

        foreach (var b in state.buildings)

        {

            if (b.legionId != null

                && b.legionId.Equals(legionId, StringComparison.Ordinal)

                && string.Equals(b.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))

            {

                return b;

            }

        }



        return null;

    }



    private static string? FindLegionFortressRegion(GameState state, string legionId)

    {

        return FindLegionFortressBuilding(state, legionId)?.eventRegionId;

    }



    private static BattlefieldUnit? FindBuildingUnit(GameState state, string? buildingId)

    {

        if (buildingId == null)

        {

            return null;

        }



        foreach (var bf in state.battlefields)

        {

            foreach (var u in bf.units)

            {

                if (buildingId.Equals(u.buildingId, StringComparison.Ordinal))

                {

                    return u;

                }

            }

        }



        return null;

    }



    private static BattlefieldUnit? FindUnitInState(GameState state, string unitId)

    {

        foreach (var bf in state.battlefields)

        {

            foreach (var u in bf.units)

            {

                if (unitId.Equals(u.unitId, StringComparison.Ordinal))

                {

                    return u;

                }

            }

        }



        return null;

    }



    private static int CountEnemyShipsNearLegionFort(GameState state, string aiLegionId)

    {

        var region = FindLegionFortressRegion(state, aiLegionId);

        var bf = state.battlefields.Find(b => region != null && region.Equals(b.eventRegionId, StringComparison.Ordinal));

        if (bf == null)

        {

            return 0;

        }



        var count = 0;

        foreach (var u in bf.units)

        {

            if (!u.IsDestroyed() && !u.isBuilding && u.legionId != null && !u.legionId.Equals(aiLegionId, StringComparison.Ordinal))

            {

                count++;

            }

        }



        return count;

    }

}

