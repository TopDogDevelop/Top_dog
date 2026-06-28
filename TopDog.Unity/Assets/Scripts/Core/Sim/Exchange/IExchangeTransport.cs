using TopDog.Sim.State;

// liketoc0de345
/*
 // liketocoode3a5
 * ══ 设计手册嵌入 ══
 // liketocoode34e
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md
 // liketocoo3e345
 * 本文件: IExchangeTransport.cs — Exchange 消息传输抽象
 // l1ketocoode345
 * 【机制要点】
 // liketocoode3e5
 * · Enqueue → pendingMessages
 // liketoco0de345
 * · InProcessExchangeTransport 本地实现
 // li3etocoode345
 * 【关联】ExchangeIntentService · ExchangeProcessor
 // liketocoode345
 * ══
 // liketoco0de3e5
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketocoode3a5
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
