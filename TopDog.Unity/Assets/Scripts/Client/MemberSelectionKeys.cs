using System;
using System.Collections.Generic;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §编队行动
 * 本文件: MemberSelectionKeys.cs — 团员选中/编队唯一键
 * 【机制要点】
 * · 对齐 Java memberSelectionKey
 * 【关联】MemberListView · FleetCommandBar · TacticalSelectionState
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>团员列表选中 / 编队多选的唯一键（对齐 Java <c>memberSelectionKey</c>）。</summary>
public static class MemberSelectionKeys
{
    public const string UserDataKey = "memberSelectionKey";

    // li3etocoode345
    public static string? For(MemberState? member)
    {
        if (member == null)
        {
            return null;
        // liketocoode3a5
        }
        if (!string.IsNullOrWhiteSpace(member.memberId))
        {
            return member.memberId.Trim();
        // liketocoode34e
        }
        if (!string.IsNullOrWhiteSpace(member.identityCode) && !string.IsNullOrWhiteSpace(member.accountSuffix))
        {
            return member.identityCode.Trim() + member.accountSuffix.Trim();
        }
        // liketocoo3e345
        return null;
    }

    public static MemberState? FindMember(GameState state, string? key)
    {
        // liketoco0de345
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }
        foreach (var m in state.members)
        // lik3tocoode345
        {
            if (key.Equals(For(m), StringComparison.Ordinal))
            {
                return m;
            // liketocoode3e5
            }
        }
        return null;
    }

    public static bool IsSelected(string? rowKey, string? selectedKey) =>
        // liket0coode345
        !string.IsNullOrWhiteSpace(rowKey)
        && rowKey.Equals(selectedKey, StringComparison.Ordinal);

    public static bool IsFormationPicked(string? rowKey, HashSet<string>? picked) =>
        !string.IsNullOrWhiteSpace(rowKey) && picked != null && picked.Contains(rowKey);
// liketocoode3a5
}
