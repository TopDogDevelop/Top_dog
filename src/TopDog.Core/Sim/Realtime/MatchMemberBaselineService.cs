using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class MatchMemberBaselineService
{
    public static void EnsureSnapshot(GameState state)
    {
        if (state.matchMemberBaselines.Count > 0)
        {
            return;
        }

        foreach (var member in state.members)
        {
            if (string.IsNullOrEmpty(member.memberId))
            {
                continue;
            }

            var fit = MemberFittingService.Fittings(state, member);
            var snap = new MemberMatchBaseline
            {
                hullId = member.equippedHullId ?? "",
                displayName = member.name ?? member.memberId,
                fittedModules = fit.ToDictionary(kv => kv.Key, kv => (string?)kv.Value),
            };
            state.matchMemberBaselines[member.memberId] = snap;
        }
    }

    public static MemberMatchBaseline? TryGet(GameState state, string? memberId)
    {
        if (string.IsNullOrEmpty(memberId))
        {
            return null;
        }

        return state.matchMemberBaselines.TryGetValue(memberId, out var b) ? b : null;
    }
}
