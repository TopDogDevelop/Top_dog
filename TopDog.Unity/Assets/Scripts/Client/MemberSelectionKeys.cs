using System;
using System.Collections.Generic;
using TopDog.Sim.State;

namespace TopDog.Client;

/// <summary>团员列表选中 / 编队多选的唯一键（对齐 Java <c>memberSelectionKey</c>）。</summary>
public static class MemberSelectionKeys
{
    public const string UserDataKey = "memberSelectionKey";

    public static string? For(MemberState? member)
    {
        if (member == null)
        {
            return null;
        }
        if (!string.IsNullOrWhiteSpace(member.memberId))
        {
            return member.memberId.Trim();
        }
        if (!string.IsNullOrWhiteSpace(member.identityCode) && !string.IsNullOrWhiteSpace(member.accountSuffix))
        {
            return member.identityCode.Trim() + member.accountSuffix.Trim();
        }
        return null;
    }

    public static MemberState? FindMember(GameState state, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }
        foreach (var m in state.members)
        {
            if (key.Equals(For(m), StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    public static bool IsSelected(string? rowKey, string? selectedKey) =>
        !string.IsNullOrWhiteSpace(rowKey)
        && rowKey.Equals(selectedKey, StringComparison.Ordinal);

    public static bool IsFormationPicked(string? rowKey, HashSet<string>? picked) =>
        !string.IsNullOrWhiteSpace(rowKey) && picked != null && picked.Contains(rowKey);
}
