using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
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
}
