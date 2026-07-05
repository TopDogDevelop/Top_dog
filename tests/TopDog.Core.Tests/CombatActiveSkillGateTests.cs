using System.Linq;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatActiveSkillGateTests
{
    [Test]
    public void ListActiveSkillCasters_InCombatPrep_WithRosterMember()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP, storyRound = 1 };
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
        };
        state.members.Add(caster);
        state.combatQueue.Add(new CombatQueueEntry
        {
            entryId = "e1",
            friendlyMemberIds = { "1000100101" },
        });
        state.combatQueueIndex = 0;

        var list = CombatActiveSkillGate.ListActiveSkillCasters(
            state,
            TraitActiveSkillService.BoardSummonTraitId).ToList();

        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0].Caster.memberId, Is.EqualTo("1000100101"));
    }

    [Test]
    public void ListMemberActiveSkills_ReturnsTraitsForRosterMember()
    {
        var state = new GameState
        {
            phase = GamePhase.COMBAT_PREP,
            storyRound = 1,
            combatRealtimeActive = true,
        };
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds =
            {
                TraitActiveSkillService.BoardSummonTraitId,
                TraitActiveSkillService.PlanningSupportTraitId,
            },
        };
        state.members.Add(new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIP",
        });
        state.combatQueue.Add(new CombatQueueEntry
        {
            entryId = "e1",
            friendlyMemberIds = { "1000100101" },
        });
        state.combatQueueIndex = 0;

        var skills = CombatActiveSkillGate.ListMemberActiveSkills(state, "1000100101").ToList();

        Assert.That(skills, Has.Count.EqualTo(1));
        Assert.That(skills[0].TraitId, Is.EqualTo(TraitActiveSkillService.BoardSummonTraitId));
        Assert.That(skills.All(s => s.CanUse), Is.True);
    }

    [Test]
    public void ListMemberActiveSkills_RequiresCombatRealtimeActive()
    {
        var state = new GameState
        {
            phase = GamePhase.COMBAT_PREP,
            storyRound = 1,
            combatRealtimeActive = false,
        };
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIP",
        });
        state.combatQueue.Add(new CombatQueueEntry
        {
            entryId = "e1",
            friendlyMemberIds = { "1000100101" },
        });
        state.combatQueueIndex = 0;

        var skills = CombatActiveSkillGate.ListMemberActiveSkills(state, "1000100101").ToList();

        Assert.That(skills, Is.Empty);
    }

    [Test]
    public void ListMemberActiveSkills_EmptyForEnemyLegionMember()
    {
        var state = new GameState { phase = GamePhase.COMBAT, storyRound = 1 };
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.legions.Add(new LegionState { legionId = "FOE" });
        state.identities["20002002"] = new IdentityState
        {
            identityCode = "20002002",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(new MemberState
        {
            memberId = "2000200201",
            identityCode = "20002002",
            legionId = "FOE",
        });

        var skills = CombatActiveSkillGate.ListMemberActiveSkills(state, "2000200201").ToList();

        Assert.That(skills, Is.Empty);
    }
}
