using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BoardSummonWingTests
{
    [Test]
    public void LiveCombat_Summon_SpawnsFiveWingsFromCaster()
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
        Assert.That(ships, Is.Not.Null);
        Assert.That(modules, Is.Not.Null);

        var echo = BoardSummonWingService.TrySpawnFromCaster(
            state, bf, caster, ships, modules, new Random(1));
        Assert.That(echo, Does.Contain("翼"));

        var wings = bf.units
            .Where(u => BoardSummonWingService.WingTonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal))
            .ToList();
        Assert.That(wings, Has.Count.EqualTo(BoardSummonWingService.WingCount));
        foreach (var w in wings)
        {
            Assert.That(w.parentUnitId, Is.EqualTo(casterUnit.unitId));
            Assert.That(w.Arrived(bf.timeSec), Is.True);
            Assert.That(w.pinnedToBattlefield, Is.True);
            Assert.That(w.salvoRoundDmg, Is.GreaterThan(0f));
        }
    }

    [Test]
    public void CapFull_RejectsSpawn()
    {
        var bf = new BattlefieldState();
        for (var i = 0; i < BattlefieldUnitLimits.MaxUnitsPerBattlefield; i++)
        {
            bf.units.Add(new BattlefieldUnit { unitId = "u-" + i, structureHp = 1f, structureMax = 1f });
        }
        var caster = new BattlefieldUnit
        {
            unitId = "caster",
            memberId = "m1",
            side = UnitSide.FRIENDLY,
            structureHp = 100f,
            structureMax = 100f,
        };
        bf.units.Add(caster);
        var spawned = BoardSummonWingService.SpawnFromCasterUnit(bf, caster, new Random(1));
        Assert.That(spawned, Is.EqualTo(0));
    }

    [Test]
    public void PendingInject_MultiRegionHarvest_SpawnsOnCastersBattlefield()
    {
        var state = new GameState { storyRound = 2, phase = GamePhase.COMBAT_PREP };
        state.pendingBoardSummonCasterMemberId = "m2";
        state.pendingBoardSummonLegionId = "VIP";
        state.members.Add(Member("m1", "region_a"));
        state.members.Add(Member("m2", "region_b"));
        var entry = new CombatQueueEntry
        {
            entryId = "e1",
            combatSubtype = CombatSubtype.HARVEST,
            battlefieldSystemId = "sys1",
            friendlyMemberIds = { "m1", "m2" },
            enemyRoster =
            {
                new CombatRosterLine { displayName = "守军", hullId = "hull_bc_spear", tonnageClass = "BATTLECRUISER" },
            },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        var spawned = BattlefieldSpawner.SpawnAll(state, entry, ships, modules, new Random(1));

        Assert.That(spawned, Has.Count.EqualTo(2));
        var casterBf = spawned.First(b => b.eventRegionId == "region_b");
        var wings = casterBf.units
            .Where(u => BoardSummonWingService.WingTonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal))
            .ToList();
        Assert.That(wings, Has.Count.EqualTo(BoardSummonWingService.WingCount));
        Assert.That(state.pendingBoardSummonCasterMemberId, Is.Null);
    }

    [Test]
    public void PendingInject_ClearsWhenCasterNotOnAnyBattlefield()
    {
        var state = new GameState { storyRound = 2, phase = GamePhase.COMBAT_PREP };
        state.pendingBoardSummonCasterMemberId = "missing_caster";
        state.pendingBoardSummonLegionId = "VIP";
        state.members.Add(Member("m1", "region_a"));
        var entry = new CombatQueueEntry
        {
            entryId = "e1",
            combatSubtype = CombatSubtype.HARVEST,
            battlefieldSystemId = "sys1",
            friendlyMemberIds = { "m1" },
            enemyRoster =
            {
                new CombatRosterLine { displayName = "守军", hullId = "hull_bc_spear", tonnageClass = "BATTLECRUISER" },
            },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        BattlefieldSpawner.SpawnAll(state, entry, ships, modules, new Random(1));

        Assert.That(state.pendingBoardSummonCasterMemberId, Is.Null);
        Assert.That(state.alertLog.Last(), Does.Contain("未生效"));
    }

    private static MemberState Member(string id, string region) => new()
    {
        memberId = id,
        name = id,
        equippedHullId = "hull_bc_spear",
        opsDeployEventRegionId = region,
        opsDeploySystemId = "sys1",
    };
}
