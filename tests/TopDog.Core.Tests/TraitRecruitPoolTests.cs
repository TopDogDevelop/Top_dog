using TopDog.Content.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TraitRecruitPoolTests
{
    [Test]
    public void SheepLineTraits_AreGlobal_NotRecruitPool()
    {
        var cat = TraitCatalog.LoadDefault();
        Assert.That(cat.IsInRecruitPool("trait_devotion"), Is.False);
        Assert.That(cat.IsInRecruitPool("trait_multibox"), Is.True);
        Assert.That(cat.Find("trait_devotion")!.mechanismId, Is.EqualTo("devotion"));
        Assert.That(cat.Find("trait_devotion")!.presentationTags, Does.Not.Contain("vip_invest"));
    }
}
