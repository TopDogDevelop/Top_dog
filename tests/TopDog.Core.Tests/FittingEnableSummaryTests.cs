using System.Collections.Generic;
using TopDog.Content.Ships;
using TopDog.Sim.Ship;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FittingEnableSummaryTests
{
    [Test]
    public void SimultaneousEnableLimit_defaults_to_slot_count_when_unset()
    {
        var hull = new HullDef
        {
            attackSlots = 2,
            functionSlots = 4,
            defenseSlots = 3,
            passiveSlots = 2,
            launchTubeSlots = 4,
        };

        var snapshot = FittingEnableSummary.Compute(hull, new Dictionary<string, string>());
        Assert.That(snapshot.SlotCount, Is.EqualTo(15));
        Assert.That(snapshot.SimultaneousEnableLimit, Is.EqualTo(15));
        Assert.That(snapshot.EnablePoolFull, Is.True);
    }

    [Test]
    public void SimultaneousEnableLimit_respects_hull_cap()
    {
        var hull = new HullDef
        {
            attackSlots = 4,
            functionSlots = 3,
            simultaneousEnableLimit = 4,
        };

        var snapshot = FittingEnableSummary.Compute(hull, new Dictionary<string, string>());
        Assert.That(snapshot.SlotCount, Is.EqualTo(7));
        Assert.That(snapshot.SimultaneousEnableLimit, Is.EqualTo(4));
        Assert.That(snapshot.EnablePoolFull, Is.False);
    }

    [Test]
    public void Compute_counts_fitted_modules()
    {
        var hull = new HullDef { attackSlots = 2, functionSlots = 1 };
        var fit = new Dictionary<string, string>
        {
            ["atk_0"] = "mod_a",
            ["fn_0"] = "mod_b",
        };

        var snapshot = FittingEnableSummary.Compute(hull, fit);
        Assert.That(snapshot.EquippedCount, Is.EqualTo(2));
    }
}
