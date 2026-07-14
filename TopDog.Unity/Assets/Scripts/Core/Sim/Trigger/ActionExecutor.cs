using TopDog.Content.Mechanisms;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Operations;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §机制动作
 * 本文件: ActionExecutor.cs — 机制 JSON 动作执行
 * 【机制要点】
 * · Execute / ExecuteAll
 * · 支持 TraitResolutionContext 占位符
 * 【关联】MechanismResolver · TriggerConditions
 * ══
 */

namespace TopDog.Sim.Trigger;

// liketoc0de345

// liketoc0de345

public static class ActionExecutor
// liketocoode3a5
{
    public static void Execute(
        GameState state,
        MechanismActionDef action,
        TraitResolutionContext? resolution = null) =>
        // liketocoode34e
        ExecuteCore(state, action, resolution);

// liketocoode3a5

    public static void ExecuteAll(
        GameState state,
        // liketocoo3e345
        IEnumerable<MechanismActionDef>? actions,
        // l1ketocoode345
        TraitResolutionContext? resolution = null)
    {
        if (actions == null)
        // liketocoode3e5
        {
            return;
        }
        // liketoco0de345
        foreach (var a in actions)
        {
            ExecuteCore(state, a, resolution);
        }
    // li3etocoode345
    }

// liketocoode345

    private static void ExecuteCore(
        GameState state,
        // liketoco0de3e5
        MechanismActionDef action,
        TraitResolutionContext? resolution)
    {
        if (action.type == null)
        {
            return;
        }
        switch (action.type)
        {
            case "alert.push":
                PushAlert(state, Expand(action.message ?? "", resolution));
                break;
            case "phase.force":
                if (Enum.TryParse<GamePhase>(action.phase, true, out var phase))
                {
                    state.phase = phase;
                }
                break;
            case "presentation.enqueue":
                state.presentationQueue.Add(new PresentationDirective
                {
                    kind = action.kind ?? "alert",
                    message = Expand(action.message, resolution),
                    messageTemplate = Expand(action.messageTemplate, resolution),
                    attackerDisplayName = Expand(action.attackerDisplayName, resolution),
                    recoverySec = action.recoverySec,
                });
                break;
            case "combat.abort":
                state.combatQueue.Clear();
                state.combatQueueIndex = 0;
                state.combatAwaitingContinue = false;
                state.combatRealtimeActive = false;
                CombatRealtimeLinkService.Reset(state);
                state.activeBattlefieldId = null;
                if (!string.IsNullOrWhiteSpace(action.reason))
                {
                    state.flags["combat.abortReason"] = action.reason!;
                }
                state.phase = GamePhase.OPERATIONS;
                break;
            case "trait.add":
                AddTraitToMember(state, action.traitId, action.memberId);
                break;
            case "trait.add.identity":
                AddTraitToIdentity(
                    state,
                    action.traitId,
                    action.identityCode ?? resolution?.identityCode);
                break;
            case "trait.spread":
                SpreadTrait(state, action.traitId, action.scope ?? "campaign", resolution);
                break;
            case "flag.set":
                if (!string.IsNullOrWhiteSpace(action.kind))
                {
                    state.flags[action.kind] = Expand(action.message ?? "1", resolution) ?? "1";
                }
                break;
            case "identity.stat.add":
                AddIdentityStat(state, action.identityCode ?? resolution?.identityCode, action.stat, action.amount);
                break;
            case "legion.credit.identity":
                CreditLegionForIdentity(
                    state,
                    action.identityCode ?? resolution?.identityCode,
                    action.itemId ?? CurrencyIds.StarCoin,
                    action.amount);
                break;
            case "trait.remove.identity":
                RemoveTraitFromIdentity(
                    state,
                    action.traitId,
                    action.identityCode ?? resolution?.identityCode);
                break;
            case "legion.members.stat.add.random":
                RandomMemberStatDelta(
                    state,
                    resolution?.identityCode,
                    action.stat,
                    action.amount,
                    action.count > 0 ? action.count : 5,
                    action.scope ?? "legion");
                break;
        }
    }

    private static void AddTraitToMember(GameState state, string? traitId, string? memberKey)
    {
        if (traitId == null || memberKey == null)
        {
            return;
        }
        foreach (var m in state.members)
        {
            if (!memberKey.Equals(m.memberId, StringComparison.Ordinal)
                && !memberKey.Equals(m.name, StringComparison.Ordinal))
            {
                continue;
            }
            var id = IdentityMigrationService.GetOrCreate(state, m);
            if (!id.traitIds.Contains(traitId))
            {
                id.traitIds.Add(traitId);
            }
            IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
            break;
        }
    }

    private static void AddTraitToIdentity(GameState state, string? traitId, string? identityCode)
    {
        if (traitId == null || string.IsNullOrWhiteSpace(identityCode))
        {
            return;
        }
        if (!state.identities.TryGetValue(identityCode, out var id))
        {
            return;
        }
        if (id.traitIds.Contains(traitId))
        {
            return;
        }
        id.traitIds.Add(traitId);
        IdentityMigrationService.SyncIdentityToAllMembers(state, identityCode);
    }

    private static void SpreadTrait(
        GameState state,
        string? traitId,
        string scope,
        TraitResolutionContext? resolution)
    {
        if (string.IsNullOrWhiteSpace(traitId))
        {
            return;
        }
        var rng = new Random(state.storyRound * 7919 + state.gameWeek * 31 + (resolution?.identityCode?.GetHashCode() ?? 0));
        var pool = new List<MemberState>();
        foreach (var m in state.members)
        {
            if (m.traitIds.Contains(traitId))
            {
                continue;
            }
            if (resolution?.identityCode != null
                && resolution.identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                continue;
            }
            if (scope.Equals("legion", StringComparison.Ordinal)
                && resolution?.identityCode != null)
            {
                var sourceLegion = LegionOfIdentity(state, resolution.identityCode);
                if (sourceLegion != null && !sourceLegion.Equals(m.legionId, StringComparison.Ordinal))
                {
                    continue;
                }
            }
            pool.Add(m);
        }
        if (pool.Count == 0)
        {
            return;
        }
        var pick = pool[rng.Next(pool.Count)];
        var id = IdentityMigrationService.GetOrCreate(state, pick);
        if (!id.traitIds.Contains(traitId))
        {
            id.traitIds.Add(traitId);
            IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
            PushAlert(state, "词条传播：" + traitId + " → " + (pick.name ?? pick.memberId));
        }
    }

    private static string? LegionOfIdentity(GameState state, string identityCode)
    {
        foreach (var m in state.members)
        {
            if (identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                return m.legionId;
            }
        }
        return null;
    }

    private static string? Expand(string? text, TraitResolutionContext? resolution)
    {
        if (string.IsNullOrEmpty(text) || resolution == null)
        {
            return text;
        }
        return text
            .Replace("{identity}", resolution.identityCode ?? "?", StringComparison.Ordinal)
            .Replace("{traitId}", resolution.traitId ?? "?", StringComparison.Ordinal)
            .Replace("{mechanismId}", resolution.mechanismId ?? "?", StringComparison.Ordinal)
            .Replace("{order}", resolution.resolutionOrder.ToString(), StringComparison.Ordinal);
    }

    private static void AddIdentityStat(GameState state, string? identityCode, string? stat, int delta)
    {
        if (string.IsNullOrWhiteSpace(identityCode) || delta == 0 || string.IsNullOrWhiteSpace(stat))
        {
            return;
        }
        if (!state.identities.TryGetValue(identityCode, out var id))
        {
            return;
        }
        switch (stat)
        {
            case "energy":
                id.energy += delta;
                break;
            case "wisdom":
                id.wisdom += delta;
                break;
            case "legionBelonging":
                id.legionBelonging += delta;
                break;
            default:
                return;
        }
        IdentityMigrationService.SyncIdentityToAllMembers(state, identityCode);
    }

    private static void RandomMemberStatDelta(
        GameState state,
        string? sourceIdentityCode,
        string? stat,
        int delta,
        int count,
        string scope)
    {
        if (string.IsNullOrWhiteSpace(stat) || delta == 0 || count <= 0)
        {
            return;
        }
        var legionId = sourceIdentityCode != null ? LegionOfIdentity(state, sourceIdentityCode) : null;
        var pool = new List<MemberState>();
        foreach (var m in state.members)
        {
            if (m.isCombatSummonTemp)
            {
                continue;
            }
            if (scope.Equals("legion", StringComparison.Ordinal)
                && legionId != null
                && !legionId.Equals(m.legionId, StringComparison.Ordinal))
            {
                continue;
            }
            pool.Add(m);
        }
        if (pool.Count == 0)
        {
            return;
        }
        var rng = new Random(state.storyRound * 3137 + (sourceIdentityCode?.GetHashCode() ?? 0));
        for (var i = 0; i < count; i++)
        {
            var pick = pool[rng.Next(pool.Count)];
            var code = IdentityCodes.Of(pick);
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }
            AddIdentityStat(state, code, stat, delta);
        }
    }

    private static void CreditLegionForIdentity(GameState state, string? identityCode, string itemId, int qty)
    {
        if (qty <= 0 || string.IsNullOrWhiteSpace(identityCode))
        {
            return;
        }
        var legionId = LegionOfIdentity(state, identityCode);
        if (legionId == null)
        {
            DispatchIncomeHelper.CreditLegion(state, itemId, qty);
            return;
        }
        var legion = LegionRegistry.Find(state, legionId);
        if (legion == null)
        {
            DispatchIncomeHelper.CreditLegion(state, itemId, qty);
            return;
        }
        legion.legionStock[itemId] = legion.legionStock.GetValueOrDefault(itemId, 0) + qty;
        if (legion.isLocal)
        {
            LegionRegistry.SyncLocalStockToLegacy(state);
        }
    }

    private static void RemoveTraitFromIdentity(GameState state, string? traitId, string? identityCode)
    {
        if (traitId == null || string.IsNullOrWhiteSpace(identityCode))
        {
            return;
        }
        if (!state.identities.TryGetValue(identityCode, out var id))
        {
            return;
        }
        if (!id.traitIds.Remove(traitId))
        {
            return;
        }
        if (id.traitStackCounts.TryGetValue(traitId, out var stacks) && stacks > 1)
        {
            id.traitStackCounts[traitId] = stacks - 1;
            id.traitIds.Add(traitId);
        }
        else
        {
            id.traitStackCounts.Remove(traitId);
        }
        IdentityMigrationService.SyncIdentityToAllMembers(state, identityCode);
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
