using TopDog.App.Brick;
using TopDog.Content.Mechanisms;
using TopDog.Foundation.Bus;
using TopDog.Sim.State;
using TopDog.Sim.Trigger;

namespace TopDog.Sim.Trigger;

public sealed class TriggerEngineBrick : IBrick
{
    private MechanismCatalog? _catalog;
    private readonly Dictionary<string, List<(MechanismDef mech, MechanismTriggerDef trigger)>> _byEvent = new(StringComparer.Ordinal);

    public string Id() => "trigger.engine";

    public void OnRegister(BrickContext ctx)
    {
        _catalog = MechanismCatalog.LoadDefault();
        BuildIndex();
        foreach (var eventType in _byEvent.Keys)
        {
            ctx.Bus.Subscribe(eventType, evt => Dispatch(ctx.State, evt));
        }
    }

    public void Tick(BrickContext ctx, float dtSec)
    {
    }

    private void BuildIndex()
    {
        _byEvent.Clear();
        if (_catalog == null)
        {
            return;
        }
        foreach (var mech in _catalog.All().Values)
        {
            if (mech.triggers == null)
            {
                continue;
            }
            foreach (var trigger in mech.triggers)
            {
                if (string.IsNullOrWhiteSpace(trigger.when))
                {
                    continue;
                }
                if (!_byEvent.TryGetValue(trigger.when, out var list))
                {
                    list = new List<(MechanismDef, MechanismTriggerDef)>();
                    _byEvent[trigger.when] = list;
                }
                list.Add((mech, trigger));
            }
        }
    }

    internal void Dispatch(GameState state, GameEvent evt)
    {
        if (_catalog == null || string.IsNullOrWhiteSpace(evt.Type))
        {
            return;
        }
        if (!_byEvent.TryGetValue(evt.Type, out var triggers))
        {
            return;
        }
        foreach (var (mech, trigger) in triggers)
        {
            if (!TriggerConditions.Passes(state, trigger.@if))
            {
                if (trigger.@else?.actions != null)
                {
                    ActionExecutor.ExecuteAll(state, trigger.@else.actions);
                }
                continue;
            }
            ActionExecutor.ExecuteAll(state, trigger.then?.actions);
        }
    }
}
