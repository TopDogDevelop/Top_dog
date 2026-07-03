using TopDog.Content.Members;
using TopDog.Foundation.Io;
using TopDog.Sim.Member;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MemberPortraitCatalogTests
{
    private string? _tempRoot;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "topdog-portrait-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "content", "member_portrait_templates", "pool"));
        AppRoot.SetOverrideRoot(_tempRoot);
        MemberPortraitCatalog.Refresh();
    }

    [TearDown]
    public void TearDown()
    {
        MemberPortraitCatalog.ClearExtraScanRoots();
        AppRoot.InvalidateCache();
        if (_tempRoot != null && Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }

    [Test]
    public void RollRandomRef_PicksAnyImageInTemplatesTree()
    {
        File.WriteAllBytes(Path.Combine(_tempRoot!, "content", "member_portrait_templates", "a.png"), MinimalPng());
        File.WriteAllBytes(
            Path.Combine(_tempRoot, "content", "member_portrait_templates", "pool", "b.jpg"),
            MinimalPng());
        MemberPortraitCatalog.Refresh();

        var refs = MemberPortraitCatalog.PoolRefs();
        Assert.That(refs.Count, Is.EqualTo(2));
        Assert.That(refs, Does.Contain("a.png"));
        Assert.That(refs, Does.Contain("pool/b.jpg"));

        var picked = MemberPortraitCatalog.RollRandomRef(new Random(1));
        Assert.That(picked, Is.Not.Null);
        Assert.That(MemberPortraitCatalog.ResolveRef(picked), Does.Exist);
    }

    [Test]
    public void PoolRefs_DedupesSameFileAcrossRefresh()
    {
        var path = Path.Combine(_tempRoot!, "content", "member_portrait_templates", "one.png");
        File.WriteAllBytes(path, MinimalPng());
        MemberPortraitCatalog.Refresh();
        MemberPortraitCatalog.Refresh();
        Assert.That(MemberPortraitCatalog.PoolRefs().Count, Is.EqualTo(1));
    }

    [Test]
    public void RegisterScanRoot_ResolvesRefsOutsideAppRoot()
    {
        var extra = Path.Combine(_tempRoot!, "external_pool");
        Directory.CreateDirectory(extra);
        File.WriteAllBytes(Path.Combine(extra, "external.png"), MinimalPng());
        MemberPortraitCatalog.RegisterScanRoot(extra);
        MemberPortraitCatalog.Refresh();

        Assert.That(MemberPortraitCatalog.PoolRefs(), Does.Contain("external.png"));
        Assert.That(MemberPortraitCatalog.ResolveRef("external.png"), Does.Exist);
    }

    [Test]
    public void ProceduralIdentitySetup_UsesPoolWhenAvailable()
    {
        File.WriteAllBytes(
            Path.Combine(_tempRoot!, "content", "member_portrait_templates", "hero.png"),
            MinimalPng());
        MemberPortraitCatalog.Refresh();

        var anchor = new TopDog.Sim.State.MemberState { identityCode = "10009999" };
        ProceduralIdentitySetup.ApplyShared(anchor, new Random(3));
        Assert.That(anchor.portraitRef, Is.EqualTo("hero.png"));
    }

    private static byte[] MinimalPng() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
}
