using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatActiveSkillGateTests
{
    [Test]
    public void ListUsableActiveSkills_RequiresLiveCombatParticipant()
    {
        var state = BuildState();
        state.legions.Add(new LegionState { legionId = "PLAYER", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(new MemberState
        {
            memberId = "m_ai_sheep",
            name = "绵羊伸腿",
            identityCode = "10001001",
            legionId = "AI",
            isAi = true,
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        });
        state.members.Add(new MemberState
        {
            memberId = "m_player",
            name = "奥法凯",
            identityCode = "20002002",
            legionId = "PLAYER",
            isPlayer = true,
        });

        Assert.That(CombatActiveSkillGate.ListUsableActiveSkills(
            state, TraitActiveSkillService.BoardSummonTraitId).ToList(), Is.Empty);
    }

    [Test]
    public void ListUsableActiveSkills_OneSheepAltOnField_IsEnough()
    {
        var state = BuildState();
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(new MemberState
        {
            memberId = "m_sheep_a",
            name = "绵羊伸腿",
            identityCode = "10001001",
            legionId = "VIP",
            isPlayer = true,
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        });
        state.members.Add(new MemberState
        {
            memberId = "m_sheep_b",
            name = "绵羊控股",
            identityCode = "10001001",
            legionId = "VIP",
            isPlayer = true,
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        });
        var bf = state.battlefields[0];
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u1",
            memberId = "m_sheep_a",
            side = UnitSide.FRIENDLY,
            alive = true,
        });

        var skills = CombatActiveSkillGate.ListUsableActiveSkills(
            state, TraitActiveSkillService.BoardSummonTraitId).ToList();
        Assert.That(skills, Has.Count.EqualTo(1));
        Assert.That(skills[0].Caster.memberId, Is.EqualTo("m_sheep_a"));
    }

    [Test]
    public void ListUsableActiveSkills_RosterOnlyWithoutSpawn_IsEmpty()
    {
        var state = BuildState();
        state.legions.Add(new LegionState { legionId = "VIP", isLocal = true });
        state.identities["10001001"] = new IdentityState
        {
            identityCode = "10001001",
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        };
        state.members.Add(new MemberState
        {
            memberId = "m_sheep",
            name = "绵羊伸腿",
            identityCode = "10001001",
            legionId = "VIP",
            isPlayer = true,
            traitIds = { TraitActiveSkillService.BoardSummonTraitId },
        });

        Assert.That(CombatActiveSkillGate.ListUsableActiveSkills(
            state, TraitActiveSkillService.BoardSummonTraitId).ToList(), Is.Empty);
    }

    private static GameState BuildState()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            phase = GamePhase.COMBAT,
        };
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf1",
            systemId = "sys1",
        });
        return state;
    }
}
