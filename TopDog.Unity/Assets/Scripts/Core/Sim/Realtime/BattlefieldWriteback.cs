using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class BattlefieldWriteback
{
    public static string Apply(GameState state, BattlefieldState bf, CombatQueueEntry? entry)
    {
        if (bf.winReason == "defend_no_attack_15m")
        {
            return "建筑守卫成功 · 15 分钟未受攻击 @ " + Label(bf);
        }
        if (bf.winReason == "legion_fort_phase_end")
        {
            return "军堡攻城阶段结束 · 需再次约战 @ " + Label(bf);
        }
        if (bf.winnerSide == UnitSide.ENEMY)
        {
            StripRandomFriendlies(state, entry, Math.Max(1, entry?.friendlyMemberIds.Count / 2 ?? 1));
            return "实时交战失利 @ " + Label(bf);
        }
        if (bf.winnerSide == UnitSide.FRIENDLY)
        {
            return "实时交战胜利 @ " + Label(bf);
        }
        return "实时交战僵持 @ " + Label(bf);
    }

    private static void StripRandomFriendlies(GameState state, CombatQueueEntry? entry, int count)
    {
        if (entry == null)
        {
            return;
        }
        var stripped = 0;
        foreach (var id in entry.friendlyMemberIds)
        {
            if (stripped >= count)
            {
                break;
            }
            var m = FindMember(state, id);
            if (m?.equippedHullId != null)
            {
                m.equippedHullId = null;
                stripped++;
            }
        }
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

    private static string Label(BattlefieldState bf) =>
        (bf.systemId ?? "?") + (bf.subLocation != null ? " · " + bf.subLocation : "");
}
