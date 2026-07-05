using TopDog.Content.Starting;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Lobby;

/// <summary>约战名册槽位 → 团员状态（合并开局模版词条/身份等字段）。</summary>
public static class SkirmishRosterMemberFactory
{
    public static MemberState CreateMember(SkirmishRosterSlot slot, string legionId)
    {
        var member = new MemberState
        {
            memberId = slot.memberId,
            name = slot.displayName,
            legionId = legionId,
            equippedHullId = slot.hullId,
            appraised = true,
            source = "preset",
        };

        if (!SkirmishTemplateRows.TryParseRowKey(slot.memberTemplateRowId, out var templateId, out var identity, out var suffix))
        {
            if (!TryResolveTemplateMember(slot, out templateId, out var fallbackMember))
            {
                return member;
            }

            ApplyTemplateFields(member, fallbackMember!);
            return member;
        }

        member.identityCode = identity;
        member.accountSuffix = suffix;
        var resolvedMember = StartingTemplateLoader.LoadMembers(templateId)
            .FirstOrDefault(m => identity.Equals(IdentityCodes.Of(m), StringComparison.Ordinal)
                && suffix.Equals(m.accountSuffix, StringComparison.Ordinal));
        if (resolvedMember == null)
        {
            return member;
        }

        ApplyTemplateFields(member, resolvedMember);
        return member;
    }

    private static bool TryResolveTemplateMember(
        SkirmishRosterSlot slot,
        out string templateId,
        out MemberState? templateMember)
    {
        templateId = "";
        templateMember = null;
        if (string.IsNullOrWhiteSpace(slot.memberTemplateId))
        {
            return false;
        }

        templateId = slot.memberTemplateId;
        var members = StartingTemplateLoader.LoadMembers(templateId);
        if (members.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(slot.memberId))
        {
            templateMember = members.FirstOrDefault(m =>
                slot.memberId.Equals(m.memberId, StringComparison.Ordinal));
        }

        templateMember ??= members[0];
        return true;
    }

    private static void ApplyTemplateFields(MemberState member, MemberState templateMember)
    {
        member.accountName = templateMember.accountName;
        member.rarity = templateMember.rarity;
        member.trueRarity = templateMember.trueRarity;
        member.bio = templateMember.bio;
        member.labels = new List<string>(templateMember.labels);
        member.traitIds = new List<string>(templateMember.traitIds);
        member.cardBackdrop = templateMember.cardBackdrop;
        member.legionBelonging = templateMember.legionBelonging;
        member.energy = templateMember.energy;
        member.wisdom = templateMember.wisdom;
        member.accountBuildScore = templateMember.accountBuildScore;
        if (string.IsNullOrWhiteSpace(member.identityCode))
        {
            member.identityCode = templateMember.identityCode;
        }

        if (string.IsNullOrWhiteSpace(member.accountSuffix))
        {
            member.accountSuffix = templateMember.accountSuffix;
        }

        if (string.IsNullOrWhiteSpace(member.equippedHullId) && !string.IsNullOrWhiteSpace(templateMember.equippedHullId))
        {
            member.equippedHullId = templateMember.equippedHullId;
        }
    }
}
