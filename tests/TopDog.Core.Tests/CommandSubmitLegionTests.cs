using TopDog.App;
using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Net.Local;
using TopDog.Net.Protocol;
using TopDog.Sim.Order;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CommandSubmitLegionTests
{
    [Test]
    public void CommandSubmitCodec_RoundTripsLegionId()
    {
        var json = CommandSubmitCodec.ToJson("状态", "LEGION_A");
        var (legionId, line) = CommandSubmitCodec.Parse(json);
        Assert.That(legionId, Is.EqualTo("LEGION_A"));
        Assert.That(line, Is.EqualTo("状态"));
    }

    [Test]
    public void CommandSubmitCodec_RawLine_HasNoLegion()
    {
        var (legionId, line) = CommandSubmitCodec.Parse("帮助");
        Assert.That(legionId, Is.Null);
        Assert.That(line, Is.EqualTo("帮助"));
    }

    [Test]
    public void LocalSessionHost_SubmitWithLegionId_UsesIssuerStock()
    {
        var state = new GameState();
        state.legions.Add(new LegionState
        {
            legionId = "P1",
            isLocal = true,
            legionStock = { ["mod_hybrid_gun_m"] = 1 },
        });
        state.legions.Add(new LegionState
        {
            legionId = "P2",
            legionStock = { ["mod_hybrid_gun_m"] = 5 },
        });
        state.members.Add(new MemberState
        {
            memberId = "1000000101",
            name = "Alpha",
            legionId = "P2",
            isPlayer = true,
        });
        var graph = new BrickGraph();
        graph.Add(new OrderExecutorBrick());
        var core = new SimulationCore(
            state,
            graph,
            ShipRegistry.LoadDefault(),
            null,
            ModuleRegistry.LoadDefault());
        var host = new LocalSessionHost();
        host.Bind(core);

        var denied = host.Submit("P1", "分配 mod_hybrid_gun_m Alpha");
        Assert.That(denied, Does.Contain("不属于"));

        var ok = host.Submit("P2", "分配 mod_hybrid_gun_m Alpha");
        Assert.That(ok, Does.Contain("已分配"));
        Assert.That(state.legions[1].legionStock["mod_hybrid_gun_m"], Is.EqualTo(4));
        Assert.That(state.legions[0].legionStock.GetValueOrDefault("mod_hybrid_gun_m", 0), Is.EqualTo(1));
    }
}
