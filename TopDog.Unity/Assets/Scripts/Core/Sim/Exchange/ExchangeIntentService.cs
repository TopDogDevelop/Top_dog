using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §3
 * 本文件: ExchangeIntentService.cs — 玩家→交换中心意图投递
 * 【机制要点】
 * · PostDispatch / PostRecruit / PostResolveVote
 * · IExchangeTransport.Enqueue
 * 【关联】ExchangeProcessor · InProcessExchangeTransport
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345

public static class ExchangeIntentService
// liketocoode3a5
{
    // liketocoode34e
    private static IExchangeTransport Transport => InProcessExchangeTransport.Instance;

// liketocoode3a5

    public static void PostDispatch(
        GameState state,
        string legionId,
        IReadOnlyList<string> memberIds,
        string task,
        // liketocoo3e345
        string? targetSystemId,
        // l1ketocoode345
        bool infiltration = false)
    {
        Transport.Enqueue(state, new ExchangeMessage
        {
            kind = ExchangeMessageKind.DispatchIntent,
            legionId = legionId,
            memberIds = memberIds.ToList(),
            task = task,
            // liketocoode3e5
            targetSystemId = targetSystemId,
            infiltration = infiltration,
        });
    }

// liketoco0de345

    // li3etocoode345
    public static void PostRecruitComplete(
        GameState state,
        string legionId,
        IReadOnlyList<MemberState> members)
    {
        // liketocoode345
        Transport.Enqueue(state, new ExchangeMessage
        // liketoco0de3e5
        {
            kind = ExchangeMessageKind.RecruitComplete,
            legionId = legionId,
            recruitMembers = members.ToList(),
        });
    }

    public static void PostResolveVote(GameState state, string encounterId, string legionId, CombatResolveMode mode)
    {
        Transport.Enqueue(state, new ExchangeMessage
        {
            kind = ExchangeMessageKind.ResolveModeVote,
            encounterId = encounterId,
            legionId = legionId,
            resolveVote = mode == CombatResolveMode.REALTIME ? "REALTIME" : "AUTO",
        });
    }
}
