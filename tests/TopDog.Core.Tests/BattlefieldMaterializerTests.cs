using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BattlefieldMaterializerTests
{
    [Test]
    public void CollectProjections_SkipsMembersWithoutHull()
    {
        var state = new GameState();
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        LegionPlayerRegistry.AddMemberToLegion(
            state,
            CampaignLegionIds.Player,
            new MemberState { memberId = "bare", legionId = CampaignLegionIds.Player });
        LegionPlayerRegistry.AddMemberToLegion(
            state,
            CampaignLegionIds.Player,
            new MemberState
            {
                memberId = "armed",
                legionId = CampaignLegionIds.Player,
                equippedHullId = "hull_frigate_scout",
            });

        var projections = BattlefieldMaterializer.CollectProjections(state, CampaignLegionIds.Player);

        Assert.That(projections.Count, Is.EqualTo(1));
        Assert.That(projections[0].memberId, Is.EqualTo("armed"));
    }

    [Test]
    public void TryMaterialize_EntersCombatPhase()
    {
        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
            currentSolarSystemId = "sys_a",
        };
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        state.members.Add(new MemberState
        {
            memberId = "p1",
            legionId = CampaignLegionIds.Player,
            equippedHullId = "hull_bc_spear",
            currentSolarSystemId = "sys_a",
        });
        var brief = new EncounterBrief
        {
            encounterId = "enc_test",
            systemId = "sys_a",
            attackerLegionId = CampaignLegionIds.Player,
            defenderLegionId = CampaignLegionIds.Ai,
            combatSubtype = CombatSubtype.HARVEST,
            attackerRoster =
            {
                new CombatRosterLine { memberId = "p1", canParticipate = true },
            },
            defenderRoster =
            {
                new CombatRosterLine
                {
                    displayName = "AI",
                    hullId = "hull_frigate_scout",
                    tonnageClass = "FRIGATE",
                    combatPower = 10f,
                },
            },
        };

        var ok = BattlefieldMaterializer.TryMaterialize(state, brief);

        Assert.That(ok, Is.True);
        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT));
        Assert.That(state.combatRealtimeActive, Is.True);
        Assert.That(state.battlefields.Count, Is.GreaterThan(0));
    }
}
