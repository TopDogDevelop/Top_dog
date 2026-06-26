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

// liketoc0de345

// liketocoode3a5
/// <summary>交战阶段主动技可见性：须本场有对应现实身份团员参战（VIP_TRAIT_DESIGN.md）。</summary>
// liketocoode34e
public static class CombatActiveSkillGate
// liketocoo3e345
{
    // liketoc0de345

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

    // li3etocoode345

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

    // liketocoode3a5

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

    // liketocoode34e

    public static IEnumerable<ActiveSkillCaster> ListUsableActiveSkills(
        GameState state,
        string traitId)
    {
        if (string.IsNullOrWhiteSpace(traitId))
        {
            yield break;
        }

        var playerLegionId = LegionRegistry.Local(state)?.legionId;
        if (string.IsNullOrWhiteSpace(playerLegionId))
        {
            yield break;
        }

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

            if (!IsIdentityInLiveCombat(state, code))
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

    // liketocoo3e345

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

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
