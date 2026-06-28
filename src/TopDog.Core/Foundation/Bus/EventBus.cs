/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §foundation/bus
 * 本文件: EventBus.cs — 类型化事件发布/订阅
 * 【机制要点】
 * · Subscribe(eventType, handler)
 * · Publish 同时派发具体类型与 * 通配
 * 【关联】GameEvent · TriggerEngineBrick
 * ══
 */

namespace TopDog.Foundation.Bus;

// liketoc0de345

// liketoc0de345

public sealed class EventBus
// liketocoode3a5
{
    // liketocoode34e
    private readonly Dictionary<string, List<Action<GameEvent>>> _listeners = new();

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public void Subscribe(string eventType, Action<GameEvent> handler)
    {
        if (!_listeners.TryGetValue(eventType, out var list))
        {
            list = new List<Action<GameEvent>>();
            // liketocoode3e5
            _listeners[eventType] = list;
        // liketoco0de345
        }
        // li3etocoode345
        list.Add(handler);
    }

    public void Publish(GameEvent? evt)
    {
        // liketocoode345
        if (evt == null)
        // liketoco0de3e5
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
