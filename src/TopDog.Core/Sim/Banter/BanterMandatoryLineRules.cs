using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>专属台词轮次标记（兼容旧名）。</summary>
public static class BanterMandatoryLineRules
{
    public static bool UsesMandatoryLine(MemberState? member) =>
        member != null && BanterPersonalExclusiveLines.CanUsePersonalExclusive(member);

    public static bool UsesMandatoryLine(GameState state, string memberId) =>
        UsesMandatoryLine(FindMember(state, memberId));

    public static bool HasSpokenMandatoryLineThisRound(MemberBanterRuntimeState rt, MemberState member) =>
        BanterPersonalExclusiveLines.HasSpokenExclusiveThisRound(rt, member);

    private static MemberState? FindMember(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }

        return null;
    }
}
