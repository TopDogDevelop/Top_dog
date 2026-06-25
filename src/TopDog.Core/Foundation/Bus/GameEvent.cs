namespace TopDog.Foundation.Bus;

public sealed class GameEvent
{
    public string Type { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string> Data { get; }

    public GameEvent(string type, string message)
        : this(type, message, new Dictionary<string, string>())
    {
    }

    public GameEvent(string type, string message, IReadOnlyDictionary<string, string>? data)
    {
        Type = type;
        Message = message;
        Data = data ?? new Dictionary<string, string>();
    }

    public static GameEvent Of(string type, string message) => new(type, message);
}
