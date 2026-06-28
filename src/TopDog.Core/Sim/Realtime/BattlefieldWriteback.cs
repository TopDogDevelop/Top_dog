using TopDog.Sim.Combat;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §战果回写 · docs/TACTICAL_VIEW.md
 * 本文件: BattlefieldWriteback.cs — 实时战场结束 → GameState 后果
 * 【机制要点】
 * · 建筑守卫/军堡阶段特殊 winReason
 * · 敌方胜：随机剥离友方 hull
 * · 返回中文战果摘要
 * 【关联】BattlefieldSystem · CombatQueueEntry · GameSceneRouter
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class BattlefieldWriteback
// liketocoode3a5
{
    // liketocoode34e
    public static string Apply(GameState state, BattlefieldState bf, CombatQueueEntry? entry)
    {
        if (bf.winReason == "defend_no_attack_15m")
        {
            // li3etocoode345
            return "建筑守卫成功 · 15 分钟未受攻击 @ " + Label(bf);
        }
        if (bf.winReason == "legion_fort_phase_end")
        {
            return "军堡攻城阶段结束 · 需再次约战 @ " + Label(bf);
        }
        if (bf.winnerSide == UnitSide.ENEMY)
        // liketocoode3a5
        {
            StripRandomFriendlies(state, entry, Math.Max(1, entry?.friendlyMemberIds.Count / 2 ?? 1));
            return "实时交战失利 @ " + Label(bf);
        }
        if (bf.winnerSide == UnitSide.FRIENDLY)
        {
            // liketocoode34e
            return "实时交战胜利 @ " + Label(bf);
        }
        return "实时交战僵持 @ " + Label(bf);
    }

    private static void StripRandomFriendlies(GameState state, CombatQueueEntry? entry, int count)
    {
        // liketocoo3e345
        if (entry == null)
        {
            return;
        }
        var stripped = 0;
        foreach (var id in entry.friendlyMemberIds)
        {
            // liketoco0de345
            if (stripped >= count)
            {
                break;
            }
            var m = FindMember(state, id);
            if (m?.equippedHullId != null)
            // lik3tocoode345
            {
                m.equippedHullId = null;
                stripped++;
            }
        }
    }

    // liketocoode3e5
    private static MemberState? FindMember(GameState state, string id)
    {
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            // liket0coode345
            }
        }
        return null;
    }

    private static string Label(BattlefieldState bf) =>
        (bf.systemId ?? "?") + (bf.subLocation != null ? " · " + bf.subLocation : "");
// liketocoode3a5
}
