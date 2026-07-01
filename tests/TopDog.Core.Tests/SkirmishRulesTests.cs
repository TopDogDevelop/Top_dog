using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class SkirmishFortressGateTests
{
    [Test]
    public void LegionFortress_ImmuneUntilTwoPersonalFortsDestroyed()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        var defender = "legion_b";
        var attackerUnit = new BattlefieldUnit
        {
            unitId = "u-atk",
            side = UnitSide.FRIENDLY,
            legionId = "legion_a",
        };
        var building = new BuildingState
        {
            buildingId = "bld_legion",
            buildingType = BuildingService.LegionFortress,
            legionId = defender,
        };
        var buildingUnit = new BattlefieldUnit { side = UnitSide.ENEMY, legionId = defender };

        Assert.That(SkirmishBuildingRules.CanDamageBuilding(state, attackerUnit, building, buildingUnit), Is.False);

        state.skirmish!.enemyPersonalFortsDestroyed[defender] = 2;
        Assert.That(SkirmishBuildingRules.CanDamageBuilding(state, attackerUnit, building, buildingUnit), Is.True);
    }
}

[TestFixture]
public sealed class SkirmishScoreTests
{
    [Test]
    public void DestroyFrigate_AddsLedgerEntry()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        var bf = new BattlefieldState { battlefieldId = "bf-1", timeSec = 0f };
        var attacker = new BattlefieldUnit { unitId = "u-a", legionId = "legion_a", side = UnitSide.FRIENDLY };
        var victim = new BattlefieldUnit
        {
            unitId = "u-v",
            legionId = "legion_b",
            side = UnitSide.ENEMY,
            tonnageClass = "FRIGATE",
            hullId = "hull_frigate_pineapple",
        };
        state.skirmish!.elapsedSec = 12f;
        SkirmishScoreService.OnUnitDestroyed(state, bf, victim, attacker, ShipRegistry.LoadDefault());

        Assert.That(state.skirmish.scores["legion_a"], Is.EqualTo(2));
        Assert.That(state.skirmish.scoreLedger, Has.Count.EqualTo(1));
        Assert.That(state.skirmish.scoreLedger[0].targetHullId, Is.EqualTo("hull_frigate_pineapple"));
    }
}

[TestFixture]
public sealed class SkirmishMatchEndTests
{
    [Test]
    public void EndByScore_HigherScoreWins()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        state.skirmish!.scores["legion_a"] = 10;
        state.skirmish.scores["legion_b"] = 3;
        SkirmishMatchEndService.EndByScore(state, "test");

        Assert.That(state.matchEnded, Is.True);
        Assert.That(state.matchWinnerLegionId, Is.EqualTo("legion_a"));
    }
}

[TestFixture]
public sealed class SkirmishRespawnRulesTests
{
    [Test]
    public void PermadeadVictim_DoesNotQueueRespawn()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        state.boardingPermadeadMemberIds.Add("m-victim");
        var unit = new BattlefieldUnit { memberId = "m-victim", legionId = "legion_a", tonnageClass = "FRIGATE" };
        SkirmishRespawnService.QueueRespawn(state, unit);
        Assert.That(state.skirmish!.respawnQueue, Is.Empty);
    }

    [Test]
    public void WingUnit_DoesNotQueueRespawn()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        state.members.Add(new MemberState { memberId = "m-pilot", legionId = "legion_a", name = "Pilot" });
        var wing = new BattlefieldUnit
        {
            memberId = "m-pilot",
            legionId = "legion_a",
            tonnageClass = "STRIKE_CRAFT",
            parentUnitId = "u-carrier",
        };
        SkirmishRespawnService.QueueRespawn(state, wing);
        Assert.That(state.skirmish!.respawnQueue, Is.Empty);
    }

    [Test]
    public void MainShip_QueuesFiveMinuteRespawnWithSingleNotice()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        state.members.Add(new MemberState
        {
            memberId = "m-pilot",
            legionId = "legion_a",
            name = "Pilot",
            equippedHullId = "hull_frigate_pineapple",
        });
        MatchMemberBaselineService.EnsureSnapshot(state);
        var unit = new BattlefieldUnit
        {
            memberId = "m-pilot",
            legionId = "legion_a",
            hullId = "hull_frigate_pineapple",
            tonnageClass = "FRIGATE",
            displayName = "Pilot",
        };
        SkirmishRespawnService.QueueRespawn(state, unit);
        Assert.That(state.skirmish!.respawnQueue, Has.Count.EqualTo(1));
        Assert.That(state.skirmish.respawnQueue[0].respawnAtSec, Is.EqualTo(300f));
        Assert.That(state.alertLog, Has.Count.EqualTo(1));
        Assert.That(state.alertLog[0], Does.Contain("Pilot"));
        Assert.That(state.alertLog[0], Does.Contain("还有 5 分钟重生"));
    }
}

