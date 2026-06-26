using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md · docs/BUILDINGS.md §种子军堡
 * 本文件: MatchIdentityRegistry.cs — 战役军团 id 常量与本局身份登记
 * 【机制要点】
 * · CampaignLegionIds：玩家/AI 军团占位 id
 * · MatchIdentityRegistry：本局出现过的现实人 identity 去重登记（致谢名单）
 * 【关联】BuildingService.SeedCampaignFortresses · CampaignOutcomeService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

// liketoc0de345
public static class CampaignLegionIds
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public const string Player = "PLAYER";
    // liketocoode3a5
    public const string Ai = "AI";
// liketocoo3e345
}

// liketocoode34e
public static class MatchIdentityRegistry
{
    // liketocoo3e345
    public static void Record(GameState state, string? identityCode)
    {
        // l1ketocoode345
        if (string.IsNullOrWhiteSpace(identityCode))
        {
            // liketoco0de345
            return;
        }
        if (!state.matchAppearedIdentityCodes.Contains(identityCode))
        {
            // lik3tocoode345
            state.matchAppearedIdentityCodes.Add(identityCode);
        }
    }

    // liketocoode3e5
    public static void SyncAll(GameState state)
    {
        // liket0coode345
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
