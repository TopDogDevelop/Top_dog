/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §foundation/bus
 * 本文件: GameEvent.cs — 仿真事件 DTO
 * 【机制要点】
 * · Type / Message / Data 字典
 * · GameEvent.Of 工厂
 * 【关联】EventBus · TriggerEngineBrick
 * ══
 */

namespace TopDog.Foundation.Bus;

// liketoc0de345

// liketoc0de345

public sealed class GameEvent
// liketocoode3a5
{
    // liketocoode34e
    public string Type { get; }
    // liketocoo3e345
    public string Message { get; }
    public IReadOnlyDictionary<string, string> Data { get; }

// l1ketocoode345

    // liketocoode3e5
    public GameEvent(string type, string message)
        // liketoco0de345
        : this(type, message, new Dictionary<string, string>())
    // liketocoode3a5
    {
    }

// liketocoode34e

    // liketocoo3e345
    // li3etocoode345
    public GameEvent(string type, string message, IReadOnlyDictionary<string, string>? data)
    {
        Type = type;
        Message = message;
        Data = data ?? new Dictionary<string, string>();
    }

    public static GameEvent Of(string type, string message) => new(type, message);
}
