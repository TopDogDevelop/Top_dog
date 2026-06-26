using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatHullPrepTests
{
    [Test]
    public void AiDefender_CanEquipFromLegionStock()
    {
        var state = new GameState();
        var aiId = "legion-ai";
        state.legions.Add(new LegionState
        {
            legionId = aiId,
            isAiControlled = true,
            legionStock = { ["hull_bc_spear"] = 3 },
        });
        var m = new MemberState { memberId = "ai1", legionId = aiId, isAi = true };
        LegionPlayerRegistry.AddMemberToLegion(state, aiId, m);
        var ships = ShipRegistry.LoadDefault();

        Assert.That(CombatHullPrepService.TryEquipFromLegionStock(state, m, ships, new Random(1)), Is.True);
        Assert.That(m.equippedHullId, Is.EqualTo("hull_bc_spear"));
        Assert.That(state.legions[0].legionStock.GetValueOrDefault("hull_bc_spear"), Is.EqualTo(2));
    }

    [Test]
    public void PlayerDefender_DoesNotEquipFromLegionStock()
    {
        var state = new GameState();
        var playerId = "legion-player";
        state.legions.Add(new LegionState
        {
            legionId = playerId,
            isLocal = true,
            isAiControlled = false,
            legionStock = { ["hull_bc_spear"] = 3 },
        });
        var m = new MemberState { memberId = "p1", legionId = playerId, isPlayer = true, isAi = false };
        LegionPlayerRegistry.AddMemberToLegion(state, playerId, m);
        var ships = ShipRegistry.LoadDefault();

        Assert.That(CombatHullPrepService.TryEquipFromLegionStock(state, m, ships, new Random(1)), Is.False);
        Assert.That(m.equippedHullId, Is.Null);
        Assert.That(state.legions[0].legionStock["hull_bc_spear"], Is.EqualTo(3));
    }
}
