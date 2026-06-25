using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Exchange;

public static class ExchangeIntentService
{
    private static IExchangeTransport Transport => InProcessExchangeTransport.Instance;

    public static void PostDispatch(
        GameState state,
        string legionId,
        IReadOnlyList<string> memberIds,
        string task,
        string? targetSystemId,
        bool infiltration = false)
    {
        Transport.Enqueue(state, new ExchangeMessage
        {
            kind = ExchangeMessageKind.DispatchIntent,
            legionId = legionId,
            memberIds = memberIds.ToList(),
            task = task,
            targetSystemId = targetSystemId,
            infiltration = infiltration,
        });
    }

    public static void PostRecruitComplete(
        GameState state,
        string legionId,
        IReadOnlyList<MemberState> members)
    {
        Transport.Enqueue(state, new ExchangeMessage
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
