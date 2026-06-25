using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BoardSummonApproachTests
{
    [Test]
    public void LiveCombat_Summon_CreatesFiveInboundThenArrives()
    {
        var state = new GameState
        {
            storyRound = 2,
            phase = GamePhase.COMBAT,
            combatRealtimeActive = true,
            map = new LoadedMap(
                new MapProject
                {
                    systems =
                    {
                        new SolarSystemDef
                        {
                            solarSystemId = "sys1",
                            eventRegions =
                            {
                                new EventRegionDef
                                {
                                    eventRegionId = "planet_a",
                                    kind = EventRegionKinds.Planet,
                                    name = "测试星",
                                    anchorAu = new[] { 2f, 0f, 0.5f },
                                },
                            },
                        },
                    },
                },
                null),
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
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var echo = TraitActiveSkillService.TryUse(state, caster, TraitActiveSkillService.BoardSummonTraitId);
        Assert.That(echo, Does.Contain("已召唤"));

        var inbound = bf.units
            .Where(u => u.displayName != null && u.displayName.Contains("跃迁中", StringComparison.Ordinal))
            .ToList();
        Assert.That(inbound, Has.Count.EqualTo(BoardSummonService.ReinforcementCount));

        foreach (var u in inbound)
        {
            Assert.That(u.Arrived(bf.timeSec), Is.False);
            u.arrivalAtSec = bf.timeSec;
        }

        BoardSummonApproachService.TickWarpArrivals(bf, new Random(1));
        foreach (var u in inbound)
        {
            Assert.That(u.Arrived(bf.timeSec), Is.True);
            Assert.That(u.displayName, Does.Not.Contain("跃迁中"));
            Assert.That(u.pinnedToBattlefield, Is.True);
        }
    }
}
