using TopDog.Content.Banter;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

public sealed class MemberBanterService
{
    public const float ReactiveCooldownSec = 2f;

    private readonly BanterCatalog _catalog;
    private readonly Random _rng;

    public MemberBanterService(BanterCatalog catalog, int seed = 1)
    {
        _catalog = catalog;
        _rng = new Random(seed);
    }

    public BanterCatalog Catalog => _catalog;

    public void Tick(GameState state, float dtSec, float simTimeSec)
    {
        EnsureRuntime(state);
        TickReactiveCooldowns(state, dtSec);
        DrainSignals(state, simTimeSec);
        TickIdle(state, simTimeSec);
    }

    public void TryReactive(GameState state, string eventKey, string memberId, float simTimeSec)
    {
        if (!CanEmitReactive(state, memberId))
        {
            return;
        }

        var line = PickReactiveLine(memberId, eventKey);
        if (line == null)
        {
            return;
        }

        EmitBanter(
            state,
            memberId,
            line.Text,
            "reactive",
            eventKey,
            null,
            simTimeSec,
            allowMultiboxSync: true);
        state.banterReactiveCooldownSec[memberId] = ReactiveCooldownSec;
    }

    private void DrainSignals(GameState state, float simTimeSec)
    {
        var batch = new List<BanterSignal>();
        BanterSignalHub.Drain(batch);
        foreach (var sig in batch)
        {
            TryReactive(state, sig.EventKey, sig.MemberId, simTimeSec);
        }
    }

    private void TickIdle(GameState state, float simTimeSec)
    {
        var rt = state.banterRuntime!;
        DrainIdleEmitQueue(state, rt, simTimeSec);

        if (rt.idleEmitQueueIndex < rt.idleEmitQueue.Count)
        {
            rt.idleNextEmitSec = rt.idleEmitQueue[rt.idleEmitQueueIndex].EmitAtSec;
            return;
        }

        if (rt.idleEmitQueue.Count > 0)
        {
            var lastAt = rt.idleEmitQueue[^1].EmitAtSec;
            FinishIdleRoundSchedule(rt, lastAt + BanterIdleTiming.RoundGapSec);
            return;
        }

        if (TryResumePrestartedIdleRound(state, rt, simTimeSec))
        {
            return;
        }

        var eligibleCount = BanterEligibleSpeakers.List(state).Count;
        // 空团不空转开组：否则 RoundGap 被「空轮」占掉，招新入队后还要再空等一轮才出左栏伴聊。
        if (eligibleCount == 0)
        {
            rt.idleLastEligibleSpeakerCount = 0;
            if (simTimeSec >= rt.idleNextEmitSec)
            {
                rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.EmptyRosterPollSec;
            }

            return;
        }

        if (rt.idleLastEligibleSpeakerCount == 0)
        {
            rt.idleNextEmitSec = Math.Min(rt.idleNextEmitSec, simTimeSec);
        }

        rt.idleLastEligibleSpeakerCount = eligibleCount;

        if (simTimeSec < rt.idleNextEmitSec)
        {
            return;
        }

        if (!TryStartIdleGroup(state, rt, simTimeSec))
        {
            rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.RoundGapSec;
            return;
        }

        PlanIdleRound(state, rt, simTimeSec);
        DrainIdleEmitQueue(state, rt, simTimeSec);
        if (rt.idleEmitQueueIndex < rt.idleEmitQueue.Count)
        {
            rt.idleNextEmitSec = rt.idleEmitQueue[rt.idleEmitQueueIndex].EmitAtSec;
        }
        else if (rt.idleEmitQueue.Count > 0)
        {
            var lastAt = rt.idleEmitQueue[^1].EmitAtSec;
            FinishIdleRoundSchedule(rt, lastAt + BanterIdleTiming.RoundGapSec);
        }
        else
        {
            FinishIdleRoundSchedule(rt, simTimeSec + BanterIdleTiming.RoundGapSec);
        }
    }

    private bool TryResumePrestartedIdleRound(GameState state, MemberBanterRuntimeState rt, float simTimeSec)
    {
        if (string.IsNullOrWhiteSpace(rt.idleGroupId)
            || rt.idleEmitQueue.Count > 0
            || rt.idleNextSeq > rt.idleGroupLineCount)
        {
            return false;
        }

        if (!_catalog.IdleGroups.TryGetValue(rt.idleGroupId, out var lines) || lines.Count == 0)
        {
            rt.idleGroupId = null;
            return false;
        }

        PlanIdleRound(state, rt, simTimeSec);
        DrainIdleEmitQueue(state, rt, simTimeSec);
        if (rt.idleEmitQueueIndex < rt.idleEmitQueue.Count)
        {
            rt.idleNextEmitSec = rt.idleEmitQueue[rt.idleEmitQueueIndex].EmitAtSec;
            return true;
        }

        if (rt.idleEmitQueue.Count > 0)
        {
            var lastAt = rt.idleEmitQueue[^1].EmitAtSec;
            FinishIdleRoundSchedule(rt, lastAt + BanterIdleTiming.RoundGapSec);
        }
        else
        {
            FinishIdleRoundSchedule(rt, simTimeSec + BanterIdleTiming.RoundGapSec);
        }

        return true;
    }

