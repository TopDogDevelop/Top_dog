using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class RecruitServiceTests
{
    [Test]
    public void Start_ThenTick_CreatesMembers()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS, currentSolarSystemId = "sys_a" };
        var echo = RecruitService.Start(state, null);
        Assert.That(echo, Does.Contain("20"));
        Assert.That(state.recruitProgressSec, Is.EqualTo(20f).Within(0.01f));

        var ships = ShipRegistry.LoadDefault();
        var traits = TraitCatalog.Empty();
        var rng = new Random(42);
        while (state.recruitProgressSec > 0f)
        {
            RecruitService.Tick(state, 5f, traits, rng, ships);
        }
        Assert.That(state.members.Count, Is.GreaterThan(0));
        Assert.That(state.lastRecruitSummary, Does.Contain("招新完成"));
    }
}
