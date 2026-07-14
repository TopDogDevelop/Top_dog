using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FieldAuraMechanismDumpTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void MtFieldAura_AfterFieldTick_ArmorPoolsOnGreyWolf()
    {
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_field_aura");
        var state = core.State;
        var bf = state.battlefields[0];
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();

        var grey = bf.units.FirstOrDefault(u =>
            "hull_cruiser_greywolf_guard".Equals(u.hullId, StringComparison.Ordinal));
        Assert.That(grey, Is.Not.Null);

        var wolvesBefore = bf.units
            .Where(u => "hull_frigate_shortlegwolf".Equals(u.hullId, StringComparison.Ordinal))
            .ToList();
        Assert.That(wolvesBefore, Has.Count.GreaterThan(0));

        var totalArmorBefore = bf.units.Sum(u => u.armorHp);
        TestContext.WriteLine($"BEFORE tick: grey={grey!.armorHp}/{grey.armorMax} wolves={wolvesBefore.Count} fleetArmorSum={totalArmorBefore}");

        for (var i = 0; i < 3; i++)
        {
            FieldAuraService.Tick(state, bf, modules, ships, 1.1f);
            TestContext.WriteLine(
                $"t={bf.timeSec:F1} grey={grey.armorHp}/{grey.armorMax} dominant={grey.fieldAuraDominant} enabled={grey.fieldAuraEnabledAtSec}");
            foreach (var w in wolvesBefore.Take(3))
            {
                TestContext.WriteLine(
                    $"  wolf {w.displayName} armor={w.armorHp}/{w.armorMax} host={w.armorFieldHostUnitId}");
            }
        }

        var totalArmorAfter = bf.units.Sum(u => u.armorHp);
        var wolvesInField = wolvesBefore.Count(w => grey.unitId!.Equals(w.armorFieldHostUnitId, StringComparison.Ordinal));
        TestContext.WriteLine($"AFTER: fleetArmorSum={totalArmorAfter} wolvesInField={wolvesInField}/{wolvesBefore.Count}");

        Assert.That(grey.fieldAuraEnabledAtSec, Is.GreaterThan(0f));
        Assert.That(grey.armorHp, Is.GreaterThan(4000f), "Grey wolf should pool protege armor");
        Assert.That(wolvesInField, Is.EqualTo(wolvesBefore.Count));
        Assert.That(totalArmorAfter, Is.EqualTo(totalArmorBefore).Within(0.01f), "Fleet total armor should be conserved");
    }

    [Test]
    public void MtShieldFusion_AfterFieldTick_FrigatesFuseShieldsToWhitewolf()
    {
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_shield_fusion");
        var state = core.State;
        var bf = state.battlefields[0];
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();

        var wolf = bf.units.FirstOrDefault(u =>
            "hull_cruiser_whitewolf_guard".Equals(u.hullId, StringComparison.Ordinal));
        Assert.That(wolf, Is.Not.Null);
        Assert.That(wolf!.tonnageClass, Is.EqualTo("CRUISER"));
        Assert.That(wolf!.fittedModules.Values, Does.Contain("mod_shield_fusion_l"));
        var shieldMod = FieldAuraService.FindFieldModule(wolf, modules, "shield_fusion_field");
        Assert.That(shieldMod, Is.Not.Null, "FindFieldModule should resolve fn_4 shield fusion");

        var frigs = bf.units
            .Where(u => "hull_frigate_shortlegwolf".Equals(u.hullId, StringComparison.Ordinal))
            .ToList();
        Assert.That(frigs, Has.Count.GreaterThan(0));

        var shieldBefore = frigs.Sum(f => f.shieldHp);
        TestContext.WriteLine(
            $"BEFORE: wolf={wolf!.shieldHp}/{wolf.shieldMax} frigShieldSum={shieldBefore}");

        for (var i = 0; i < 3; i++)
        {
            FieldAuraService.Tick(state, bf, modules, ships, 1.1f);
        }

        var fused = frigs.Count(f => wolf.unitId!.Equals(f.shieldFieldHostUnitId, StringComparison.Ordinal));
        TestContext.WriteLine(
            $"AFTER: wolf={wolf.shieldHp}/{wolf.shieldMax} fused={fused}/{frigs.Count} dominant={wolf.fieldAuraDominant}");
        foreach (var f in frigs.Take(3))
        {
            TestContext.WriteLine($"  {f.displayName} shield={f.shieldHp}/{f.shieldMax} host={f.shieldFieldHostUnitId}");
        }

        Assert.That(wolf.fieldAuraEnabledAtSec, Is.GreaterThan(0f));
        Assert.That(wolf.fieldAuraDominant, Is.True);
        Assert.That(fused, Is.EqualTo(frigs.Count));
        Assert.That(wolf.shieldHp, Is.GreaterThan(5000f), "Whitewolf should pool frigate shields");
    }
}
