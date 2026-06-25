namespace TopDog.Foundation.Bus;

public sealed class EventBus
{
    private readonly Dictionary<string, List<Action<GameEvent>>> _listeners = new();

    public void Subscribe(string eventType, Action<GameEvent> handler)
    {
        if (!_listeners.TryGetValue(eventType, out var list))
        {
            list = new List<Action<GameEvent>>();
            _listeners[eventType] = list;
        }
        list.Add(handler);
    }

    public void Publish(GameEvent? evt)
    {
        if (evt == null)
        {
            return;
        }
        Dispatch(evt.Type, evt);
        Dispatch("*", evt);
    }

    private void Dispatch(string type, GameEvent evt)
    {
        if (!_listeners.TryGetValue(type, out var subs))
        {
            return;
        }
        foreach (var h in subs.ToArray())
        {
            h(evt);
        }
    }
}
