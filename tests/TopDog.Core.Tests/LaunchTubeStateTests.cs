using TopDog.Content.Modules;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class LaunchTubeStateTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();
    [Test]
    public void MissileLaunch_Activated_Then_Destroyed_Depleted()
    {
        var modules = ModuleRegistry.LoadDefault();
        var modId = "mod_structure_disrupt_s";
        Assert.That(modules.Resolve(modId), Is.Not.Null);

        var launcher = new BattlefieldUnit
        {
            unitId = "carrier",
            fittedModules = { ["tube_1"] = modId },
        };
        LaunchTubeStateService.InitTubeStates(launcher, modules);
        Assert.That(launcher.tubeStates["tube_1"], Is.EqualTo(LaunchTubeState.Inactive));

        LaunchTubeStateService.OnMissileLaunched(launcher, "tube_1");
        Assert.That(launcher.tubeStates["tube_1"], Is.EqualTo(LaunchTubeState.Activated));

        LaunchTubeStateService.OnConsumableDestroyed(launcher, "tube_1");
        Assert.That(launcher.tubeStates["tube_1"], Is.EqualTo(LaunchTubeState.Depleted));
    }

    [Test]
    public void Producer_Resets_Depleted_Tube()
    {
        var modules = ModuleRegistry.LoadDefault();
        var wingId = "mod_strike_wing_a_l";
        var assemblyId = "mod_strike_assembly_l";
        Assert.That(modules.Resolve(wingId), Is.Not.Null);
        Assert.That(modules.Resolve(assemblyId), Is.Not.Null);

        var ally = new BattlefieldUnit
        {
            unitId = "ally",
            tonnageClass = "FRIGATE",
            fittedModules = { ["tube_1"] = wingId },
            tubeStates = { ["tube_1"] = LaunchTubeState.Depleted },
        };
        var producer = new BattlefieldUnit { unitId = "prod", fittedModules = { ["fn_1"] = assemblyId } };
        var producerMod = modules.Resolve(assemblyId)!;

        Assert.That(
            LaunchTubeStateService.TryResetDepleted(producer, ally, producerMod, modules),
            Is.True);
        Assert.That(ally.tubeStates["tube_1"], Is.EqualTo(LaunchTubeState.Inactive));
    }
}
