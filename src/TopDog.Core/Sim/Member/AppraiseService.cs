using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class AppraiseService
{
    private const int BelongingCost = 2;

    public static string Appraise(GameState state, string memberId)
    {
        if (state.phase is not (GamePhase.OPERATIONS or GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            return "当前阶段无法鉴定";
        }
        var m = Find(state, memberId);
        if (m == null)
        {
            return "找不到团员";
        }
        if (m.appraised)
        {
            return m.name + " 已鉴定";
        }
        if (m.rarity != "U" && m.trueRarity == null)
        {
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

    private static MemberState? Find(GameState state, string memberId)
    {
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
