using TopDog.Content;
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

    [Test]
    public void ObjectOverviewLine1_UsesChineseHullMemberSideLegion()
    {
        var state = new GameState
        {
            legions =
            {
                new LegionState { legionId = "leg-a", displayName = "红隼军团" },
            },
            members =
            {
                new MemberState { memberId = "m1", name = "张三", legionId = "leg-a" },
            },
        };
        var unit = new BattlefieldUnit
        {
            unitId = "u1",
            memberId = "m1",
            legionId = "leg-a",
            side = UnitSide.FRIENDLY,
            tonnageClass = "FRIGATE",
        };

        var line = DisplayLabels.ObjectOverviewLine1(state, unit, null);
        Assert.That(line, Is.EqualTo("护卫舰-张三-友好-红隼军团"));
        Assert.That(line, Does.Not.Contain("Frigate"));
    }
}
