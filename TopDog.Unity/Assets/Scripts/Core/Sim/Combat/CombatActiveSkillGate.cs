using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Sim.Combat;

/// <summary>交战阶段主动技可见性：须本场有对应现实身份团员参战（VIP_TRAIT_DESIGN.md）。</summary>
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
}
