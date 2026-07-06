using TopDog.Content.Modules;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BedrockArmorTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void FlatReduction_StacksPerModule()
    {
        var modules = ModuleRegistry.LoadDefault();
        var mod = modules.Find("mod_bedrock_armor_xl");
        Assert.That(mod?.damageMitigationKind, Is.EqualTo("bedrock_armor_flat"));

        var target = new BattlefieldUnit
        {
            fittedModules =
            {
                ["def_1"] = "mod_bedrock_armor_xl",
                ["def_2"] = "mod_bedrock_armor_xl",
            },
        };

        var ctx = new CombatDamageContext
        {
            target = target,
            rawDamage = 1500f,
            armorDamage = 1000f,
            structureDamage = 500f,
        };

        ctx = DamageMitigationService.ApplyMitigation(ctx, modules);
        Assert.That(ctx.armorDamage, Is.EqualTo(500f));
        Assert.That(ctx.structureDamage, Is.EqualTo(0f));
    }
}

[TestFixture]
public sealed class ReflexArcBlockTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void BlockLayer_AbsorbsShieldDamageOnce()
    {
        var modules = ModuleRegistry.LoadDefault();
        Assert.That(modules.Find("mod_reflex_arc_xl")?.damageMitigationKind,
            Is.EqualTo("reflex_shield_block"));

        var target = new BattlefieldUnit
        {
            shieldMax = 10_000f,
            shieldHp = 10_000f,
            blockShieldLayers = 1,
            fittedModules = { ["def_1"] = "mod_reflex_arc_xl" },
        };

        var ctx = new CombatDamageContext
        {
            target = target,
            rawDamage = 200f,
            shieldDamage = 200f,
        };

        ctx = DamageMitigationService.ApplyMitigation(ctx, modules);
        Assert.That(ctx.shieldDamage, Is.EqualTo(100f).Within(0.01f));
        Assert.That(target.blockShieldLayers, Is.EqualTo(0));
        Assert.That(target.blockLockSec, Is.GreaterThan(0f));
    }
}
