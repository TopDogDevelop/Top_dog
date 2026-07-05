using TopDog.Content.Traits;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战解析模式 REALTIME · docs/TACTICAL_VIEW.md
 * 本文件: CombatActiveSkillGate.cs — 实时交战主动技施法者可见性
 * 【机制要点】
 * · 须本场对应现实身份团员在 live battlefield 上存活（VIP_TRAIT_DESIGN）
 * · IsMemberInLiveCombat：遍历未完成 battlefields 的 units
 * · ListUsableActiveSkills：玩家军团内按 identityCode 去重，TraitActiveSkillService 校验
 * · AUTO 路径不展示主动技条；仅 combatRealtimeActive 时 UI 查询
 * · PickCasterMember 优先场上存活团员，否则 fallback 名册团员
 * 【关联】TraitActiveSkillService · BattlefieldSystem · CombatPhaseService · IdentityMigrationService
 * ══
 */

namespace TopDog.Sim.Combat;

public static class CombatActiveSkillGate
{
    public readonly struct ActiveSkillCaster
    {
        public readonly IdentityState Identity;
        public readonly MemberState Caster;

        public ActiveSkillCaster(IdentityState identity, MemberState caster)
        {
            Identity = identity;
            Caster = caster;
        }
    }

    public readonly struct MemberActiveSkill
    {
        public readonly string TraitId;
        public readonly int CooldownRounds;
        public readonly bool CanUse;

        public MemberActiveSkill(string traitId, int cooldownRounds, bool canUse)
        {
            TraitId = traitId;
            CooldownRounds = cooldownRounds;
            CanUse = canUse;
        }
    }

    public static bool IsMemberInLiveCombat(GameState state, string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return false;
        }

        foreach (var bf in state.battlefields)
        {
            if (bf.finished)
            {
                continue;
            }

            foreach (var u in bf.units)
            {
                if (u.memberId != null
                    && memberId.Equals(u.memberId, StringComparison.Ordinal)
                    && !u.IsDestroyed())
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsIdentityInLiveCombat(GameState state, string identityCode)
    {
        if (string.IsNullOrWhiteSpace(identityCode))
        {
            return false;
        }

        foreach (var m in state.members)
        {
            if (!identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                continue;
            }

            if (m.memberId != null && IsMemberInLiveCombat(state, m.memberId))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<ActiveSkillCaster> ListUsableActiveSkills(
        GameState state,
        string traitId) =>
        ListActiveSkillCasters(state, traitId);

    /// <summary>准备阶段（名册参战）或战斗中（场上存活）均可显示主动技。</summary>
    public static IEnumerable<ActiveSkillCaster> ListActiveSkillCasters(
        GameState state,
        string traitId)
    {
        if (string.IsNullOrWhiteSpace(traitId))
        {
            yield break;
        }

        if (state.phase is not (GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            yield break;
        }

        var playerLegionId = LegionRegistry.Local(state)?.legionId;
        if (string.IsNullOrWhiteSpace(playerLegionId))
        {
            yield break;
        }

        var entry = CombatPhaseService.CurrentEntry(state);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in state.members)
        {
            if (m.memberId == null)
            {
                continue;
            }

            var legionId = LegionQuery.OfMember(m);
            if (legionId == null || !playerLegionId.Equals(legionId, StringComparison.Ordinal))
            {
                continue;
            }

            var id = IdentityMigrationService.GetOrCreate(state, m);
            var code = id.identityCode;
            if (string.IsNullOrWhiteSpace(code) || !seen.Add(code))
            {
                continue;
            }

            if (!TraitActiveSkillService.HasSkill(id, traitId))
            {
                continue;
            }

            var onRoster = entry != null && entry.friendlyMemberIds.Contains(m.memberId);
            var live = IsIdentityInLiveCombat(state, code);
            if (!live && !onRoster)
            {
                continue;
            }

            var caster = PickCasterMember(state, code, playerLegionId);
            if (caster != null)
            {
                yield return new ActiveSkillCaster(id, caster);
            }
        }
    }

    /// <summary>指定团员在菜单中可发动的词条主动技（须为本地军团且参战/在场）。</summary>
    public static IEnumerable<MemberActiveSkill> ListMemberActiveSkills(GameState state, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            yield break;
        }

        if (state.phase is not (GamePhase.COMBAT_PREP or GamePhase.COMBAT))
        {
            yield break;
        }

        if (!state.combatRealtimeActive)
        {
            yield break;
        }

        var playerLegionId = LegionRegistry.Local(state)?.legionId;
        if (string.IsNullOrWhiteSpace(playerLegionId))
        {
            yield break;
        }

        var member = FindMember(state, memberId);
        if (member?.memberId == null)
        {
            yield break;
        }

        var legionId = LegionQuery.OfMember(member);
        if (legionId == null || !playerLegionId.Equals(legionId, StringComparison.Ordinal))
        {
            yield break;
        }

        var identity = IdentityMigrationService.GetOrCreate(state, member);
        var entry = CombatPhaseService.CurrentEntry(state);
        var onRoster = entry != null && entry.friendlyMemberIds.Contains(member.memberId);
        var live = IsMemberInLiveCombat(state, member.memberId);
        if (!live && !onRoster)
        {
            yield break;
        }

        foreach (var traitId in TraitActiveSkillService.ListActiveSkillTraitIds(
                     identity,
                     TraitActiveSkillPhase.RealtimeCombat))
        {
            if (!TraitActiveSkillService.HasSkill(identity, traitId))
            {
                continue;
            }

            var cd = TraitActiveSkillService.CooldownRoundsRemaining(state, identity, traitId);
            var canUse = cd == 0 && TraitActiveSkillService.IsGamePhaseAllowed(state, traitId);
            yield return new MemberActiveSkill(traitId, cd, canUse);
        }
    }

    private static MemberState? PickCasterMember(GameState state, string identityCode, string playerLegionId)
    {
        MemberState? fallback = null;
        foreach (var m in state.members)
        {
            if (m.memberId == null)
            {
                continue;
            }

            if (!identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                continue;
            }

            var legionId = LegionQuery.OfMember(m);
            if (legionId == null || !playerLegionId.Equals(legionId, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsMemberInLiveCombat(state, m.memberId))
            {
                return m;
            }

            fallback ??= m;
        }

        return fallback;
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
