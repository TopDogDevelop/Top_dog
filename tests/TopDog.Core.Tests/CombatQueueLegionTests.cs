using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Net.Local;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatQueueLegionTests
{
    [Test]
    public void BuildingAssault_FromAiPendingAssault_TagsLegionIds()
    {
        var state = new GameState();
        MapTestFixtures.AttachMineSystem(state);
        state.currentSolarSystemId = "sys_mine";
        state.legions.Add(new LegionState
        {
            legionId = "AI_ALPHA",
            isAiControlled = true,
        });
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_player",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_mine",
            playerOwned = true,
            legionId = CampaignLegionIds.Player,
            status = BuildingService.Normal,
        });
        state.aiPendingAssaults.Add(new AiPendingAssaultOp
        {
            attackerLegionId = "AI_ALPHA",
            buildingId = "bld_player",
        });

        CombatQueueCompiler.Compile(state, ShipRegistry.LoadDefault(), ModuleRegistry.LoadDefault());

        Assert.That(state.combatQueue, Has.Count.EqualTo(1));
        var entry = state.combatQueue[0];
        Assert.That(entry.attackerLegionId, Is.EqualTo("AI_ALPHA"));
        Assert.That(entry.defenderLegionId, Is.EqualTo(CampaignLegionIds.Player));
        Assert.That(entry.aiAttacker, Is.True);
    }
}