    private static void FinishIdleRoundSchedule(MemberBanterRuntimeState rt, float nextRoundAtSec)
    {
        rt.idleGroupId = null;
        rt.idleNextSeq = 1;
        rt.idleEmitQueue.Clear();
        rt.idleEmitQueueIndex = 0;
        rt.idleNextEmitSec = nextRoundAtSec;
    }

    private void PlanIdleRound(GameState state, MemberBanterRuntimeState rt, float roundStartSec)
    {
        if (string.IsNullOrWhiteSpace(rt.idleGroupId)
            || !_catalog.IdleGroups.TryGetValue(rt.idleGroupId, out var lines)
            || lines.Count == 0)
        {
            rt.idleEmitQueue.Clear();
            rt.idleEmitQueueIndex = 0;
            return;
        }

        BanterDiagnosticLog.Log(
            $"round-start group={rt.idleGroupId} lines={rt.idleGroupLineCount} t={roundStartSec:0.###}");
        var roundRng = BanterRng.ForIdleRound(_rng, rt, roundStartSec);
        rt.idleEmitQueue = BanterIdleRoundPlanner.Plan(
            state,
            rt,
            lines,
            rt.idleGroupId,
            roundStartSec,
            roundRng);
        rt.idleEmitQueueIndex = 0;
        rt.idleNextSeq = rt.idleGroupLineCount + 1;
    }

    private static void DrainIdleEmitQueue(GameState state, MemberBanterRuntimeState rt, float simTimeSec)
    {
        while (rt.idleEmitQueueIndex < rt.idleEmitQueue.Count
               && simTimeSec >= rt.idleEmitQueue[rt.idleEmitQueueIndex].EmitAtSec)
        {
            var planned = rt.idleEmitQueue[rt.idleEmitQueueIndex++];
            BanterCompanionOutput.EmitLine(
                state,
                planned.EmitAtSec,
                planned.MemberId,
                planned.Text,
                planned.Channel,
                planned.EventKey,
                planned.GroupId);
        }
    }

    private static void ResetIdleRound(MemberBanterRuntimeState rt)
    {
        rt.idleGroupId = null;
        rt.idleNextSeq = 1;
        rt.idleLastSpeakerMemberId = null;
        rt.idleLastSplitMsgId = null;
        rt.idleMandatoryLineSpokenIdentities.Clear();
        rt.idleRosterSpeakerSlots.Clear();
        rt.idleCastDrawOrder.Clear();
        rt.idleScriptOptOutMemberIds.Clear();
        rt.idleDynamicContext = null;
        rt.idleEmitQueue.Clear();
        rt.idleEmitQueueIndex = 0;
    }

    private bool TryStartIdleGroup(GameState state, MemberBanterRuntimeState rt, float simTimeSec)
    {
        if (!string.IsNullOrWhiteSpace(rt.idleGroupId) && rt.idleNextSeq <= rt.idleGroupLineCount)
        {
            return false;
        }

        var eligible = EligibleIdleGroups(state);
        if (eligible.Count == 0)
        {
            return false;
        }

        var pick = eligible[_rng.Next(eligible.Count)];
        rt.banterRoundSalt++;
        rt.idleGroupId = pick;
        rt.idleNextSeq = 1;
        rt.idleGroupLineCount = _catalog.IdleGroups[pick].Count;
        rt.idleLastSpeakerMemberId = null;
        rt.idleLastSplitMsgId = null;
        rt.idleMandatoryLineSpokenIdentities.Clear();
        rt.idleRosterSpeakerSlots.Clear();
        rt.idleCastDrawOrder.Clear();
        rt.idleScriptOptOutMemberIds.Clear();
        rt.idleDynamicContext = null;
        rt.idleEmitQueue.Clear();
        rt.idleEmitQueueIndex = 0;
        if (GroupUsesRosterSlots(_catalog.IdleGroups[pick]))
        {
            BanterRosterSpeakers.PrepareRound(state, rt, _rng);
        }

        return true;
    }

