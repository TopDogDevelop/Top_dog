using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_INDEX.md · docs/MECHANISM_TEST_SCENARIOS.md · mt_intra_scene_warp
 * 本文件: MechanismIntraSceneWarpSpawnService.cs — ±500km 六轴友舰 + 中心敌舰
 * 【机制要点】
 * · AxisOffsetM=500_000；±X/±Y/±Z 各抽 1 羊村菠萝；中心抽 1 敌方羊村菠萝
 * · 全员空配装；验收同场景跃迁（>150km）与友好声望门控（TACTICAL_WARP §2.1.1）
 * 【关联】CampaignBootstrap · MechanismTestCatalog · FleetOrderService.OrderWarpToSceneTarget
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismIntraSceneWarpSpawnService
{
    public const float AxisOffsetM = 500_000f;

    private static readonly (float x, float y, float z)[] AxisOffsets =
    {
        (AxisOffsetM, 0f, 0f),
        (-AxisOffsetM, 0f, 0f),
        (0f, AxisOffsetM, 0f),
        (0f, -AxisOffsetM, 0f),
        (0f, 0f, AxisOffsetM),
        (0f, 0f, -AxisOffsetM),
    };

    public static void BootstrapBattlefields(
        GameState state,
        MechanismTestScenarioDef scenario,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (state.map?.Project.systems.Count == 0)
        {
            return;
        }

        state.battlefields.Clear();
        var sys = state.map.Project.systems[0];
        var belt = sys.eventRegions.Find(er =>
            MechanismMapGenerator.BeltRegionId.Equals(er.eventRegionId, StringComparison.Ordinal));
        if (belt?.eventRegionId == null)
        {
            return;
        }

        var bf = new BattlefieldState
        {
            battlefieldId = "mt_bf_" + belt.eventRegionId,
            systemId = sys.solarSystemId,
            eventRegionId = belt.eventRegionId,
            anchorAu = belt.anchorAu,
            subLocation = belt.name,
        };
        state.battlefields.Add(bf);

        var playerLegion = state.legions.Find(l => l.isLocal);
        var enemyLegion = state.legions.Find(l => !l.isLocal);
        if (playerLegion?.legionId == null || enemyLegion?.legionId == null)
        {
            return;
        }

        var friendlies = state.members
            .Where(m => playerLegion.legionId.Equals(m.legionId, StringComparison.Ordinal))
            .ToList();
        Shuffle(friendlies, rng);
        var spawnFriendly = Math.Min(AxisOffsets.Length, friendlies.Count);
        for (var i = 0; i < spawnFriendly; i++)
        {
            var o = AxisOffsets[i];
            SpawnMember(state, bf, friendlies[i], UnitSide.FRIENDLY, o.x, o.y, o.z, ships, modules);
        }

        var enemies = state.members
            .Where(m => enemyLegion.legionId.Equals(m.legionId, StringComparison.Ordinal))
            .ToList();
        if (enemies.Count > 0)
        {
            var enemy = enemies[rng.Next(enemies.Count)];
            SpawnMember(state, bf, enemy, UnitSide.ENEMY, 0f, 0f, 0f, ships, modules);
        }

        state.activeBattlefieldId = bf.battlefieldId;
        SkirmishDisplayNames.SyncSkirmishLabels(state);
        SeedInitialVisionFocus(state);
        BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        MechanismTestPhaseRules.EnsureRealtimeCombat(state);
        MatchMemberBaselineService.EnsureSnapshot(state);
    }

    private static void SpawnMember(
        GameState state,
        BattlefieldState bf,
        MemberState member,
        UnitSide side,
        float x,
        float y,
        float z,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var hullId = member.equippedHullId;
        var hull = string.IsNullOrWhiteSpace(hullId) ? null : ships.FindHull(hullId);
        if (hull?.hullId == null)
        {
            return;
        }

        var facing = side == UnitSide.FRIENDLY
            ? MathF.Atan2(-z, -x)
            : 0f;
        var u = new BattlefieldUnit
        {
            unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
            displayName = string.IsNullOrWhiteSpace(member.name) ? member.memberId ?? "?" : member.name,
            hullId = hull.hullId,
            memberId = member.memberId,
            legionId = member.legionId,
            side = side,
            arrivalAtSec = 0f,
            x = x,
            y = y,
            z = z,
            facingRad = facing,
        };
        u.fittedModules = new Dictionary<string, string>(
            MemberFittingService.Fittings(state, member));
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        ModuleActivationService.EnableFieldModulesByDefault(u, modules);
        LaunchTubeStateService.InitTubeStates(u, modules);
        bf.units.Add(u);
        member.currentSolarSystemId = bf.systemId;
    }

    private static void SeedInitialVisionFocus(GameState state)
    {
        foreach (var bf in state.battlefields)
        {
            foreach (var u in bf.units)
            {
                if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || u.memberId == null)
                {
                    continue;
                }

                state.tacticalCameraUnitId = u.unitId;
                return;
            }
        }
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
