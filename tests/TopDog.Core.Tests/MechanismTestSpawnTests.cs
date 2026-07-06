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
    public void Catalog_LoadsSixScenarios()
    {
        Assert.That(MechanismTestCatalog.ListAll(), Has.Count.EqualTo(6));
    }
}
