using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class LegionCommanderService
{
    public const int DismissCooldownRounds = 2;
    public const int DismissBelongingPenalty = 50;

    public static bool IsCommanderIdentity(GameState state, string? identityCode) =>
        !string.IsNullOrWhiteSpace(identityCode)
        && identityCode.Equals(state.commanderIdentityCode, StringComparison.Ordinal);

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
        IdentityMigrationService.EnsureFromMembers(state);
        MergePersonalStockToLegion(state, m);
        state.commanderIdentityCode = code;
        if (state.identities.TryGetValue(code, out var id))
        {
            id.isLegionCommander = true;
            IdentityMigrationService.SyncIdentityToAllMembers(state, code);
        }
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
                LegionDepartureService.Depart(state, kv.Key);
            }
        }
        state.commanderIdentityCode = null;
        state.commanderLastDismissStoryRound = state.storyRound;
        PushAlert(state, "军团长已卸任；除军团长外全员归属感 −" + DismissBelongingPenalty);
        return "军团长已卸任";
    }

    public static void MergePersonalStockToLegion(GameState state, MemberState anchor)
    {
        var stock = MemberAssetService.PersonalStock(state, anchor);
        foreach (var e in stock.ToList())
        {
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
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal)
                || memberId.Equals(m.name, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static MemberState? FindAnyMember(GameState state, string identityCode)
    {
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
