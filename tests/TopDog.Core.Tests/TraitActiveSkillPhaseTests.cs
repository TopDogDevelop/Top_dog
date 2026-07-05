using TopDog.Content.Traits;
using TopDog.Sim.Traits;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TraitActiveSkillPhaseTests
{
    [Test]
    public void GetActiveSkillPhase_ReadsTraitJson()
    {
        var catalog = TraitCatalog.LoadDefault();
        Assert.That(
            TraitActiveSkillService.GetActiveSkillPhase(
                TraitActiveSkillService.BoardSummonTraitId,
                catalog),
            Is.EqualTo(TraitActiveSkillPhase.RealtimeCombat));
        Assert.That(
            TraitActiveSkillService.GetActiveSkillPhase(
                TraitActiveSkillService.PlanningSupportTraitId,
                catalog),
            Is.EqualTo(TraitActiveSkillPhase.Operations));
    }

    [Test]
    public void GetActiveSkillPhase_FallsBackWhenJsonMissing()
    {
        Assert.That(
            TraitActiveSkillService.GetActiveSkillPhase(TraitActiveSkillService.BoardSummonTraitId),
            Is.EqualTo(TraitActiveSkillPhase.RealtimeCombat));
        Assert.That(
            TraitActiveSkillService.GetActiveSkillPhase("trait_direct_possess"),
            Is.Null);
    }
}