[TestFixture]
public sealed class RespawnNoticeServiceTests
{
    [Test]
    public void FormatRemainText_UsesMinutesWhenWholeMinute()
    {
        Assert.That(RespawnNoticeService.FormatRemainText(300f), Is.EqualTo("还有 5 分钟重生"));
    }

    [Test]
    public void FormatRemainText_UsesSecondsOtherwise()
    {
        Assert.That(RespawnNoticeService.FormatRemainText(45f), Is.EqualTo("还有 45 秒重生"));
    }

    [Test]
    public void PushQueuedOnce_OnlyFiresOncePerMember()
    {
        var state = new GameState();
        RespawnNoticeService.PushQueuedOnce(state, "m-1", "护卫舰", "张三", 300f);
        RespawnNoticeService.PushQueuedOnce(state, "m-1", "护卫舰", "张三", 300f);
        Assert.That(state.alertLog, Has.Count.EqualTo(1));
    }
}

[TestFixture]
public sealed class SkirmishAiBrainTests
{
    [Test]
    public void OpeningTick_IssuesWarpFlagForAiLegion()
    {
        var state = SkirmishTestHelper.NewSkirmishState();
        state.legions[1].isAiControlled = true;
        state.buildings.Add(new BuildingState
        {
            buildingId = "pf-enemy",
            buildingType = BuildingService.PersonalFortress,
            legionId = "legion_a",
            eventRegionId = "er-pf",
        });
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-home",
            eventRegionId = "er-home",
            units =
            {
                new BattlefieldUnit
                {
                    unitId = "u-ai",
                    legionId = "legion_b",
                    hullId = "hull_frigate_pineapple",
                    inTacticalWarp = false,
                },
            },
        });
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-pf",
            eventRegionId = "er-pf",
            anchorAu = new[] { 1f, 0f, 0f },
            units =
            {
                new BattlefieldUnit
                {
                    unitId = "u-pf",
                    buildingId = "pf-enemy",
                    isBuilding = true,
                    legionId = "legion_a",
                    structureHp = 1000f,
                    structureMax = 1000f,
                },
            },
        });

        var rng = new Random(1);
        SkirmishAiBrain.TickAll(state, ShipRegistry.LoadDefault(), ModuleRegistry.LoadDefault(), 0.1f, rng);

        Assert.That(state.skirmish!.aiOpeningWarpIssued["legion_b"], Is.True);
        Assert.That(state.skirmish.aiTargetPersonalFortBuildingId["legion_b"], Is.EqualTo("pf-enemy"));
    }
}

internal static class SkirmishTestHelper
{
    public static GameState NewSkirmishState()
    {
        var state = new GameState
        {
            worldline = { type = WorldlineType.LEGION_SKIRMISH },
            skirmish = new SkirmishMatchState { scale = 10 },
        };
        state.legions.Add(new LegionState { legionId = "legion_a", isLocal = true });
        state.legions.Add(new LegionState { legionId = "legion_b" });
        return state;
    }
}
