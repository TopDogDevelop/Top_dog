using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TraitActiveSkillTests
{
    [Test]
    public void BoardSummon_SchedulesReinforcements_InCombatPrep()
    {
        var state = new GameState { storyRound = 2, phase = GamePhase.COMBAT_PREP };
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
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(caster);
        IdentityMigrationService.EnsureFromMembers(state);

        var echo = TraitActiveSkillService.TryUse(state, caster, TraitActiveSkillService.BoardSummonTraitId);
        Assert.That(echo, Does.Contain("已预约"));
        Assert.That(state.pendingBoardSummonLegionId, Is.EqualTo("VIP"));
        Assert.That(
            TraitActiveSkillService.CooldownRoundsRemaining(
                state,
                state.identities["10001001"],
                TraitActiveSkillService.BoardSummonTraitId),
            Is.EqualTo(TraitActiveSkillService.CooldownStoryRounds));
    }

    [Test]
    public void PlanningSupport_RevealsDiscord_AndCostsStarCoin()
    {
        var state = new GameState { storyRound = 1, phase = GamePhase.OPERATIONS };
        state.legions.Add(new LegionState
        {
            legionId = "VIP",
            isLocal = true,
            legionStock = { [CurrencyIds.StarCoin] = 6000 },
        });
        state.legionStock[CurrencyIds.StarCoin] = 6000;
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { TraitActiveSkillService.PlanningSupportTraitId },
        };
        state.identities["10001002"] = new IdentityState
        {
            identityCode = "10001002",
            traitIds = { TraitActiveSkillService.InfiltratorTraitId },
        };
        var caster = new MemberState
        {
            memberId = "1000100101",
            identityCode = "10001001",
            legionId = "VIP",
            traitIds = { TraitActiveSkillService.PlanningSupportTraitId },
        };
        state.members.Add(caster);
        state.members.Add(new MemberState
        {
            memberId = "1000100201",
            identityCode = "10001002",
            legionId = "VIP",
            traitIds = { TraitActiveSkillService.InfiltratorTraitId },
        });
        IdentityMigrationService.EnsureFromMembers(state);

        var echo = TraitActiveSkillService.TryUse(state, caster, TraitActiveSkillService.PlanningSupportTraitId);
        Assert.That(echo, Does.Contain("已揭露"));
        Assert.That(state.revealedInfiltratorIdentityCodes, Does.Contain("10001002"));
        Assert.That(state.legionStock[CurrencyIds.StarCoin], Is.EqualTo(1000));
    }
}
