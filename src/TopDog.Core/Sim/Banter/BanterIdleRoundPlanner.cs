using TopDog.Content.Banter;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>闲聊整轮预排：编织 cast/开场专属 → 剧本成稿 → 节奏排期 → 标准输出队列。</summary>
public static class BanterIdleRoundPlanner
{
    public static List<BanterPlannedEmit> Plan(
        GameState state,
        MemberBanterRuntimeState rt,
        IReadOnlyList<IdleBanterLine> lines,
        string groupId,
        float roundStartSec,
        Random rng)
    {
        var queue = new List<BanterPlannedEmit>();
        var eligiblePool = BanterEligibleSpeakers.List(state);
        var usesSlots = GroupUsesRosterSlots(lines);
        var cursorSec = roundStartSec;

        if (usesSlots)
        {
            var firstLine = FindLineBySeq(lines, 1);
            cursorSec = BanterScriptCastWeaver.AppendOpeningExclusives(
                state,
                rt,
                eligiblePool,
                groupId,
                roundStartSec,
                rng,
                queue,
                firstLine?.Text);
        }

        for (var seq = 1; seq <= rt.idleGroupLineCount; seq++)
        {
            var line = FindLineBySeq(lines, seq);
            if (line == null)
            {
                continue;
            }

            var speakerId = ResolveSpeaker(state, rt, line, eligiblePool, rng, usesSlots);
            if (speakerId == null)
            {
                cursorSec += BanterIdleTiming.GapBeforeNextMessage(FindLineBySeq(lines, seq + 1)?.Text);
                continue;
            }

            var member = FindMember(state, speakerId);
            if (member == null)
            {
                cursorSec += BanterIdleTiming.GapBeforeNextMessage(FindLineBySeq(lines, seq + 1)?.Text);
                continue;
            }

            string text;
            bool usedExclusive;
            if (usesSlots && BanterRosterSpeakers.IsSlot(line.MemberId))
            {
                text = BanterScriptTextComposer.ComposeScriptLine(state, rt, line.Text, rng);
                usedExclusive = false;
            }
            else if (!BanterScriptTextComposer.TryComposeIdleLine(
                         state,
                         rt,
                         member,
                         line.Text,
                         rng,
                         out text,
                         out usedExclusive))
            {
                BanterDiagnosticLog.Log(
                    $"plan skip seq={seq} group={groupId} mid={speakerId} reason=exclusive-miss");
                cursorSec += BanterIdleTiming.GapBeforeNextMessage(FindLineBySeq(lines, seq + 1)?.Text);
                continue;
            }

            BanterDiagnosticLog.Log(
                $"plan seq={seq} group={groupId} mid={speakerId} final={TruncateForLog(text)}");

            var allowMultibox = string.IsNullOrWhiteSpace(line.SplitMsgId);
            var speakerIds = allowMultibox
                ? BanterMultiboxSync.CollectSyncSpeakers(eligiblePool, speakerId, rng)
                : new List<string> { speakerId };

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

            rt.idleLastSpeakerMemberId = speakerIds[0];
            rt.idleLastSplitMsgId = line.SplitMsgId;
            if (usedExclusive)
            {
                rt.idleMandatoryLineSpokenIdentities.Add(IdentityCodes.Of(member));
            }

            cursorSec += BanterIdleTiming.GapBeforeNextMessage(FindLineBySeq(lines, seq + 1)?.Text);
        }

        BanterDiagnosticLog.Log(
            $"plan-done group={groupId} emits={queue.Count} span={(queue.Count > 0 ? queue[^1].EmitAtSec - roundStartSec : 0f):0.###}s");
        return queue;
    }

    private static bool GroupUsesRosterSlots(IReadOnlyList<IdleBanterLine> lines)
    {
        foreach (var line in lines)
        {
            if (BanterRosterSpeakers.IsSlot(line.MemberId))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ResolveSpeaker(
        GameState state,
        MemberBanterRuntimeState rt,
        IdleBanterLine line,
        IReadOnlyList<MemberState> eligiblePool,
        Random rng,
        bool usesSlots)
    {
        if (BanterRosterSpeakers.IsSlot(line.MemberId))
        {
            var slotSpeaker = BanterRosterSpeakers.ResolveSlot(rt, line.MemberId);
            return slotSpeaker != null && FindMember(state, slotSpeaker) != null ? slotSpeaker : null;
        }

        if (!string.Equals(line.MemberId, "*", StringComparison.Ordinal))
        {
            return FindMember(state, line.MemberId) != null ? line.MemberId : null;
        }

        if (!usesSlots)
        {
            if (eligiblePool.Count == 0)
            {
                return null;
            }

            var picked = BanterSpeakerPicker.Pick(
                eligiblePool,
                rt.idleLastSpeakerMemberId,
                rt.idleLastSplitMsgId,
                line.SplitMsgId,
                rng,
                rt.idleMandatoryLineSpokenIdentities);
            return picked?.memberId;
        }

        return null;
    }

    private static IdleBanterLine? FindLineBySeq(IReadOnlyList<IdleBanterLine> lines, int seq)
    {
        foreach (var line in lines)
        {
            if (line.Seq == seq)
            {
                return line;
            }
        }

        return null;
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

    private static string TruncateForLog(string text, int maxChars = 72) =>
        text.Length <= maxChars ? text : text[..maxChars] + "…";
}
