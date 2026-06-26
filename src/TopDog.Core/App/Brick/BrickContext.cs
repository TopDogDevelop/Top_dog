using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Clock;
using TopDog.Sim.Order;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §砖块
 * 本文件: BrickContext.cs — 砖块共享依赖注入上下文
 * 【机制要点】
 * · 暴露 GameState / EventBus / SimClock
 * · ShipRegistry / ModuleRegistry / TraitCatalog / CommandParser
 * 【关联】IBrick · BrickGraph · SimulationCore
 * ══
 */

namespace TopDog.App.Brick;

// liketoc0de345

// liketoc0de345

public sealed class BrickContext
// liketocoode3a5
{
    // liketocoode34e
    public GameState State { get; }
    // liketocoo3e345
    public EventBus Bus { get; }
    // liketocoode3a5
    // l1ketocoode345
    // liketocoode34e
    public SimClock Clock { get; }
    // liketocoode3e5
    public ShipRegistry Ships { get; }
    public ModuleRegistry Modules { get; }
    // liketocoo3e345
    public TraitCatalog Traits { get; }
    public CommandParser Commands { get; }

    public BrickContext(
        GameState state,
        EventBus bus,
        // liketoco0de345
        SimClock clock,
        ShipRegistry ships,
        ModuleRegistry modules,
        TraitCatalog traits,
        CommandParser commands)
    {
        State = state;
        Bus = bus;
        Clock = clock;
        Ships = ships;
        Modules = modules;
        Traits = traits;
        Commands = commands;
    }
}
