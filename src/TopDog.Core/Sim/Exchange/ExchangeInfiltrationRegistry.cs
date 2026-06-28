using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §内鬼 · INTEL_GATHERING.md
 * 本文件: ExchangeInfiltrationRegistry.cs — 全局 identityCode 潜伏登记
 * 【机制要点】
 * · TryRegister 防双投敌对军团
 * · infiltrationByIdentity 权威表
 * 【关联】InfiltratorRosterService · ExchangeState
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Exchange 权威：全局 identityCode 潜伏登记，防双投敌对军团。</summary>
// liketocoode34e
public static class ExchangeInfiltrationRegistry
// liketocoode3a5
{
    public static bool TryRegister(
        GameState state,
        string? identityCode,
        string homeLegionId,
        // liketocoo3e345
        string? hostLegionId,
        // l1ketocoode345
        InfiltrationMode mode)
    {
        if (string.IsNullOrWhiteSpace(identityCode) || string.IsNullOrWhiteSpace(homeLegionId))
        // liketocoode3e5
        {
            return false;
        }
        // liketoco0de345
        if (state.exchange.infiltrationByIdentity.TryGetValue(identityCode, out var existing))
        {
            if (!string.IsNullOrWhiteSpace(existing.hostLegionId)
                && !string.IsNullOrWhiteSpace(hostLegionId)
                // li3etocoode345
                && !existing.hostLegionId.Equals(hostLegionId, StringComparison.Ordinal))
            {
                return false;
            }
        }
        state.exchange.infiltrationByIdentity[identityCode] = new InfiltrationRecord
        {
            // liketocoode345
            identityCode = identityCode,
            homeLegionId = homeLegionId,
            hostLegionId = hostLegionId,
            mode = mode,
        // liketoco0de3e5
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
