using TopDog.Content.Modules;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class StructureDisruptMissileTests
{
    [Test]
    public void ModuleCatalog_RecognizesStructureDisrupt()
    {
        Assert.That(ModuleCatalog.IsMissileModuleId("mod_structure_disrupt_s"), Is.True);
    }

    [Test]
    public void Profile_IsStructureOnlyBallistic()
    {
        var mod = new ModuleDef
        {
            moduleId = "mod_structure_disrupt_s",
            missileAoeBaseDamage = 30,
            missileAoeZeroRadiusM = 20000,
            missileAoeStructureOnly = true,
        };
        var profile = MissileProjectileProfile.FromModule(mod);
        Assert.That(profile.IsBallistic, Is.True);
        Assert.That(profile.AoeStructureOnly, Is.True);
    }
}
