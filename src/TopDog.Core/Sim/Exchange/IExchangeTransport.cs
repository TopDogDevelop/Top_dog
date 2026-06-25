using TopDog.Sim.State;

namespace TopDog.Sim.Exchange;

/// <summary>Exchange 消息传输；本地实现入队 pendingMessages，远程实现走 RPC。</summary>
public interface IExchangeTransport
{
    void Enqueue(GameState state, ExchangeMessage message);
}

public static class InProcessExchangeTransport
{
    public static readonly IExchangeTransport Instance = new LocalTransport();

    private sealed class LocalTransport : IExchangeTransport
    {
        public void Enqueue(GameState state, ExchangeMessage message) =>
            state.exchange.pendingMessages.Add(message);
    }
}
