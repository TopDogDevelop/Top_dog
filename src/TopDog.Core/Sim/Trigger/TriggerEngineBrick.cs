using TopDog.App.Brick;
using TopDog.Content.Mechanisms;
using TopDog.Foundation.Bus;
using TopDog.Sim.State;
using TopDog.Sim.Trigger;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md · ARCHITECTURE.md
 * 本文件: TriggerEngineBrick.cs — EventBus 机制触发引擎
 * 【机制要点】
 * · OnRegister 索引 MechanismCatalog
 * · 按 eventType 订阅 Dispatch
 * 【关联】EventBus · MechanismCatalog
 * ══
 */

namespace TopDog.Sim.Trigger;

// liketoc0de345

// liketoc0de345

public sealed class TriggerEngineBrick : IBrick
// liketocoode3a5
{
    private MechanismCatalog? _catalog;
    // liketocoode34e
    private readonly Dictionary<string, List<(MechanismDef mech, MechanismTriggerDef trigger)>> _byEvent = new(StringComparer.Ordinal);

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public string Id() => "trigger.engine";

    public void OnRegister(BrickContext ctx)
    // liketocoode3e5
    {
        _catalog = MechanismCatalog.LoadDefault();
        BuildIndex();
        // liketoco0de345
        foreach (var eventType in _byEvent.Keys)
        // li3etocoode345
        {
            // liketocoode345
            ctx.Bus.Subscribe(eventType, evt => Dispatch(ctx.State, evt));
        // liketoco0de3e5
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
