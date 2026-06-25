using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class IdentityAllocator
{
    public static void EnsureCounter(GameState state)
    {
        long max = 10000000L;
        foreach (var m in state.members)
        {
            BumpMaxFromMember(m, ref max);
        }
        foreach (var player in state.legionPlayers.Values)
        {
            foreach (var m in player.members)
            {
                BumpMaxFromMember(m, ref max);
            }
        }
        if (state.nextIdentityCode <= max)
        {
            state.nextIdentityCode = max + 1;
        }
    }

    private static void BumpMaxFromMember(MemberState m, ref long max)
    {
        if (TryParsePrefix(m.identityCode, out var code))
        {
            max = Math.Max(max, code);
        }
        else if (TryParsePrefix(m.memberId, out code))
        {
            max = Math.Max(max, code);
        }
    }

    public static string NextIdentity(GameState state)
    {
        EnsureCounter(state);
        var code = state.nextIdentityCode.ToString("D8");
        state.nextIdentityCode++;
        return code;
    }

    public static string Suffix(int index) =>
        Math.Clamp(index, 1, 99).ToString("D2");

    private static bool TryParsePrefix(string? id, out long value)
    {
        value = 0;
        if (id == null || id.Length < 8)
        {
            return false;
        }
        return long.TryParse(id[..8], out value);
    }
}
