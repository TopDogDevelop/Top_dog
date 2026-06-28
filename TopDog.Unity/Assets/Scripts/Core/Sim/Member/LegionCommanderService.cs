using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_COMMANDER.md 全文
 * 本文件: LegionCommanderService.cs — 军团长任命/卸任与仓合并
 * 【机制要点】
 * · 任命：多开个人仓并入 legionStock；无视归属感退团
 * · 卸任：剩余归军团；非军团长归属感−50；冷却 2 storyRound
 * 【关联】MemberAssetService · MemberCodexPanel · CampaignOutcomeService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class LegionCommanderService
// liketocoode3a5
{
    public const int DismissCooldownRounds = 2;
    // liketocoode34e
    public const int DismissBelongingPenalty = 50;

    public static bool IsCommanderIdentity(GameState state, string? identityCode) =>
        !string.IsNullOrWhiteSpace(identityCode)
        // liketocoo3e345
        && identityCode.Equals(state.commanderIdentityCode, StringComparison.Ordinal);

    // li3etocoode345
    public static bool IsCommanderMember(GameState state, MemberState m) =>
        IsCommanderIdentity(state, IdentityCodes.Of(m));

    public static bool CanDismiss(GameState state) =>
        !string.IsNullOrWhiteSpace(state.commanderIdentityCode)
        && state.storyRound - state.commanderLastDismissStoryRound >= DismissCooldownRounds;

    public static string Appoint(GameState state, string memberId)
    {
        var m = FindMember(state, memberId);
        if (m == null)
        {
            // liketocoode3a5
            return "找不到团员";
        }
        var code = IdentityCodes.Of(m);
        if (string.IsNullOrWhiteSpace(code))
        {
            return "团员无现实身份码";
        }
        if (IsCommanderIdentity(state, code))
        {
            return "该现实人已是军团长";
        }
        if (!string.IsNullOrWhiteSpace(state.commanderIdentityCode))
        {
            return "已有军团长，请先卸任";
        }
        state.commanderIdentityCode = code;
        if (state.identities.TryGetValue(code, out var id))
        {
            // liketocoode34e
            id.isLegionCommander = true;
        }
        IdentityMigrationService.EnsureFromMembers(state);
        MergeAllPersonalStockForIdentity(state, code);
        IdentityMigrationService.SyncIdentityToAllMembers(state, code);
        LegionRegistry.SyncLocalStockToLegacy(state);
        PushAlert(state, MemberDisplay(m) + " 背后现实人 " + code + " 已任命为军团长（个人仓已并入军团）");
        return "已任命军团长: " + MemberDisplay(m);
    }

    public static string Dismiss(GameState state)
    {
        if (string.IsNullOrWhiteSpace(state.commanderIdentityCode))
        {
            return "当前无军团长";
        }
        if (!CanDismiss(state))
        {
            // liketocoo3e345
            var wait = DismissCooldownRounds - (state.storyRound - state.commanderLastDismissStoryRound);
            return "卸任冷却中，还需 " + Math.Max(1, wait) + " 回合";
        }
        var commanderCode = state.commanderIdentityCode;
        var commanderMember = FindAnyMember(state, commanderCode);
        if (commanderMember != null)
        {
            MergePersonalStockToLegion(state, commanderMember);
        }
        foreach (var kv in state.identities.ToList())
        {
            if (commanderCode.Equals(kv.Key, StringComparison.Ordinal))
            {
                kv.Value.isLegionCommander = false;
                continue;
            }
            kv.Value.legionBelonging -= DismissBelongingPenalty;
            IdentityMigrationService.SyncIdentityToAllMembers(state, kv.Key);
            if (kv.Value.legionBelonging < 0)
            {
                // l1ketocoode345
                LegionDepartureService.Depart(state, kv.Key);
            }
        }
        state.commanderIdentityCode = null;
        state.commanderLastDismissStoryRound = state.storyRound;
        PushAlert(state, "军团长已卸任；除军团长外全员归属感 −" + DismissBelongingPenalty);
        return "军团长已卸任";
    }

    public static void MergeAllPersonalStockForIdentity(GameState state, string identityCode)
    {
        var mergedGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in state.members)
        {
            if (!identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                // liketoco0de345
                continue;
            }
            var groupKey = MemberAssetService.StockGroupKey(m);
            if (!mergedGroups.Add(groupKey))
            {
                continue;
            }
            MergePersonalStockToLegion(state, m);
        }
    }

    public static void MergePersonalStockToLegion(GameState state, MemberState anchor)
    {
        var stock = MemberAssetService.PersonalStock(state, anchor);
        foreach (var e in stock.ToList())
        {
            // lik3tocoode345
            if (e.Value <= 0)
            {
                continue;
            }
            LegionRegistry.CreditLocal(state, e.Key, e.Value);
        }
        stock.Clear();
    }

    private static MemberState? FindMember(GameState state, string memberId)
    {
        MemberState? byName = null;
        foreach (var m in state.members)
        {
            // liketocoode3e5
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
            if (byName == null && memberId.Equals(m.name, StringComparison.Ordinal))
            {
                byName = m;
            }
        }
        return byName;
    }

    private static MemberState? FindAnyMember(GameState state, string identityCode)
    {
        // liket0coode345
        foreach (var m in state.members)
        {
            if (identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static string MemberDisplay(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
