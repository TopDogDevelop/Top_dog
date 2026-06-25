using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Clock;
using TopDog.Sim.Order;
using TopDog.Sim.State;

namespace TopDog.App.Brick;

public sealed class BrickContext
{
    public GameState State { get; }
    public EventBus Bus { get; }
    public SimClock Clock { get; }
    public ShipRegistry Ships { get; }
    public ModuleRegistry Modules { get; }
    public TraitCatalog Traits { get; }
    public CommandParser Commands { get; }

    public BrickContext(
        GameState state,
        EventBus bus,
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
