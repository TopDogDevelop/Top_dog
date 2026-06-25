using TopDog.Content.Map;

namespace TopDog.Tests;

public sealed class RegionGraphLoaderTests
{
    [Test]
    public void LoadsSeedContentMap()
    {
        var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "content", "map");
        var loader = new RegionGraphLoader();
        var result = loader.Load(root);
        Assert.That(result.IsOk, Is.True, () => string.Join("; ", result.Errors));
        Assert.That(result.Value!.Project.systems.Count, Is.EqualTo(3));
        Assert.That(result.Value.Project.bridges.Count, Is.EqualTo(2));
    }
}
