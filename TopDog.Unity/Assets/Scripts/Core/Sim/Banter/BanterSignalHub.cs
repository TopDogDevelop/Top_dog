namespace TopDog.Sim.Banter;

/// <summary>跨静态服务 → MemberBanterBrick 的只读事件队列（不写诊断日志）。</summary>
public static class BanterSignalHub
{
    private static readonly object Gate = new();
    private static readonly Queue<BanterSignal> Pending = new();

    public static void Publish(string eventKey, string memberId, string? otherMemberId = null)
    {
        if (string.IsNullOrWhiteSpace(eventKey) || string.IsNullOrWhiteSpace(memberId))
        {
            return;
        }

        lock (Gate)
        {
            Pending.Enqueue(new BanterSignal(eventKey.Trim(), memberId.Trim(), otherMemberId?.Trim()));
        }
    }

    public static void Drain(List<BanterSignal> into)
    {
        lock (Gate)
        {
            while (Pending.Count > 0)
            {
                into.Add(Pending.Dequeue());
            }
        }
    }

    public static void ClearForTests()
    {
        lock (Gate)
        {
            Pending.Clear();
        }
    }
}

public readonly struct BanterSignal
{
    public string EventKey { get; }
    public string MemberId { get; }
    public string? OtherMemberId { get; }

    public BanterSignal(string eventKey, string memberId, string? otherMemberId = null)
    {
        EventKey = eventKey;
        MemberId = memberId;
        OtherMemberId = otherMemberId;
    }
}
