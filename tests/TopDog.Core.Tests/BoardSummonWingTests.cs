using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BoardSummonWingTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void LiveCombat_Summon_AddsTempTubesAndSpawnsFiveWings()
    {
        var state = new GameState
        {
            storyRound = 2,
            phase = GamePhase.COMBAT,
            combatRealtimeActive = true,
        };
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        var caster = new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIP",
            equippedHullId = "hull_bc_spear",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(caster);
        IdentityMigrationService.EnsureFromMembers(state);

        var bf = new BattlefieldState
        {
            battlefieldId = "bf-live",
            systemId = "sys1",
            timeSec = 10f,
        };
        var casterUnit = new BattlefieldUnit
        {
            unitId = "u-caster",
            memberId = caster.memberId,
            legionId = "VIP",
            displayName = "施法舰",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            structureHp = 1000f,
            structureMax = 1000f,
        };
        bf.units.Add(casterUnit);
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        var echo = BoardSummonWingService.TrySummonViaTempTubes(
            state, bf, caster, casterUnit.unitId, ships, modules, new Random(1));
        Assert.That(echo, Does.Contain("翼"));

        var wings = bf.units
            .Where(u => BoardSummonWingService.WingTonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal))
            .ToList();
        Assert.That(wings, Has.Count.EqualTo(BoardSummonWingService.WingCount));
        foreach (var w in wings)
        {
            Assert.That(w.parentUnitId, Is.EqualTo(casterUnit.unitId));
            Assert.That(w.shieldMax, Is.EqualTo(5000f));
            Assert.That(w.armorMax, Is.EqualTo(30_000f));
            Assert.That(w.structureMax, Is.EqualTo(10_000f));
        }

        Assert.That(casterUnit.fittedModules.Keys.Count(k =>
            BoardSummonWingService.IsTempBoardTube(k)), Is.EqualTo(5));
    }

    [Test]
    public void WingDestroyed_RemovesTempTube()
    {
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var carrier = new BattlefieldUnit
        {
            unitId = "carrier",
            side = UnitSide.FRIENDLY,
            structureHp = 100f,
            structureMax = 100f,
            fittedModules = { [BoardSummonWingService.TempTubePrefix + "0"] = BoardSummonWingService.BoardSummonWingModuleId },
            tubeStates = { [BoardSummonWingService.TempTubePrefix + "0"] = LaunchTubeState.Activated },
        };
        var wing = new BattlefieldUnit
        {
            unitId = "wing",
            parentUnitId = "carrier",
            hullId = BoardSummonWingService.BoardSummonWingModuleId,
            structureHp = 0f,
            alive = false,
        };
        bf.units.Add(carrier);
        bf.units.Add(wing);

        LaunchTubeStateService.NotifyChildDestroyed(bf, wing);
        Assert.That(carrier.fittedModules.ContainsKey(BoardSummonWingService.TempTubePrefix + "0"), Is.False);
    }

    [Test]
    public void PendingInject_ClearsAfterRealtimeSummon()
    {
        var state = new GameState
        {
            storyRound = 2,
            phase = GamePhase.COMBAT,
            combatRealtimeActive = true,
            pendingBoardSummonCasterMemberId = "m2",
        };
        state.members.Add(Member("m2", "region_b", "legion_a"));
        var bf = new BattlefieldState
        {
            battlefieldId = "bf2",
            systemId = "sys1",
            eventRegionId = "region_b",
            timeSec = 1f,
        };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u-m2",
            memberId = "m2",
            legionId = "legion_a",
            side = UnitSide.FRIENDLY,
            structureHp = 1000f,
            structureMax = 1000f,
            arrivalAtSec = 0f,
        });
        state.battlefields.Add(bf);

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        BoardSummonWingService.TryInjectPendingAtSpawn(state, bf, ships, modules, new Random(2));

        Assert.That(state.pendingBoardSummonCasterMemberId, Is.Null);
        Assert.That(
            bf.units.Count(u => BoardSummonWingService.WingTonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal)),
            Is.EqualTo(BoardSummonWingService.WingCount));
    }

    private static MemberState Member(string id, string region, string legionId) => new()
    {
        memberId = id,
        name = id,
        legionId = legionId,
        equippedHullId = "hull_bc_spear",
        opsDeployEventRegionId = region,
        opsDeploySystemId = "sys1",
        traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        identityCode = id,
    };
}
