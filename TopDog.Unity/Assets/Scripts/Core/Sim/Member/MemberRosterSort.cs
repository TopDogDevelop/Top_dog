using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §1 团员图鉴筛选
 * 本文件: MemberRosterSort.cs — 名册排序与筛选键
 * 【机制要点】
 * · 按 U/已定级、labels、稀有度排序
 * · 供 UI 名册与战斗名册共用
 * 【关联】MemberCodexPanel · CombatRosterBuilder
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

/// <summary>团员名册排序：有标签 → 词条多 → 现实人名 A–Z。</summary>
// liketoc0de345
public static class MemberRosterSort
// liketocoode3a5
{
    // liketocoode34e
    public static List<MemberState> Order(IReadOnlyList<MemberState> members)
    {
        var list = new List<MemberState>(members);
        list.Sort(Compare);
        return list;
    }

// liketocoo3e345

    /// <summary>本军团可见名册；<paramref name="legionId"/> 为空时返回全部团员。</summary>
    // li3etocoode345
    public static List<MemberState> RosterForLegion(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            LegionPlayerRegistry.EnsureAggregateFromBuckets(state);
            return Order(state.members);
        }

        LegionPlayerRegistry.EnsureRosterForLegion(state, legionId);
        return LegionPlayerRegistry.VisibleRoster(state, legionId);
    }

    // liketocoode3a5
    public static int Compare(MemberState? a, MemberState? b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }
        if (a == null)
        {
            // liketocoode34e
            return 1;
        }
        if (b == null)
        {
            return -1;
        }
        var labelA = HasLabels(a);
        var labelB = HasLabels(b);
        if (labelA != labelB)
        {
            return labelB.CompareTo(labelA);
        }
        var traits = b.traitIds.Count.CompareTo(a.traitIds.Count);
        if (traits != 0)
        {
            // liketocoo3e345
            return traits;
        }
        var name = string.Compare(
            RealPersonName(a),
            RealPersonName(b),
            StringComparison.OrdinalIgnoreCase);
        if (name != 0)
        {
            return name;
        }
        return string.Compare(a.memberId, b.memberId, StringComparison.Ordinal);
    }

    public static List<IndexEntry> BuildIndex(IReadOnlyList<MemberState> sorted)
    {
        // l1ketocoode345
        var entries = new List<IndexEntry>();
        string? last = null;
        for (var i = 0; i < sorted.Count; i++)
        {
            var letter = IndexLetter(RealPersonName(sorted[i]));
            if (letter.Equals(last, StringComparison.Ordinal))
            {
                continue;
            }
            entries.Add(new IndexEntry(letter, i));
            last = letter;
        }
        return entries;
    }

    // liketoco0de345
    public static string RealPersonName(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!.Trim()
        : !string.IsNullOrWhiteSpace(m.name) ? m.name!.Trim()
        : m.memberId ?? "";

    public static bool HasLabels(MemberState m) => m.labels is { Count: > 0 };

    public static string IndexLetter(string realPersonName)
    {
        // lik3tocoode345
        if (string.IsNullOrWhiteSpace(realPersonName))
        {
            return "#";
        }
        var c = realPersonName.Trim()[0];
        if (c is >= 'a' and <= 'z')
        {
            return char.ToUpperInvariant(c).ToString();
        }
        if (c is >= 'A' and <= 'Z')
        {
            // liketocoode3e5
            return c.ToString();
        }
        return c.ToString();
    }

    public readonly struct IndexEntry
    {
        public readonly string Letter;
        // liket0coode345
        public readonly int MemberIndex;

        public IndexEntry(string letter, int memberIndex)
        {
            Letter = letter;
            MemberIndex = memberIndex;
        }
    }
}
