using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Exchange;

/// <summary>Exchange 权威：全局 identityCode 潜伏登记，防双投敌对军团。</summary>
public static class ExchangeInfiltrationRegistry
{
    public static bool TryRegister(
        GameState state,
        string? identityCode,
        string homeLegionId,
        string? hostLegionId,
        InfiltrationMode mode)
    {
        if (string.IsNullOrWhiteSpace(identityCode) || string.IsNullOrWhiteSpace(homeLegionId))
        {
            return false;
        }
        if (state.exchange.infiltrationByIdentity.TryGetValue(identityCode, out var existing))
        {
            if (!string.IsNullOrWhiteSpace(existing.hostLegionId)
                && !string.IsNullOrWhiteSpace(hostLegionId)
                && !existing.hostLegionId.Equals(hostLegionId, StringComparison.Ordinal))
            {
                return false;
            }
        }
        state.exchange.infiltrationByIdentity[identityCode] = new InfiltrationRecord
        {
            identityCode = identityCode,
            homeLegionId = homeLegionId,
            hostLegionId = hostLegionId,
            mode = mode,
        };
        return true;
    }

    public static bool TryRegisterHostileRecruit(
        GameState state,
        MemberState member,
        string hostLegionId)
    {
        var code = IdentityCodes.Of(member);
        var home = member.homeLegionId ?? member.legionId;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(home))
        {
            return false;
        }
        return TryRegister(state, code, home, hostLegionId, InfiltrationMode.HostileRecruit);
    }

    public static void Unregister(GameState state, string? identityCode)
    {
        if (string.IsNullOrWhiteSpace(identityCode))
        {
            return;
        }
        state.exchange.infiltrationByIdentity.Remove(identityCode);
    }
}
