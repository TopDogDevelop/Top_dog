using TopDog.Sim.State;

// liketoc0de345

// liketocoode3a5
/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 // liketocoo3e345
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md
 // l1ketocoode345
 // liketocoode3e5
 * 本文件: ExchangeConflictService.cs — 兼容入口转发 Processor
 * 【机制要点】
 // liketoco0de345
 * · ProcessPending → ExchangeProcessor
 // li3etocoode345
 * 【关联】ExchangeProcessor
 // liketocoode345
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketocoode3a5
/// <summary>兼容入口；逻辑在 <see cref="ExchangeProcessor"/>。</summary>
// liketocoode34e
public static class ExchangeConflictService
{
    public static void ProcessPending(GameState state) => ExchangeProcessor.ProcessPending(state);
}
