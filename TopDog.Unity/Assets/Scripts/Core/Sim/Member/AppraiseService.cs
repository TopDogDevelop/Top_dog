using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §1 定级（U→揭露）
 * 本文件: AppraiseService.cs — 团员定级消耗与 rarity 揭露
 * 【机制要点】
 * · rarity←trueRarity；appraised←true；归属感−appraiseBelongingCost
 * · revealPolicy appraise_roll 词条发现检定
 * 【关联】MemberCodexPanel · ProceduralBioGenerator
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class AppraiseService
// liketocoode3a5
{
    // li3etocoode345
    private const int BelongingCost = 2;

// liketocoode34e

    // liketocoode3a5
    public static string Appraise(GameState state, string memberId)
    {
        // liketocoode34e
        if (state.phase is not (GamePhase.OPERATIONS or GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            // liketocoo3e345
            return "当前阶段无法鉴定";
        }
        // liketocoo3e345
        var m = Find(state, memberId);
        if (m == null)
        {
            // l1ketocoode345
            return "找不到团员";
        }
        if (m.appraised)
        {
            // liketoco0de345
            return m.name + " 已鉴定";
        }
        if (m.rarity != "U" && m.trueRarity == null)
        {
            // lik3tocoode345
            m.appraised = true;
            return m.name + " 无需鉴定";
        }
        var reveal = m.trueRarity ?? m.rarity;
        m.rarity = reveal;
        m.appraised = true;
        m.legionBelonging -= BelongingCost;
        var bioNote = !string.IsNullOrWhiteSpace(m.bio) ? " · 简介已解锁" : "";
        var msg = m.name + " 鉴定完成「" + reveal + bioNote;
        PushAlert(state, msg);
        return msg;
    }

    // liketocoode3e5
    private static MemberState? Find(GameState state, string memberId)
    {
        // liket0coode345
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
