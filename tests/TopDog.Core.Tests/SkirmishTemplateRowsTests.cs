using TopDog.Content.Starting;
using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class SkirmishTemplateRowsTests
{
    [Test]
    public void RowKey_DistinguishesSameIdentityDifferentSuffix()
    {
        var a = new MemberState { identityCode = "10000009", accountSuffix = "01", name = "清风客" };
        var b = new MemberState { identityCode = "10000009", accountSuffix = "02", name = "月明诗" };
        Assert.That(SkirmishTemplateRows.RowKey("template_1", a), Is.Not.EqualTo(SkirmishTemplateRows.RowKey("template_1", b)));
    }

    [Test]
    public void TryParseRowKey_RoundTripsTemplateRow()
    {
        var key = SkirmishTemplateRows.RowKey("template_1", new MemberState
        {
            identityCode = "10000009",
            accountSuffix = "02",
        });
        Assert.That(SkirmishTemplateRows.TryParseRowKey(key, out var tpl, out var id, out var suf), Is.True);
        Assert.That(tpl, Is.EqualTo("template_1"));
        Assert.That(id, Is.EqualTo("10000009"));
        Assert.That(suf, Is.EqualTo("02"));
    }

    [Test]
    public void MemberTemplates_IncludesNestedTemplate1_WithAllMemberRows()
    {
        var root = RepoContentRoot();
        AppRoot.SetOverrideRoot(root);
        var templates = SkirmishLobbyCatalog.MemberTemplates();
        var template1 = templates.FirstOrDefault(t => t.templateId == "template_1");
        Assert.That(template1, Is.Not.Null);
        var members = StartingTemplateLoader.LoadMembers("template_1");
        Assert.That(members.Count, Is.GreaterThan(1));
        var pineappleRows = members.Where(m => m.name is "清风客" or "月明诗").ToList();
        Assert.That(pineappleRows.Count, Is.EqualTo(2));
    }

    [Test]
    public void MemberTemplates_IncludesVipInvest_AndResolvesNestedCsv()
    {
        AppRoot.SetOverrideRoot(RepoContentRoot());
        var templates = SkirmishLobbyCatalog.MemberTemplates();
        var ids = templates.Select(t => t.templateId).ToHashSet(StringComparer.Ordinal);
        Assert.That(ids, Does.Contain("template_1"));
        Assert.That(ids, Does.Contain("template_vip_invest"));
        Assert.That(StartingTemplateLoader.LoadMembers("template_vip_invest").Count, Is.GreaterThan(10));
    }

    [Test]
    public void ResolveTemplateCsvPath_FindsNestedStartingTemplatesFolder()
    {
        AppRoot.SetOverrideRoot(RepoContentRoot());
        Assert.That(StartingTemplateLoader.LoadMembers("template_1").Count, Is.GreaterThan(10));
        Assert.That(StartingTemplateLoader.LoadMembers("template_vip_invest").Count, Is.GreaterThan(10));
    }

    private static string RepoContentRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
}
