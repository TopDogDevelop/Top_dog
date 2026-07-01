using TopDog.Content.Starting;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Lobby;

/// <summary>约战名册模版行键（模版 id + 身份码 + 账号序号）。</summary>
public static class SkirmishTemplateRows
{
    public static string RowKey(string templateId, MemberState member)
    {
        var identity = IdentityCodes.Of(member);
        var suffix = member.accountSuffix ?? "";
        return $"{templateId}:{identity}:{suffix}";
    }

    public static string DisplayLabel(TemplateCatalogEntry template, MemberState member)
    {
        var tplName = template.displayName ?? template.templateId ?? "?";
        var suffix = string.IsNullOrWhiteSpace(member.accountSuffix) ? "" : "-" + member.accountSuffix;
        return $"{tplName} · {member.name} ({IdentityCodes.Of(member)}{suffix})";
    }

    public static bool IsAlreadyOnRoster(IReadOnlyList<SkirmishRosterSlot> roster, string rowKey) =>
        roster.Any(s => rowKey.Equals(s.memberTemplateRowId, StringComparison.Ordinal));

    public static bool TryParseRowKey(string rowKey, out string templateId, out string identityCode, out string accountSuffix)
    {
        templateId = "";
        identityCode = "";
        accountSuffix = "";
        if (string.IsNullOrWhiteSpace(rowKey))
        {
            return false;
        }

        var parts = rowKey.Split(':');
        if (parts.Length < 3)
        {
            return false;
        }

        templateId = parts[0];
        identityCode = parts[1];
        accountSuffix = parts[2];
        return !string.IsNullOrWhiteSpace(templateId)
            && !string.IsNullOrWhiteSpace(identityCode)
            && !string.IsNullOrWhiteSpace(accountSuffix);
    }
}
