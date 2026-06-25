using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

public static class CampaignLegionIds
{
    public const string Player = "PLAYER";
    public const string Ai = "AI";
}

public static class MatchIdentityRegistry
{
    public static void Record(GameState state, string? identityCode)
    {
        if (string.IsNullOrWhiteSpace(identityCode))
        {
            return;
        }
        if (!state.matchAppearedIdentityCodes.Contains(identityCode))
        {
            state.matchAppearedIdentityCodes.Add(identityCode);
        }
    }

    public static void SyncAll(GameState state)
    {
        foreach (var m in state.members)
        {
            Record(state, IdentityCodes.Of(m));
        }
        foreach (var kv in state.identities)
        {
            Record(state, kv.Key);
        }
    }

    public static List<string> CreditLines(GameState state)
    {
        var lines = new List<string>();
        foreach (var code in state.matchAppearedIdentityCodes)
        {
            var label = code;
            foreach (var m in state.members)
            {
                if (code.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
                {
                    label = !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
                        : !string.IsNullOrWhiteSpace(m.name) ? m.name!
                        : code;
                    break;
                }
            }
            lines.Add(label);
        }
        return lines;
    }
}
