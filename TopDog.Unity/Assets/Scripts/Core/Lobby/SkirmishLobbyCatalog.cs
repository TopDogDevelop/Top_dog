using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Starting;

namespace TopDog.Lobby;

public static class SkirmishLobbyCatalog
{
    public static IReadOnlyList<TemplateCatalogEntry> MemberTemplates()
    {
        var all = ContentCatalog.ListMemberTemplates(lobbyOnly: false);
        var filtered = new List<TemplateCatalogEntry>();
        foreach (var t in all)
        {
            if (string.IsNullOrWhiteSpace(t.templateId))
            {
                continue;
            }

            if (t.templateId.Contains("random_member", StringComparison.OrdinalIgnoreCase)
                || t.templateId.Equals("template_blank", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (StartingTemplateLoader.LoadMembers(t.templateId).Count == 0)
            {
                continue;
            }

            filtered.Add(t);
        }

        return filtered;
    }

    public static IReadOnlyList<string> AllHullIds(ShipRegistry ships)
    {
        var ids = new List<string>();
        foreach (var hull in ships.AllHulls())
        {
            if (!string.IsNullOrWhiteSpace(hull.hullId))
            {
                ids.Add(hull.hullId);
            }
        }

        return ids;
    }

    public static IReadOnlyList<string> AllModuleIds(ModuleRegistry modules)
    {
        var ids = new List<string>(modules.All().Count);
        foreach (var kv in modules.All())
        {
            ids.Add(kv.Key);
        }

        ids.Sort(StringComparer.Ordinal);
        return ids;
    }
}
