using TopDog.Content.Banter;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
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
        // 同步器多号复读视为一次发言，仅主发言人进入节流。
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
        if (simTimeSec < rt.idleNextEmitSec)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(rt.idleGroupId) || rt.idleNextSeq > rt.idleGroupLineCount)
        {
            if (!TryStartIdleGroup(state, rt))
            {
                rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.RoundGapSec;
                return;
            }
        }

        if (!_catalog.IdleGroups.TryGetValue(rt.idleGroupId!, out var lines) || lines.Count == 0)
        {
            rt.idleGroupId = null;
            rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.RoundGapSec;
            return;
        }

        var line = lines.Find(l => l.Seq == rt.idleNextSeq);
        if (line == null)
        {
            ResetIdleRound(rt);
            rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.RoundGapSec;
            return;
        }

        var speakerId = ResolveIdleSpeaker(state, rt, line);
        if (speakerId == null)
        {
            rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.RoundGapSec;
            return;
        }

        var member = FindMember(state, speakerId);
        var text = ResolveBanterText(state, speakerId, line.Text, idleRound: true, out var usedExclusive);
        var speakerIds = EmitBanter(
            state,
            speakerId,
            line.Text,
            "idle",
            null,
            rt.idleGroupId,
            simTimeSec,
            allowMultiboxSync: string.IsNullOrWhiteSpace(line.SplitMsgId),
            resolvedText: text);
        if (usedExclusive && member != null)
        {
            rt.idleMandatoryLineSpokenIdentities.Add(IdentityCodes.Of(member));
        }

        rt.idleLastSpeakerMemberId = speakerIds[0];
        rt.idleLastSplitMsgId = line.SplitMsgId;
        rt.idleNextSeq++;

        if (rt.idleNextSeq > rt.idleGroupLineCount)
        {
            ResetIdleRound(rt);
            rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.RoundGapSec;
        }
        else
        {
            var nextLine = lines.Find(l => l.Seq == rt.idleNextSeq);
            rt.idleNextEmitSec = simTimeSec + BanterIdleTiming.GapBeforeNextMessage(nextLine?.Text);
        }
    }

    private static void ResetIdleRound(MemberBanterRuntimeState rt)
    {
        rt.idleGroupId = null;
        rt.idleNextSeq = 1;
        rt.idleLastSpeakerMemberId = null;
        rt.idleLastSplitMsgId = null;
        rt.idleMandatoryLineSpokenIdentities.Clear();
    }

    private bool TryStartIdleGroup(GameState state, MemberBanterRuntimeState rt)
    {
        var eligible = EligibleIdleGroups(state);
        if (eligible.Count == 0)
        {
            return false;
        }

        var pick = eligible[_rng.Next(eligible.Count)];
        rt.idleGroupId = pick;
        rt.idleNextSeq = 1;
        rt.idleGroupLineCount = _catalog.IdleGroups[pick].Count;
        rt.idleLastSpeakerMemberId = null;
        rt.idleLastSplitMsgId = null;
        rt.idleMandatoryLineSpokenIdentities.Clear();
        return true;
    }

    private List<string> EligibleIdleGroups(GameState state)
    {
        var result = new List<string>();
        foreach (var kv in _catalog.IdleGroups)
        {
            var count = kv.Value.Count;
            if (count < 4 || count > 5)
            {
                continue;
            }

            result.Add(kv.Key);
        }

        return result;
    }

    private string? ResolveIdleSpeaker(GameState state, MemberBanterRuntimeState rt, IdleBanterLine line)
    {
        if (!string.Equals(line.MemberId, "*", StringComparison.Ordinal))
        {
            return FindMember(state, line.MemberId) != null ? line.MemberId : null;
        }

        var pool = ListEligibleSpeakers(state);
        if (pool.Count == 0)
        {
            return null;
        }

        var picked = BanterSpeakerPicker.Pick(
            pool,
            rt.idleLastSpeakerMemberId,
            rt.idleLastSplitMsgId,
            line.SplitMsgId,
            _rng,
            rt.idleMandatoryLineSpokenIdentities);
        return picked?.memberId;
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
            BanterPersonalExclusiveLines.TryResolveForIdle(
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

    private List<MemberState> ListEligibleSpeakers(GameState state)
    {
        var localLegion = LegionRegistry.Local(state)?.legionId;
        var list = new List<MemberState>();
        if (state.phase == GamePhase.COMBAT && state.combatRealtimeActive)
        {
            var bf = ActiveBattlefield(state);
            if (bf == null)
            {
                return list;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var u in bf.units)
            {
                if (u.IsDestroyed() || string.IsNullOrWhiteSpace(u.memberId) || !seen.Add(u.memberId))
                {
                    continue;
                }

                if (u.side != UnitSide.FRIENDLY)
                {
                    continue;
                }

                var m = FindMember(state, u.memberId);
                if (m != null)
                {
                    list.Add(m);
                }
            }

            return list;
        }

        foreach (var m in state.members)
        {
            if (m.memberId == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(localLegion))
            {
                var legionId = LegionPlayerRegistry.ResolveMemberLegionId(state, m);
                if (!string.Equals(legionId, localLegion, StringComparison.Ordinal))
                {
                    continue;
                }
            }

            list.Add(m);
        }

        return list;
    }

    private static BattlefieldState? ActiveBattlefield(GameState state)
    {
        if (string.IsNullOrWhiteSpace(state.activeBattlefieldId))
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
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
        var text = resolvedText ?? ResolveBanterText(state, primaryMemberId, catalogText);
        var pool = ListEligibleSpeakers(state);
        var speakerIds = new List<string> { primaryMemberId };
        if (allowMultiboxSync
            && BanterMultiboxSync.Roll(_rng)
            && BanterMultiboxSync.TryCollectSyncBurst(pool, primaryMemberId, out var burst))
        {
            speakerIds = burst;
        }

        foreach (var mid in speakerIds)
        {
            AppendCompanion(state, mid, text, channel, eventKey, groupId, simTimeSec);
        }

        return speakerIds;
    }

    private static void EnsureRuntime(GameState state)
    {
        state.banterRuntime ??= new MemberBanterRuntimeState();
    }

    private static void AppendCompanion(
        GameState state,
        string memberId,
        string text,
        string channel,
        string? eventKey,
        string? groupId,
        float simTimeSec)
    {
        state.companionLog.Add(new CompanionLogEntry
        {
            tick = simTimeSec,
            memberId = memberId,
            text = text,
            channel = channel,
            eventKey = eventKey,
            groupId = groupId,
            trustLevel = "NARRATIVE",
        });

        while (state.companionLog.Count > BanterCatalogLoader.CompanionLogCap)
        {
            state.companionLog.RemoveAt(0);
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
}
