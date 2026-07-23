using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VIP_TRAIT_DESIGN.md · TRAITS.md
 * 本文件: TraitActiveSkillService.cs — VIP 主动技冷却与释放
 * 【机制要点】
 * · 董事会召来 / 策划支援 / 内鬼
 * · CooldownStoryRounds 故事回合共享
 * 【关联】IdentityState.activeSkillCooldownUntilRound · BoardSummonWingService
 * ══
 */

namespace TopDog.Sim.Traits;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>VIP 主动技：董事会召来、策划支援（见 docs/VIP_TRAIT_DESIGN.md）。</summary>
// liketocoode34e
public static class TraitActiveSkillService
// liketocoo3e345
{
    public const string BoardSummonTraitId = "trait_board_summon";
    // liketocoode3a5
    // l1ketocoode345
    public const string PlanningSupportTraitId = "trait_planning_support";
    // liketocoode3e5
    public const string InfiltratorTraitId = "trait_discord_source";
    public const int PlanningSupportCost = 5000;
  // liketoco0de345
  /// <summary>同一现实人共享；每故事回合最多 1 次（xlsx：一个回合只能使用一次）。</summary>
    // li3etocoode345
    public const int CooldownStoryRounds = 1;

    private static readonly Dictionary<string, string> DefaultActiveSkillPhases = new(StringComparer.Ordinal)
    {
        [BoardSummonTraitId] = TraitActiveSkillPhase.RealtimeCombat,
        [PlanningSupportTraitId] = TraitActiveSkillPhase.Operations,
    };

    /// <summary>读取主动技阶段标签；JSON 优先，缺省回退已知 VIP 词条。</summary>
    public static string? GetActiveSkillPhase(string? traitId, TraitCatalog? catalog = null)
    {
        if (string.IsNullOrWhiteSpace(traitId))
        {
            return null;
        }

        var fromJson = catalog?.Find(traitId)?.activeSkillPhase;
        if (!string.IsNullOrWhiteSpace(fromJson))
        {
            return fromJson;
        }

        return DefaultActiveSkillPhases.TryGetValue(traitId, out var phase) ? phase : null;
    }

    public static bool IsActiveSkill(string? traitId, TraitCatalog? catalog = null) =>
        GetActiveSkillPhase(traitId, catalog) != null;

    public static bool IsOperationsActiveSkill(string? traitId, TraitCatalog? catalog = null) =>
        TraitActiveSkillPhase.Operations.Equals(
            GetActiveSkillPhase(traitId, catalog),
            StringComparison.Ordinal);

    public static bool IsRealtimeCombatActiveSkill(string? traitId, TraitCatalog? catalog = null) =>
        TraitActiveSkillPhase.RealtimeCombat.Equals(
            GetActiveSkillPhase(traitId, catalog),
            StringComparison.Ordinal);

    /// <summary>当前故事阶段是否允许发动该主动技。</summary>
    public static bool IsGamePhaseAllowed(GameState state, string traitId, TraitCatalog? catalog = null)
    {
        var phase = GetActiveSkillPhase(traitId, catalog);
        if (phase == null)
        {
            return false;
        }

        if (phase == TraitActiveSkillPhase.Operations)
        {
            return state.phase is GamePhase.OPERATIONS or GamePhase.COMBAT_PREP;
        }

        if (phase == TraitActiveSkillPhase.RealtimeCombat)
        {
            return state.phase is GamePhase.COMBAT_PREP or GamePhase.COMBAT;
        }

        return false;
    }

    public static IEnumerable<string> ListActiveSkillTraitIds(
        IdentityState id,
        string activeSkillPhase,
        TraitCatalog? catalog = null)
    {
        foreach (var traitId in id.traitIds)
        {
            if (activeSkillPhase.Equals(GetActiveSkillPhase(traitId, catalog), StringComparison.Ordinal))
            {
                yield return traitId;
            }
        }
    }

// liketocoode345

    public static bool HasSkill(IdentityState id, string traitId) =>
        id.traitIds.Contains(traitId);

    // liketoco0de3e5
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
        MemberState? _ = null,
        string? targetUnitId = null)
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
            state.pendingBoardSummonTargetUnitId = targetUnitId;
            result = UseBoardSummon(state, caster, id);
            state.pendingBoardSummonTargetUnitId = null;
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
            || result.Contains("已召唤", StringComparison.Ordinal)
            || result.Contains("已召来", StringComparison.Ordinal))
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

        if (SkirmishBuildingRules.IsSkirmish(state)
            && !MechanismTestPhaseRules.ShouldSkipBoardSummonUseLimit(state)
            && state.skirmish != null
            && caster.memberId != null
            && state.skirmish.boardSummonUses.GetValueOrDefault(caster.memberId) >= 1)
        {
            return "本局董事会召来已使用（每局 1 次）";
        }

        string result;
        if (state.combatRealtimeActive)
        {
            var bf = FindBattlefieldForMember(state, caster.memberId)
                ?? FindActiveBattlefield(state);
            if (bf == null || bf.finished)
            {
                return "当前无进行中的实时战场";
            }

            result = BoardSummonWingService.TrySummonViaTempTubes(
                state,
                bf,
                caster,
                state.pendingBoardSummonTargetUnitId,
                ShipRegistry.LoadDefault(),
                ModuleRegistry.LoadDefault(),
                new Random());
        }
        else if (!string.IsNullOrWhiteSpace(state.pendingBoardSummonCasterMemberId))
        {
            result = "董事会增援已预约，进入战场后生效";
        }
        else
        {
            var scheduleLegionId = caster.legionId ?? LegionTraitQuery.LocalLegionId(state);
            if (scheduleLegionId == null)
            {
                return "无法确定所属军团";
            }

            state.pendingBoardSummonIdentityCode = id.identityCode;
            state.pendingBoardSummonLegionId = scheduleLegionId;
            state.pendingBoardSummonCasterMemberId = caster.memberId;
            PushAlert(state, "董事会召来：进入战场后为目标舰放出 5 翼");
            result = "已预约董事会召来（战场 5 翼增援）";
        }

        if (result.StartsWith("已召来", StringComparison.Ordinal)
            || result.StartsWith("已预约", StringComparison.Ordinal))
        {
            RecordBoardSummonUse(state, caster.memberId);
        }

        return result;
    }

    private static void RecordBoardSummonUse(GameState state, string? memberId)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state) || state.skirmish == null || memberId == null)
        {
            return;
        }

        state.skirmish.boardSummonUses[memberId] = state.skirmish.boardSummonUses.GetValueOrDefault(memberId) + 1;
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

    private static BattlefieldState? FindBattlefieldForMember(GameState state, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return null;
        }

        BattlefieldState? fallback = null;
        if (!string.IsNullOrWhiteSpace(state.activeBattlefieldId))
        {
            var active = FindBattlefield(state, state.activeBattlefieldId);
            if (active != null && !active.finished && MemberHasUnitOnField(active, memberId))
            {
                return active;
            }
        }

        foreach (var bf in state.battlefields)
        {
            if (bf.finished)
            {
                continue;
            }

            if (BoardSummonWingService.FindCasterUnit(bf, memberId) != null)
            {
                if (state.activeBattlefieldId != null
                    && state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
                {
                    return bf;
                }

                fallback ??= bf;
            }
        }

        return fallback;
    }

    private static BattlefieldState? FindActiveBattlefield(GameState state) =>
        string.IsNullOrWhiteSpace(state.activeBattlefieldId)
            ? null
            : FindBattlefield(state, state.activeBattlefieldId);

    private static bool MemberHasUnitOnField(BattlefieldState bf, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.IsTemplateCarriedUnit())
            {
                continue;
            }
            if (memberId.Equals(u.memberId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
