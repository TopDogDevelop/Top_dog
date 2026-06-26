using TopDog.Content.Modules;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MissileProjectileTests
{
    [Test]
    public void ComputeAoeDamage_LinearZeroAt7km()
    {
        var profile = new MissileProjectileProfile
        {
            ModuleId = "test_missile",
            AoeBaseDamage = 10000,
            AoeZeroRadiusM = 7000,
        };
        Assert.That(MissileProjectileProfile.ComputeAoeDamage(0f, profile), Is.EqualTo(10000));
        Assert.That(MissileProjectileProfile.ComputeAoeDamage(3500f, profile), Is.EqualTo(5000));
        Assert.That(MissileProjectileProfile.ComputeAoeDamage(7000f, profile), Is.EqualTo(0));
        Assert.That(MissileProjectileProfile.ComputeAoeDamage(8000f, profile), Is.EqualTo(0));
    }

    [Test]
    public void ChaosMissileJson_MatchesFirstPackProfile()
    {
        var reg = ModuleRegistry.LoadDefault();
        var mod = reg.Find("mod_chaos_missile_l");
        if (mod == null)
        {
            mod = ModuleCatalog.Resolve(ModuleRegistry.Empty(), "mod_chaos_missile_l");
        }
        Assert.That(mod, Is.Not.Null);
        var profile = MissileProjectileProfile.FromModule(mod);
        Assert.That(profile.IsBallistic, Is.True);
        Assert.That(profile.StructureHp, Is.EqualTo(1000f).Within(0.01f));
        Assert.That(profile.AoeZeroRadiusM, Is.EqualTo(7000f).Within(0.01f));
        Assert.That(profile.AoeBaseDamage, Is.EqualTo(10000f).Within(0.01f));
    }

    [Test]
    public void DetonateAoE_SkipsBeyondRadius()
    {
        var state = new TopDog.Sim.State.GameState();
        var bf = new BattlefieldState { battlefieldId = "bf-test", timeSec = 1f };
        var missile = new BattlefieldUnit
        {
            unitId = "msl-1",
            x = 0f,
            y = 0f,
            z = 0f,
            missileModuleId = "mod_chaos_missile_l",
            missileProfileSnapshot = new MissileProjectileProfile
            {
                ModuleId = "mod_chaos_missile_l",
                AoeBaseDamage = 10000,
                AoeZeroRadiusM = 7000,
            },
        };
        var near = new BattlefieldUnit
        {
            unitId = "tgt-near",
            x = 1000f,
            y = 0f,
            z = 0f,
            structureHp = 5000f,
            structureMax = 5000f,
            alive = true,
            arrivalAtSec = 0f,
        };
        var far = new BattlefieldUnit
        {
            unitId = "tgt-far",
            x = 8000f,
            y = 0f,
            z = 0f,
            structureHp = 5000f,
            structureMax = 5000f,
            alive = true,
            arrivalAtSec = 0f,
        };
        bf.units.Add(missile);
        bf.units.Add(near);
        bf.units.Add(far);

        MissileProjectileService.DetonateAoE(state, bf, missile, missile.missileProfileSnapshot!);

        Assert.That(near.structureHp, Is.LessThan(5000f));
        Assert.That(far.structureHp, Is.EqualTo(5000f));
    }
}
