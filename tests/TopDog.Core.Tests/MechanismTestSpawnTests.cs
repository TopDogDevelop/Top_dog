using TopDog.App;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MechanismTestSpawnTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void Catalog_BoardSummon_HasRoster()
    {
        FieldNavTestContent.PinRepoContentRoot();
        Assert.That(MechanismTestCatalog.TryGet("mt_board_summon", out var scenario), Is.True);
        Assert.That(scenario.legions, Has.Count.EqualTo(2));
        Assert.That(scenario.legions[0].members, Has.Count.EqualTo(1));
    }

    [Test]
    public void CreateFromMechanismTest_Spawns20kmApart_InRealtimeCombat()
    {
        FieldNavTestContent.PinRepoContentRoot();
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_board_summon");
        var state = core.State;
        Assert.That(state.members.Count, Is.GreaterThan(0));
        Assert.That(state.combatRealtimeActive, Is.True);
        Assert.That(state.mechanismTest?.scenarioId, Is.EqualTo("mt_board_summon"));
        Assert.That(state.battlefields, Has.Count.EqualTo(1));

        var bf = state.battlefields[0];
        Assert.That(bf.eventRegionId, Is.EqualTo(MechanismMapGenerator.BeltRegionId));
        var friendlies = bf.units.Where(u => u.side == UnitSide.FRIENDLY && u.parentUnitId == null).ToList();
        var enemies = bf.units.Where(u => u.side == UnitSide.ENEMY && u.parentUnitId == null).ToList();
        Assert.That(friendlies, Has.Count.EqualTo(1));
        Assert.That(enemies, Has.Count.EqualTo(1));

        var dist = MathF.Abs(friendlies[0].x - enemies[0].x);
        Assert.That(dist, Is.EqualTo(20_000f).Within(1f));
    }

    [Test]
    public void AntiMissile_Spawns50kmFromCenter()
    {
        FieldNavTestContent.PinRepoContentRoot();
        Assert.That(MechanismTestCatalog.TryGet("mt_anti_missile", out var scenario), Is.True);
        Assert.That(scenario.spawnSeparationM, Is.EqualTo(100_000f).Within(1f));

        var core = CampaignBootstrap.CreateFromMechanismTest("mt_anti_missile");
        var bf = core.State.battlefields[0];
        var friendlies = bf.units.Where(u => u.side == UnitSide.FRIENDLY && u.parentUnitId == null).ToList();
        var enemies = bf.units.Where(u => u.side == UnitSide.ENEMY && u.parentUnitId == null).ToList();
        Assert.That(friendlies, Is.Not.Empty);
        Assert.That(enemies, Is.Not.Empty);
        Assert.That(MathF.Abs(friendlies[0].x), Is.EqualTo(50_000f).Within(1f));
        Assert.That(MathF.Abs(enemies[0].x), Is.EqualTo(50_000f).Within(1f));
        Assert.That(MathF.Abs(friendlies[0].x - enemies[0].x), Is.EqualTo(100_000f).Within(1f));
    }

    [Test]
    public void Catalog_FieldAura_ExpandsYangcunRoster()
    {
        FieldNavTestContent.PinRepoContentRoot();
        Assert.That(MechanismTestCatalog.TryGet("mt_field_aura", out var scenario), Is.True);
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_field_aura");
        var state = core.State;
        Assert.That(state.members.Count, Is.EqualTo(22));
        var yangcun = state.members.Where(m => m.legionId == "mt_player").ToList();
        Assert.That(yangcun, Has.Count.EqualTo(20));
        var tieju = state.members.Find(m => m.name == "羊村铁菊");
        Assert.That(tieju?.equippedHullId, Is.EqualTo("hull_cruiser_greywolf_guard"));
        Assert.That(state.memberFittedModules[tieju!.memberId!]["fn_1"], Is.EqualTo("mod_armor_link_s"));
        var starCat = state.members.Find(m => m.name == "羊村星猫");
        Assert.That(state.memberFittedModules[starCat!.memberId!]["atk_1"], Is.EqualTo("mod_deterrence_gun_yl"));
        var generic = state.members.Find(m => m.name == "羊村星星");
        Assert.That(generic?.equippedHullId, Is.EqualTo("hull_frigate_shortlegwolf"));
        Assert.That(state.memberFittedModules[generic!.memberId!].ContainsKey("atk_1"), Is.True);
    }

    [Test]
    public void Catalog_LoadsAllScenarios()
    {
        Assert.That(MechanismTestCatalog.ListAll(), Has.Count.EqualTo(10));
    }

    [Test]
    public void IntraSceneWarp_SpawnsSixAxisFriendliesAndCenterEnemy()
    {
        FieldNavTestContent.PinRepoContentRoot();
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_intra_scene_warp");
        var state = core.State;
        Assert.That(state.combatRealtimeActive, Is.True);
        Assert.That(state.battlefields, Has.Count.EqualTo(1));
        var bf = state.battlefields[0];
        var friendlies = bf.units
            .Where(u => u.side == UnitSide.FRIENDLY && u.parentUnitId == null)
            .ToList();
        var enemies = bf.units
            .Where(u => u.side == UnitSide.ENEMY && u.parentUnitId == null)
            .ToList();
        Assert.That(friendlies, Has.Count.EqualTo(6));
        Assert.That(enemies, Has.Count.EqualTo(1));
        Assert.That(enemies[0].x, Is.EqualTo(0f).Within(1f));
        Assert.That(enemies[0].y, Is.EqualTo(0f).Within(1f));
        Assert.That(enemies[0].z, Is.EqualTo(0f).Within(1f));
        Assert.That(enemies[0].hullId, Is.EqualTo("hull_frigate_pineapple"));
        Assert.That(enemies[0].fittedModules, Is.Empty);

        var axis = MechanismIntraSceneWarpSpawnService.AxisOffsetM;
        var expected = new HashSet<(int, int, int)>
        {
            ((int)axis, 0, 0),
            ((int)-axis, 0, 0),
            (0, (int)axis, 0),
            (0, (int)-axis, 0),
            (0, 0, (int)axis),
            (0, 0, (int)-axis),
        };
        var actual = friendlies
            .Select(u => ((int)MathF.Round(u.x), (int)MathF.Round(u.y), (int)MathF.Round(u.z)))
            .ToHashSet();
        Assert.That(actual, Is.EquivalentTo(expected));
        foreach (var u in friendlies)
        {
            Assert.That(u.hullId, Is.EqualTo("hull_frigate_pineapple"));
            Assert.That(u.fittedModules, Is.Empty);
        }
    }

    [Test]
    public void IntraSceneWarp_OrderToEnemy_RejectedAsNonFriendlyStanding()
    {
        FieldNavTestContent.PinRepoContentRoot();
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_intra_scene_warp");
        var state = core.State;
        var bf = state.battlefields[0];
        var enemy = bf.units.First(u => u.side == UnitSide.ENEMY);
        var msg = FleetOrderService.OrderWarpToSceneTarget(
            state, bf, enemy.unitId, core.Ships, allFriendly: true);
        Assert.That(msg, Does.Contain("友好声望"));
    }

    [Test]
    public void IntraSceneWarp_OrderToFriendly_AcceptedOver150km()
    {
        FieldNavTestContent.PinRepoContentRoot();
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_intra_scene_warp");
        var state = core.State;
        var bf = state.battlefields[0];
        var friendlies = bf.units.Where(u => u.side == UnitSide.FRIENDLY).ToList();
        var source = friendlies[0];
        var target = friendlies.First(u => u.unitId != source.unitId);
        state.tacticalCameraUnitId = source.unitId;
        state.possessingMemberId = source.memberId;
        ShipMotionIntegrator.SnapHeadingToward(source, target.x, target.y, target.z);
        source.vx = 80f;
        var msg = FleetOrderService.OrderWarpToSceneTarget(
            state, bf, target.unitId, core.Ships, allFriendly: false,
            selectedFriendlyUnitIds: new[] { source.unitId! });
        Assert.That(msg, Does.StartWith("已下令").Or.Contain("同场景跃迁"));
        Assert.That(source.warpPhase, Is.Not.EqualTo(TacticalWarpPhase.None));
    }
}
