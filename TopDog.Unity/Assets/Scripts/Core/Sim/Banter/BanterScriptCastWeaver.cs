using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>剧本编织：抽 cast → 开场专属「说台词」→ 退出剧本者补抽槽位 → 剧本行仅念成稿。</summary>
public static class BanterScriptCastWeaver
{
    public static void PrepareScriptCast(GameState state, MemberBanterRuntimeState rt, Random rng)
    {
        rt.idleRosterSpeakerSlots.Clear();
        rt.idleCastDrawOrder.Clear();
        rt.idleScriptOptOutMemberIds.Clear();
        rt.idleDynamicContext = new BanterIdleDynamicContext();

        var pool = BanterEligibleSpeakers.List(state);
        if (pool.Count == 0)
        {
            return;
        }

        var picks = PickDistinctIdentities(pool, Math.Min(BanterRosterSpeakers.SlotCount, pool.Count), rng);
        for (var i = 0; i < picks.Count; i++)
        {
            var memberId = picks[i].memberId ?? "";
            rt.idleRosterSpeakerSlots[i + 1] = memberId;
            rt.idleCastDrawOrder.Add(memberId);
        }

        BanterDiagnosticLog.Log(
            $"weave cast order={string.Join(">", rt.idleCastDrawOrder)} identities={string.Join(">", picks.ConvertAll(IdentityCodes.Of))}");
    }

    /// <summary>按 cast 顺序处理开场专属；返回剧本正文起始时间。</summary>
    public static float AppendOpeningExclusives(
        GameState state,
        MemberBanterRuntimeState rt,
        IReadOnlyList<MemberState> pool,
        string groupId,
        float roundStartSec,
        Random rng,
        List<BanterPlannedEmit> queue,
        string? firstScriptLineText)
    {
        var cursor = roundStartSec;
        var openingStartCount = queue.Count;

        foreach (var memberId in rt.idleCastDrawOrder)
        {
            if (string.IsNullOrWhiteSpace(memberId))
            {
                continue;
            }

            var member = FindMember(state, memberId);
            if (member == null || !BanterPersonalExclusiveLines.CanUsePersonalExclusive(member))
            {
                continue;
            }

            var slot = FindSlotForMember(rt, memberId);
            if (!BanterPersonalExclusiveLines.TryRollOpeningExclusive(member, rt, rng, out var exclusiveText))
            {
                BanterDiagnosticLog.Log(
                    $"weave opening slot={slot} mid={memberId} mode=script-participant");
                continue;
            }

            rt.idleMandatoryLineSpokenIdentities.Add(IdentityCodes.Of(member));
            rt.idleScriptOptOutMemberIds.Add(memberId);
            BanterDiagnosticLog.Log(
                $"weave opening slot={slot} mid={memberId} mode=exclusive-opt-out text={Truncate(exclusiveText)}");

            cursor = AppendMultiboxEmits(
                queue,
                pool,
                memberId,
                exclusiveText,
                groupId,
                cursor,
                rng);

            if (slot > 0)
            {
                BackfillSlot(state, rt, pool, slot, rng);
            }
        }

        if (queue.Count > openingStartCount)
        {
            cursor += BanterIdleTiming.GapBeforeNextMessage(firstScriptLineText);
        }

        return cursor;
    }

    private static void BackfillSlot(
        GameState state,
        MemberBanterRuntimeState rt,
        IReadOnlyList<MemberState> pool,
        int slot,
        Random rng)
    {
        var excludedMemberIds = new HashSet<string>(StringComparer.Ordinal);
        var excludedIdentities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var opted in rt.idleScriptOptOutMemberIds)
        {
            excludedMemberIds.Add(opted);
            TryAddIdentity(pool, opted, excludedIdentities);
        }

        foreach (var kv in rt.idleRosterSpeakerSlots)
        {
            if (kv.Key == slot)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(kv.Value))
            {
                excludedMemberIds.Add(kv.Value);
                TryAddIdentity(pool, kv.Value, excludedIdentities);
            }
        }

        var candidates = new List<MemberState>();
        foreach (var m in pool)
        {
            if (string.IsNullOrWhiteSpace(m.memberId))
            {
                continue;
            }

            if (excludedMemberIds.Contains(m.memberId))
            {
                continue;
            }

            if (excludedIdentities.Contains(IdentityCodes.Of(m)))
            {
                continue;
            }

            candidates.Add(m);
        }

        if (candidates.Count == 0)
        {
            rt.idleRosterSpeakerSlots.Remove(slot);
            BanterDiagnosticLog.Log($"weave backfill slot={slot} failed=no-candidate");
            return;
        }

        var pick = candidates[rng.Next(candidates.Count)];
        rt.idleRosterSpeakerSlots[slot] = pick.memberId ?? "";
        BanterDiagnosticLog.Log(
            $"weave backfill slot={slot} mid={pick.memberId} name={pick.name}");
    }

    private static float AppendMultiboxEmits(
        List<BanterPlannedEmit> queue,
        IReadOnlyList<MemberState> pool,
        string primaryMemberId,
        string text,
        string groupId,
        float cursorSec,
        Random rng)
    {
        var speakerIds = BanterMultiboxSync.CollectSyncSpeakers(pool, primaryMemberId, rng);
        for (var i = 0; i < speakerIds.Count; i++)
        {
            queue.Add(new BanterPlannedEmit
            {
                EmitAtSec = cursorSec,
                MemberId = speakerIds[i],
                Text = text,
                Channel = "idle",
                GroupId = groupId,
            });
            if (i < speakerIds.Count - 1)
            {
                cursorSec += BanterIdleTiming.MultiboxEchoGapSec;
            }
        }

        return cursorSec;
    }

    private static int FindSlotForMember(MemberBanterRuntimeState rt, string memberId)
    {
        foreach (var kv in rt.idleRosterSpeakerSlots)
        {
            if (memberId.Equals(kv.Value, StringComparison.Ordinal))
            {
                return kv.Key;
            }
        }

        return 0;
    }

    /// <summary>每槽一名团员；同一 identityCode（现实人）仅占一个槽，避免多开号互相对话。</summary>
    private static List<MemberState> PickDistinctIdentities(IReadOnlyList<MemberState> pool, int count, Random rng)
    {
        var work = new List<MemberState>(pool);
        var result = new List<MemberState>(count);
        for (var i = 0; i < count && work.Count > 0; i++)
        {
            var idx = rng.Next(work.Count);
            var pick = work[idx];
            result.Add(pick);
            var identity = IdentityCodes.Of(pick);
            work.RemoveAll(m => identity.Equals(IdentityCodes.Of(m), StringComparison.Ordinal));
        }

        return result;
    }

    private static void TryAddIdentity(
        IReadOnlyList<MemberState> pool,
        string memberId,
        ISet<string> identities)
    {
        foreach (var m in pool)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                identities.Add(IdentityCodes.Of(m));
                return;
            }
        }
    }

    private static MemberState? FindMember(GameState state, string memberId)
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

    private static string Truncate(string text, int max = 48) =>
        text.Length <= max ? text : text[..max] + "…";
}
