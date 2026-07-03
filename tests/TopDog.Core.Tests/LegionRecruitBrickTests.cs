using TopDog.App;
using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Operations;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class LegionRecruitBrickTests
{
    [Test]
    public void Tick_MirrorsLocalLegionProgressToGlobalState()
    {
        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
            currentSolarSystemId = "sys_a",
            flags = { ["exchange.enabled"] = "1" },
        };
        state.legions.Add(new LegionState
        {
            legionId = "legion_local",
            displayName = "测试军团",
            isLocal = true,
        });
        LegionPlayerRegistry.EnsureFromLegions(state);
        RecruitService.Start(state, null);

        var player = state.legionPlayers["legion_local"];
        Assert.That(player.recruitProgressSec, Is.EqualTo(20f).Within(0.01f));
        Assert.That(state.recruitProgressSec, Is.EqualTo(20f).Within(0.01f));

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var brick = new LegionRecruitBrick("legion_local");
        var ctx = new BrickContext(
            state,
            new TopDog.Foundation.Bus.EventBus(),
            new TopDog.Foundation.Clock.SimClock(),
            ships,
            modules,
            TraitCatalog.Empty(),
            new TopDog.Sim.Order.CommandParser());
        brick.Tick(ctx, 5f);

        Assert.That(player.recruitProgressSec, Is.EqualTo(15f).Within(0.01f));
        Assert.That(state.recruitProgressSec, Is.EqualTo(15f).Within(0.01f));
    }
}
