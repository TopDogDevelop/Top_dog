using TopDog.App.Brick;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §处理
 * 本文件: ExchangeProcessor.cs — 交换中心唯一跨军团写入口
 * 【机制要点】
 * · 处理 inbox：DispatchIntent / ContactDetected / RecruitComplete
 * · 投票物化 REALTIME / 交易撮合
 * 【关联】ExchangeSystemBrick · BattlefieldMaterializer
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>交换中心唯一跨军团写入口：处理 inbox、接触检测、招新提交、投票物化。</summary>
// liketocoode34e
public static class ExchangeProcessor
// liketocoo3e345
{
    public static void ProcessPending(GameState state)
    // l1ketocoode345
    // liketocoode3a5
    {
        var inbox = state.exchange.pendingMessages;
        // liketocoode3e5
        for (var i = inbox.Count - 1; i >= 0; i--)
        {
            var msg = inbox[i];
            switch (msg.kind)
            {
                case ExchangeMessageKind.DispatchIntent:
                    ApplyDispatchIntent(state, msg);
                    inbox.RemoveAt(i);
                    break;
                case ExchangeMessageKind.RecruitComplete:
                    ApplyRecruitComplete(state, msg);
                    inbox.RemoveAt(i);
                    break;
                case ExchangeMessageKind.ResolveModeVote:
                    ApplyResolveVote(state, msg);
                    inbox.RemoveAt(i);
                    break;
                case ExchangeMessageKind.ContactDetected:
                    inbox.RemoveAt(i);
                    break;
                case ExchangeMessageKind.TradePlayerBuy:
                case ExchangeMessageKind.TradePlayerList:
                    ApplyCrossPlayerTrade(state, msg);
                    inbox.RemoveAt(i);
                    // liketoco0de345
                    break;
            // li3etocoode345
            }
        // liketocoode345
        }
        DetectSystemContacts(state);
    }

    private static void ApplyCrossPlayerTrade(GameState state, ExchangeMessage msg)
    {
        msg.tradeResult = msg.kind switch
        {
            // liketoco0de3e5
            ExchangeMessageKind.TradePlayerBuy => PlayerMarketService.BuyFromPlayerListing(
                state, msg.listingId ?? "", Math.Max(1, msg.quantity)),
            ExchangeMessageKind.TradePlayerList => PlayerMarketService.ListFromLegionStock(
                state, msg.legionId ?? "", msg.itemId ?? "", Math.Max(1, msg.quantity)),
            _ => "交换中心：未知跨玩家交易",
        };
        BrickDebugLog.Log("exchange.hub", msg.kind + " → " + msg.tradeResult);
    }

    private static void ApplyDispatchIntent(GameState state, ExchangeMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.legionId))
        {
            return;
        }
        foreach (var memberId in msg.memberIds)
        {
            var m = LegionPlayerRegistry.FindMember(state, memberId);
            if (m == null)
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(msg.targetSystemId))
            {
                m.currentSolarSystemId = msg.targetSystemId;
            }
            if (!string.IsNullOrWhiteSpace(msg.task))
            {
                m.assignedTask = msg.task;
            }
            m.playerDispatchActive = true;
            if (msg.infiltration)
            {
                var code = IdentityCodes.Of(m);
                var home = m.homeLegionId ?? msg.legionId;
                if (!ExchangeInfiltrationRegistry.TryRegister(
                        state, code, home, msg.targetSystemId, InfiltrationMode.Dispatch))
                {
                    PushAlert(state, "交换中心：该身份已在其他军团潜伏，派遣潜伏被拒绝");
                    continue;
                }
                InfiltratorRosterService.BeginInfiltration(state, m, msg.legionId, msg.targetSystemId);
            }
        }
    }

    private static void ApplyRecruitComplete(GameState state, ExchangeMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.legionId))
        {
            return;
        }
        var hostLegionId = msg.legionId;
        foreach (var m in msg.recruitMembers)
        {
            var home = m.homeLegionId ?? m.legionId;
            var hostileInfiltrator = !string.IsNullOrWhiteSpace(home)
                && !home.Equals(hostLegionId, StringComparison.Ordinal)
                && LegionRegistry.IsHostile(state, home, hostLegionId)
                && MemberHasInfiltratorTrait(state, m);

            if (hostileInfiltrator)
            {
                if (!ExchangeInfiltrationRegistry.TryRegisterHostileRecruit(state, m, hostLegionId))
                {
                    PushAlert(state, "交换中心：内鬼 " + (m.name ?? m.memberId) + " 已在其他敌对军团，招新拒绝");
                    continue;
                }
                m.homeLegionId ??= home;
                m.infiltrationLegionId = hostLegionId;
                m.legionId = hostLegionId;
                m.rosterVisibility = MemberRosterVisibility.Infiltrating;
                LegionPlayerRegistry.AddMemberToLegion(state, hostLegionId, m);
                PushAlert(state, "交换中心：内鬼 " + (m.name ?? m.memberId) + " 经敌对招新进入 " + hostLegionId);
            }
            else
            {
                m.rosterVisibility = MemberRosterVisibility.Home;
                m.legionId = hostLegionId;
                m.homeLegionId ??= hostLegionId;
                m.infiltrationLegionId = null;
                LegionPlayerRegistry.AddMemberToLegion(state, hostLegionId, m);
            }
            MatchIdentityRegistry.Record(state, m.identityCode);
        }
    }

    private static bool MemberHasInfiltratorTrait(GameState state, MemberState m)
    {
        if (m.traitIds.Contains(InfiltratorRosterService.InfiltratorTraitId))
        {
            return true;
        }
        var code = IdentityCodes.Of(m);
        return !string.IsNullOrWhiteSpace(code)
            && state.identities.TryGetValue(code, out var id)
            && id.traitIds.Contains(InfiltratorRosterService.InfiltratorTraitId);
    }

    private static void ApplyResolveVote(GameState state, ExchangeMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.encounterId) || string.IsNullOrWhiteSpace(msg.legionId))
        {
            return;
        }
        if (!state.exchange.realtimeVotes.TryGetValue(msg.encounterId, out var votes))
        {
            votes = new Dictionary<string, string>(StringComparer.Ordinal);
            state.exchange.realtimeVotes[msg.encounterId] = votes;
        }
        votes[msg.legionId] = msg.resolveVote ?? "AUTO";
        if (!"REALTIME".Equals(msg.resolveVote, StringComparison.Ordinal))
        {
            return;
        }
        var brief = FindEncounter(state, msg.encounterId);
        if (brief == null)
        {
            return;
        }
        BattlefieldMaterializer.TryMaterialize(state, brief);
    }

    public static EncounterBrief? FindEncounter(GameState state, string encounterId)
    {
        foreach (var e in state.exchange.activeEncounters)
        {
            if (encounterId.Equals(e.encounterId, StringComparison.Ordinal))
            {
                return e;
            }
        }
        return null;
    }

    private static void DetectSystemContacts(GameState state)
    {
        if (state.phase != GamePhase.OPERATIONS)
        {
            return;
        }
        var bySystem = new Dictionary<string, Dictionary<string, List<MemberState>>>(StringComparer.Ordinal);
        foreach (var legion in state.legions)
        {
            var lid = legion.legionId;
            if (string.IsNullOrWhiteSpace(lid))
            {
                continue;
            }
            foreach (var m in LegionPlayerRegistry.VisibleRoster(state, lid))
            {
                if (string.IsNullOrWhiteSpace(m.currentSolarSystemId))
                {
                    continue;
                }
                if (!bySystem.TryGetValue(m.currentSolarSystemId, out var legions))
                {
                    legions = new Dictionary<string, List<MemberState>>(StringComparer.Ordinal);
                    bySystem[m.currentSolarSystemId] = legions;
                }
                if (!legions.TryGetValue(lid, out var list))
                {
                    list = new List<MemberState>();
                    legions[lid] = list;
                }
                list.Add(m);
            }
        }
        foreach (var (systemId, legions) in bySystem)
        {
            if (legions.Count < 2)
            {
                continue;
            }
            var legionIds = legions.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
            foreach (var component in HostileComponents(state, legionIds))
            {
                RegisterMultiEncounter(state, systemId, component, legions);
            }
        }
    }

    private static List<List<string>> HostileComponents(GameState state, IReadOnlyList<string> legionIds)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<List<string>>();
        foreach (var start in legionIds)
        {
            if (visited.Contains(start))
            {
                continue;
            }
            var component = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                component.Add(u);
                foreach (var v in legionIds)
                {
                    if (visited.Contains(v) || u.Equals(v, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (!LegionRegistry.IsHostile(state, u, v))
                    {
                        continue;
                    }
                    visited.Add(v);
                    queue.Enqueue(v);
                }
            }
            if (component.Count >= 2)
            {
                components.Add(component);
            }
        }
        return components;
    }

    private static void RegisterMultiEncounter(
        GameState state,
        string systemId,
        IReadOnlyList<string> legionIds,
        IReadOnlyDictionary<string, List<MemberState>> legionsById)
    {
        var sorted = legionIds.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var encounterId = "enc_" + systemId + "_" + string.Join("+", sorted);
        foreach (var existing in state.exchange.activeEncounters)
        {
            if (encounterId.Equals(existing.encounterId, StringComparison.Ordinal))
            {
                return;
            }
        }
        var participants = new List<EncounterParticipant>();
        var hasSpy = false;
        foreach (var lid in sorted)
        {
            if (!legionsById.TryGetValue(lid, out var members))
            {
                continue;
            }
            participants.Add(new EncounterParticipant
            {
                legionId = lid,
                publicRoster = ToPublicRoster(members),
            });
            if (members.Any(m => m.rosterVisibility == MemberRosterVisibility.Infiltrating))
            {
                hasSpy = true;
            }
        }
        if (participants.Count < 2)
        {
            return;
        }
        var brief = new EncounterBrief
        {
            encounterId = encounterId,
            systemId = systemId,
            attackerLegionId = sorted[0],
            defenderLegionId = sorted.Count > 1 ? sorted[1] : sorted[0],
            combatSubtype = CombatSubtype.HARVEST,
            attackerRoster = participants[0].publicRoster,
            defenderRoster = participants.Count > 1
                ? participants[1].publicRoster
                : new List<CombatRosterLine>(),
            participants = participants,
            hasHiddenInfiltrator = hasSpy,
        };
        state.exchange.activeEncounters.Add(brief);
        state.exchange.pendingMessages.Add(new ExchangeMessage
        {
            kind = ExchangeMessageKind.ContactDetected,
            encounter = brief,
        });
        PushAlert(state, "交换中心：多方军团接触 " + systemId + " · 遭遇 " + encounterId);
    }

    private static List<CombatRosterLine> ToPublicRoster(IReadOnlyList<MemberState> members)
    {
        var lines = new List<CombatRosterLine>();
        foreach (var m in members)
        {
            if (m.rosterVisibility == MemberRosterVisibility.Infiltrating)
            {
                continue;
            }
            lines.Add(new CombatRosterLine
            {
                memberId = m.memberId,
                displayName = m.name ?? m.accountName,
                hullId = m.equippedHullId,
                canParticipate = !string.IsNullOrWhiteSpace(m.equippedHullId),
            });
        }
        return lines;
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
