using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class HullLicenseFittingTests
{
    [Test]
    public void ModuleFitsHullLicenses_RequiresSubset()
    {
        var hull = new HullDef { hullLicenses = new[] { "logistics", "boarding" } };
        var ok = new ModuleDef { requiredHullLicenses = new[] { "logistics" } };
        var bad = new ModuleDef { requiredHullLicenses = new[] { "shield_fleet" } };
        Assert.That(HullLicenseCatalog.ModuleFitsHullLicenses(hull, ok), Is.True);
        Assert.That(HullLicenseCatalog.ModuleFitsHullLicenses(hull, bad), Is.False);
    }

    [Test]
    public void BoardingModule_BackCompatAllowedModuleKinds()
    {
        var hull = new HullDef { allowedModuleKinds = new[] { "boarding_module" } };
        var mod = new ModuleDef { moduleKind = "boarding_module" };
        Assert.That(HullLicenseCatalog.ModuleFitsHullLicenses(hull, mod), Is.True);
    }
}
