using TopDog.Content.Map;
using TopDog.Foundation.Io;
using TopDog.Lobby;

namespace TopDog.Tests;

public sealed class Bug6MapCatalogTests
{
    [Test]
    public void LoadsBug6MapWhenPresent()
    {
        var mapDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "TopDog.Unity", "Assets", "StreamingAssets", "maps", "eve_bug6-x.topdog-map"));
        if (!Directory.Exists(Path.Combine(mapDir, "systems")))
        {
            Assert.Ignore("BUG6 map not copied to StreamingAssets");
        }

        AppRoot.SetOverrideRoot(Path.GetDirectoryName(Path.GetDirectoryName(mapDir))!);
        var map = ContentCatalog.LoadMap(mapDir);
        Assert.That(map.Project.systems.Count, Is.EqualTo(7));
        Assert.That(map.Project.bridges.Count, Is.GreaterThanOrEqualTo(6));
    }
}
