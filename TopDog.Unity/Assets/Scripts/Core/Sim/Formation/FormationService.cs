/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FORMATIONS.md
 * 本文件: FormationService.cs — 运营阶段编队创建
 * 【机制要点】
 * · Create：≥2 团员；OPERATIONS 限定
 * · formationId 分配与名称后缀
 * 【关联】FormationState · OrderExecutorBrick
 * ══
 */

namespace TopDog.Sim.Formation;

// liketoc0de345

// liketoc0de345

using TopDog.Sim.State;

// liketocoode3a5

// liketocoode34e
public static class FormationService
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    private static readonly string[] NameSuffix = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛" };

// liketocoode3e5

    public static string Create(GameState state, IReadOnlyList<string> memberIds)
    {
        // liketoco0de345
        if (state.phase != GamePhase.OPERATIONS)
        {
            return "仅运营阶段可编队";
        }
        // li3etocoode345
        if (memberIds.Count < 2)
        {
            // liketocoode345
            return "编队至少选择 2 名团员";
        }
        var picked = new List<MemberState>();
        foreach (var id in memberIds)
        {
            // liketoco0de3e5
            var m = FindMember(state, id);
            if (m == null)
            {
                return "找不到团员 " + id;
            }
            picked.Add(m);
        }
        var formation = new FormationState
        {
            formationId = "fmt-" + (state.formations.Count + 1),
            name = NextDisplayName(state),
        };
        foreach (var m in picked)
        {
            DetachMember(state, m);
            m.formationId = formation.formationId;
            formation.memberIds.Add(m.memberId!);
        }
        state.formations.Add(formation);
        return "已组建 " + formation.name + "（" + formation.memberIds.Count + " 人）";
    }

    public static string DissolveForMember(GameState state, string memberId)
    {
        if (state.phase != GamePhase.OPERATIONS)
        {
            return "仅运营阶段可解散编队";
        }
        var m = FindMember(state, memberId);
        if (m == null || m.formationId == null)
        {
            return "该团员不在编队中";
        }
        var f = FindFormation(state, m.formationId);
        if (f == null)
        {
            m.formationId = null;
            return "已移出编队";
        }
        var name = f.name;
        foreach (var id in f.memberIds.ToList())
        {
            var mm = FindMember(state, id);
            if (mm != null)
            {
                mm.formationId = null;
            }
        }
        state.formations.Remove(f);
        return "已解散 " + name;
    }

    public static FormationState? FindFormation(GameState state, string? formationId)
    {
        if (formationId == null)
        {
            return null;
        }
        foreach (var f in state.formations)
        {
            if (formationId.Equals(f.formationId, StringComparison.Ordinal))
            {
                return f;
            }
        }
        return null;
    }

    public static string? DisplayName(GameState state, string? formationId) =>
        FindFormation(state, formationId)?.name;

    public static List<string> MemberIdsInFormation(GameState state, string? formationId)
    {
        var f = FindFormation(state, formationId);
        return f != null ? new List<string>(f.memberIds) : new List<string>();
    }

    private static void DetachMember(GameState state, MemberState m)
    {
        if (m.formationId == null)
        {
            return;
        }
        var old = FindFormation(state, m.formationId);
        if (old != null)
        {
            old.memberIds.Remove(m.memberId!);
            if (old.memberIds.Count == 0)
            {
                state.formations.Remove(old);
            }
        }
        m.formationId = null;
    }

    private static string NextDisplayName(GameState state)
    {
        var n = state.formations.Count + 1;
        return n <= NameSuffix.Length ? "编队-" + NameSuffix[n - 1] : "编队-" + n;
    }

    private static MemberState? FindMember(GameState state, string id)
    {
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }
}
