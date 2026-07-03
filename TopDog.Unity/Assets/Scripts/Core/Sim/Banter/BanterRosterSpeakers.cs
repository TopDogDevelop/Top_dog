using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>剧本闲聊：@1/@2/@3 为剧本角色槽，开局从发言人池随机抽 3 名（与 memberId 排序无关）。</summary>
public static class BanterRosterSpeakers
{
    public const int SlotCount = 3;

    public static bool IsSlot(string? memberId) =>
        !string.IsNullOrWhiteSpace(memberId)
        && memberId.StartsWith('@')
        && int.TryParse(memberId.AsSpan(1), out var slot)
        && slot is >= 1 and <= 9;

    public static void PrepareRound(GameState state, MemberBanterRuntimeState rt, Random rng) =>
        BanterScriptCastWeaver.PrepareScriptCast(state, rt, rng);

    public static string? ResolveSlot(MemberBanterRuntimeState rt, string memberId)
    {
        if (!IsSlot(memberId) || !int.TryParse(memberId.AsSpan(1), out var slot))
        {
            return null;
        }

        return rt.idleRosterSpeakerSlots.TryGetValue(slot, out var resolved)
            && !string.IsNullOrWhiteSpace(resolved)
            ? resolved
            : null;
    }
}
