using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Traits;

/// <summary>VIP 主动技：董事会召来、策划支援（见 docs/VIP_TRAIT_DESIGN.md）。</summary>
public static class TraitActiveSkillService
{
    public const string BoardSummonTraitId = "trait_board_summon";
    public const string PlanningSupportTraitId = "trait_planning_support";
    public const string InfiltratorTraitId = "trait_discord_source";
    public const int PlanningSupportCost = 5000;
  /// <summary>同一现实人共享；每故事回合最多 1 次（xlsx：一个回合只能使用一次）。</summary>
    public const int CooldownStoryRounds = 1;

    public static bool HasSkill(IdentityState id, string traitId) =>
        id.traitIds.Contains(traitId);

    public static int CooldownRoundsRemaining(GameState state, IdentityState id, string traitId)
    {
        if (!id.activeSkillCooldownUntilRound.TryGetValue(traitId, out var until))
        {
            return 0;
        }
        return Math.Max(0, until - state.storyRound);
    }

    public static bool CanUse(GameState state, IdentityState id, string traitId) =>
        HasSkill(id, traitId) && CooldownRoundsRemaining(state, id, traitId) == 0;

    public static string TryUse(
        GameState state,
        MemberState caster,
        string traitId,
        MemberState? _ = null)
    {
        var id = IdentityMigrationService.GetOrCreate(state, caster);
        if (!HasSkill(id, traitId))
        {
            return "该现实人无此主动技";
        }
        var cd = CooldownRoundsRemaining(state, id, traitId);
        if (cd > 0)
        {
            return "冷却中（剩余 " + cd + " 故事回合）";
        }

        string result;
        if (traitId == BoardSummonTraitId)
        {
            result = UseBoardSummon(state, caster, id);
        }
        else if (traitId == PlanningSupportTraitId)
        {
            result = UsePlanningSupport(state, caster, id);
        }
        else
        {
            return "未知主动技";
        }

        if (result.StartsWith("已", StringComparison.Ordinal)
            || result.Contains("已召唤", StringComparison.Ordinal))
        {
            id.activeSkillCooldownUntilRound[traitId] = state.storyRound + CooldownStoryRounds;
            IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
        }
        return result;
    }

    private static string UseBoardSummon(GameState state, MemberState caster, IdentityState id)
    {
        if (state.phase is not (GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            return "仅交战准备或战斗阶段可发动董事会召来";
        }

        if (state.combatRealtimeActive && state.activeBattlefieldId != null)
        {
            var bf = FindBattlefield(state, state.activeBattlefieldId);
            if (bf != null && !bf.finished)
            {
                return BoardSummonApproachService.SummonWithWarpApproach(
                    state,
                    bf,
                    id,
                    caster,
                    ShipRegistry.LoadDefault(),
                    ModuleRegistry.LoadDefault(),
                    new Random());
            }
        }

        if (!string.IsNullOrWhiteSpace(state.pendingBoardSummonLegionId))
        {
            return "董事会增援已预约，进入战场后生效";
        }
        var legionId = caster.legionId ?? LegionTraitQuery.LocalLegionId(state);
        if (legionId == null)
        {
            return "无法确定所属军团";
        }
        state.pendingBoardSummonIdentityCode = id.identityCode;
        state.pendingBoardSummonLegionId = legionId;
        PushAlert(state, "董事会召来：下一场友方战场将增援 5 艘无畏");
        return "已预约董事会召来（下一场战场 5 艘无畏）";
    }

    private static string UsePlanningSupport(GameState state, MemberState caster, IdentityState id)
    {
        if (state.phase is not (GamePhase.OPERATIONS or GamePhase.COMBAT_PREP))
        {
            return "仅运营或交战准备阶段可发动策划支援";
        }
        if (!MemberAssetService.TryDebitLegion(state, CurrencyIds.StarCoin, PlanningSupportCost))
        {
            return "军团星币不足（需 " + PlanningSupportCost + "）";
        }
        var legionId = caster.legionId ?? LegionTraitQuery.LocalLegionId(state);
        var moleCode = FindUnrevealedInfiltrator(state, legionId);
        if (moleCode == null)
        {
            Legion.LegionRegistry.CreditLocal(state, CurrencyIds.StarCoin, PlanningSupportCost);
            return "团内暂无可揭露的内鬼";
        }
        state.revealedInfiltratorIdentityCodes.Add(moleCode);
        var display = DisplayIdentity(state, moleCode);
        PushAlert(state, "策划支援：揭露内鬼 " + display + "（" + moleCode + "）");
        return "已揭露内鬼：" + display;
    }

    private static string? FindUnrevealedInfiltrator(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return null;
        }
        foreach (var kv in state.exchange.infiltrationByIdentity)
        {
            if (!legionId.Equals(kv.Value.homeLegionId, StringComparison.Ordinal))
            {
                continue;
            }
            if (!state.revealedInfiltratorIdentityCodes.Contains(kv.Key))
            {
                return kv.Key;
            }
        }
        foreach (var m in state.members)
        {
            if (!legionId.Equals(m.legionId, StringComparison.Ordinal))
            {
                continue;
            }
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code)
                || state.revealedInfiltratorIdentityCodes.Contains(code))
            {
                continue;
            }
            if (state.identities.TryGetValue(code, out var identity)
                && identity.traitIds.Contains(InfiltratorTraitId))
            {
                return code;
            }
        }
        return null;
    }

    private static BattlefieldState? FindBattlefield(GameState state, string battlefieldId)
    {
        foreach (var bf in state.battlefields)
        {
            if (battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }

    private static string DisplayIdentity(GameState state, string identityCode)
    {
        foreach (var m in state.members)
        {
            if (identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                return m.name ?? m.memberId ?? identityCode;
            }
        }
        return identityCode;
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