    private List<string> EligibleIdleGroups(GameState state)
    {
        var result = new List<string>();
        foreach (var kv in _catalog.IdleGroups)
        {
            var count = kv.Value.Count;
            if (count < 3 || count > 5)
            {
                continue;
            }

            result.Add(kv.Key);
        }

        return result;
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

    private string ResolveBanterText(
        GameState state,
        string memberId,
        string catalogText,
        bool idleRound = false)
    {
        return ResolveBanterText(state, memberId, catalogText, idleRound, out _);
    }

    private string ResolveBanterText(
        GameState state,
        string memberId,
        string catalogText,
        bool idleRound,
        out bool usedExclusive)
    {
        usedExclusive = false;
        var member = FindMember(state, memberId);
        if (member == null)
        {
            return catalogText;
        }

        var rt = state.banterRuntime!;
        if (idleRound)
        {
            catalogText = BanterDynamicTextResolver.Resolve(state, rt, catalogText, _rng);
            BanterPersonalExclusiveLines.TryResolveIdleEmitText(
                member,
                rt,
                catalogText,
                _rng,
                out var text,
                out usedExclusive);
            return text;
        }

        return BanterPersonalExclusiveLines.ResolveForReactive(member, rt, catalogText, _rng);
    }

    private ReactiveBanterLine? PickReactiveLine(string memberId, string eventKey)
    {
        var cfg = ResolveConfig(memberId);
        var pool = new List<ReactiveBanterLine>();
        pool.AddRange(_catalog.ReactivePersonal.Where(l =>
            l.EventKey == eventKey
            && (l.MemberId == memberId || l.MemberId == "*")));
        if (cfg.ReactiveUseCommon)
        {
            pool.AddRange(_catalog.ReactiveCommon.Where(l => l.EventKey == eventKey));
        }

        if (pool.Count == 0)
        {
            return null;
        }

        return WeightedPick(pool);
    }

    private MemberBanterConfigRow ResolveConfig(string memberId) =>
        _catalog.MemberConfig.TryGetValue(memberId, out var row)
            ? row
            : new MemberBanterConfigRow { MemberId = memberId };

    private ReactiveBanterLine? WeightedPick(List<ReactiveBanterLine> pool)
    {
        var total = 0;
        foreach (var line in pool)
        {
            total += Math.Max(1, line.Weight);
        }

        if (total <= 0)
        {
            return null;
        }

        var roll = _rng.Next(total);
        foreach (var line in pool)
        {
            roll -= Math.Max(1, line.Weight);
            if (roll < 0)
            {
                return line;
            }
        }

        return pool[^1];
    }

    private static bool CanEmitReactive(GameState state, string memberId)
    {
        if (!state.banterReactiveCooldownSec.TryGetValue(memberId, out var remain))
        {
            return true;
        }

        return remain <= 0f;
    }

    private static void TickReactiveCooldowns(GameState state, float dtSec)
    {
        if (state.banterReactiveCooldownSec.Count == 0)
        {
            return;
        }

        var keys = state.banterReactiveCooldownSec.Keys.ToList();
        foreach (var key in keys)
        {
            var next = state.banterReactiveCooldownSec[key] - dtSec;
            if (next <= 0f)
            {
                state.banterReactiveCooldownSec.Remove(key);
            }
            else
            {
                state.banterReactiveCooldownSec[key] = next;
            }
        }
    }

    private List<string> EmitBanter(
        GameState state,
        string primaryMemberId,
        string catalogText,
        string channel,
        string? eventKey,
        string? groupId,
        float simTimeSec,
        bool allowMultiboxSync,
        string? resolvedText = null)
    {
        var text = resolvedText;
        if (text == null)
        {
            var member = FindMember(state, primaryMemberId);
            if (member != null)
            {
                var rt = state.banterRuntime!;
                text = BanterScriptTextComposer.ComposeReactiveLine(state, rt, member, catalogText, _rng);
            }
            else
            {
                text = catalogText;
            }
        }
        var pool = BanterEligibleSpeakers.List(state);
        var speakerIds = allowMultiboxSync
            ? BanterMultiboxSync.CollectSyncSpeakers(pool, primaryMemberId, _rng)
            : new List<string> { primaryMemberId };

        var emitAt = simTimeSec;
        for (var i = 0; i < speakerIds.Count; i++)
        {
            BanterCompanionOutput.EmitLine(
                state,
                emitAt,
                speakerIds[i],
                text,
                channel,
                eventKey,
                groupId);
            if (i < speakerIds.Count - 1)
            {
                emitAt += BanterIdleTiming.MultiboxEchoGapSec;
            }
        }

        return speakerIds;
    }

    private static void EnsureRuntime(GameState state)
    {
        state.banterRuntime ??= new MemberBanterRuntimeState();
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
}
