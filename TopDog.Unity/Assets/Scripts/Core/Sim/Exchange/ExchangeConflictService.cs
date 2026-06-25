using TopDog.Sim.State;

namespace TopDog.Sim.Exchange;

/// <summary>兼容入口；逻辑在 <see cref="ExchangeProcessor"/>。</summary>
public static class ExchangeConflictService
{
    public static void ProcessPending(GameState state) => ExchangeProcessor.ProcessPending(state);
}
