using TopDog.Content;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class DisplayLabelsTests
{
    [Test]
    public void TonnageBilingual_ChineseFirst()
    {
        Assert.That(DisplayLabels.TonnageBilingual("CARRIER"), Is.EqualTo("航空母舰 · Carrier"));
    }

    [Test]
    public void JoinBilingual_SkipsDuplicate()
    {
        Assert.That(DisplayLabels.JoinBilingual("护卫舰", "护卫舰"), Is.EqualTo("护卫舰"));
    }
}
