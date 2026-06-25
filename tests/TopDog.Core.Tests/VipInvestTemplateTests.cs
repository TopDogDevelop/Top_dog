using TopDog.Content.Starting;
using TopDog.Lobby;
using TopDog.Sim.Member;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class VipInvestTemplateTests
{
    [Test]
    public void TemplateVipInvest_Loads38Members_WithTierStatsAndTraits()
    {
        var members = StartingTemplateLoader.LoadMembers("template_vip_invest");
        Assert.That(members, Has.Count.EqualTo(38));

        var sheep = members.First(m => m.name == "绵羊伸腿");
        Assert.That(sheep.legionBelonging, Is.EqualTo(100));
        Assert.That(sheep.energy, Is.EqualTo(30));
        Assert.That(sheep.traitIds, Does.Contain("trait_board_favor"));
        Assert.That(sheep.traitIds, Does.Contain("trait_fool_loyal"));
        Assert.That(sheep.traitIds, Does.Contain("trait_multibox"));

        var village = members.First(m => m.name == "羊村星星");
        Assert.That(village.legionBelonging, Is.EqualTo(85));
        Assert.That(village.traitIds, Does.Contain("trait_devotion"));
        Assert.That(village.traitIds, Does.Not.Contain("trait_board_favor"));

        var depth = members.First(m => m.name == "深度追踪");
        Assert.That(depth.traitIds, Is.EqualTo(new List<string> { "trait_multibox" }));

        var bei = members.First(m => m.name == "狈头军师");
        Assert.That(bei.traitIds, Does.Contain("trait_recruit_officer"));
        Assert.That(bei.traitIds, Does.Not.Contain("trait_multibox"));
    }

    [Test]
    public void ListMemberTemplates_IncludesVipInvest_WithoutAssetTemplate()
    {
        var templates = ContentCatalog.ListMemberTemplates();
        var vip = templates.FirstOrDefault(t => t.templateId == "template_vip_invest");
        Assert.That(vip, Is.Not.Null);
        Assert.That(vip!.displayName, Is.EqualTo("VIP投资集团"));
        Assert.That(vip.memberCount, Is.EqualTo(38));
        Assert.That(vip.lobbyVisible, Is.True);
        Assert.That(string.IsNullOrWhiteSpace(vip.assetTemplateId), Is.True);
    }
}
