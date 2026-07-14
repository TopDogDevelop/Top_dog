using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Lobby;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TraitGrantedModuleTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void AdminBoardModule_IsFromTraitAndHiddenFromPlayer()
    {
        var mod = ModuleRegistry.LoadDefault().Find("mod_admin_board_summon");
        Assert.That(mod, Is.Not.Null);
        Assert.That(mod!.fromTrait, Is.True);
        Assert.That(mod.playerVisibleInteractable, Is.False);
        Assert.That(MemberFittingService.IsEquippableModuleId("mod_admin_board_summon", ModuleRegistry.LoadDefault()), Is.False);
        Assert.That(SkirmishLobbyCatalog.AllModuleIds(ModuleRegistry.LoadDefault()), Does.Not.Contain("mod_admin_board_summon"));
    }

    [Test]
    public void EquipModule_RejectsHiddenTraitModule()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var m = new MemberState { memberId = "m1", equippedHullId = "hull_bc_spear" };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m)["mod_admin_board_summon"] = 1;
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");

        var echo = MemberFittingService.EquipModule(state, m, "fn_0", "mod_admin_board_summon", null, hull, modules);
        Assert.That(echo, Does.Contain("不可由玩家装配"));
        Assert.That(MemberFittingService.Fittings(state, m), Is.Empty);
    }

    [Test]
    public void SpawnGrant_WritesAdminBoardModule_ForBoardSummonTrait()
    {
        var member = new MemberState
        {
            memberId = "m1",
            equippedHullId = "hull_bc_spear",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        var unit = new BattlefieldUnit
        {
            unitId = "u1",
            memberId = member.memberId,
            fittedModules = new Dictionary<string, string>(),
        };

        TraitGrantedModuleService.ApplyForMember(member, unit, ModuleRegistry.LoadDefault());

        Assert.That(
            unit.fittedModules[TraitGrantedModuleService.AdminBoardSlotKey],
            Is.EqualTo(TraitGrantedModuleService.AdminBoardModuleId));
    }
}
