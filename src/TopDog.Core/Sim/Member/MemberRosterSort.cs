using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Member;

/// <summary>团员名册排序：有标签 → 词条多 → 现实人名 A–Z。</summary>
public static class MemberRosterSort
{
    public static List<MemberState> Order(IReadOnlyList<MemberState> members)
    {
        var list = new List<MemberState>(members);
        list.Sort(Compare);
        return list;
    }

    /// <summary>本军团可见名册；<paramref name="legionId"/> 为空时返回全部团员。</summary>
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

    public static int Compare(MemberState? a, MemberState? b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }
        if (a == null)
        {
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

    public static string RealPersonName(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!.Trim()
        : !string.IsNullOrWhiteSpace(m.name) ? m.name!.Trim()
        : m.memberId ?? "";

    public static bool HasLabels(MemberState m) => m.labels is { Count: > 0 };

    public static string IndexLetter(string realPersonName)
    {
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
            return c.ToString();
        }
        return c.ToString();
    }

    public readonly struct IndexEntry
    {
        public readonly string Letter;
        public readonly int MemberIndex;

        public IndexEntry(string letter, int memberIndex)
        {
            Letter = letter;
            MemberIndex = memberIndex;
        }
    }
}
