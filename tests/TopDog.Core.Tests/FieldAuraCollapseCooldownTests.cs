using TopDog.Content.Modules;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FieldAuraCollapseCooldownTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void ModuleJson_CollapseCooldown_IsThirtySeconds()
    {
        var modules = ModuleRegistry.LoadDefault();
        var mod = modules.Resolve("mod_armor_link_s");
        Assert.That(mod, Is.Not.Null);
        Assert.That(mod!.fieldCollapseCooldownSec, Is.EqualTo(30f));
        Assert.That(FieldAuraCollapse.ResolveCollapseCooldownSec(mod), Is.EqualTo(30f));
    }

    [Test]
    public void AfterCooldown_AutoResumes_WhenSlotEnabledAndPoolHasHp()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 100f };
        var holder = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 500f,
            armorMax = 1000f,
            fieldAuraEnabledAtSec = 10f,
            fieldAuraArmorDominant = true,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        bf.units.Add(holder);

        FieldAuraCollapse.Collapse(holder, bf, modules, "armor_link_field");
        Assert.That(holder.fieldAuraEnabledAtSec, Is.EqualTo(0f));
        Assert.That(holder.fieldAuraResumeAfterCooldown, Is.True);
        Assert.That(holder.fieldAuraCollapseCooldownSec, Is.EqualTo(130f).Within(0.01f));

        bf.timeSec = 130f;
        holder.armorHp = 500f;
        FieldAuraService.TryResumeAfterCollapseCooldown(bf, modules);

        Assert.That(holder.fieldAuraEnabledAtSec, Is.GreaterThan(0f));
        Assert.That(holder.fieldAuraResumeAfterCooldown, Is.False);
    }

    [Test]
    public void AfterCooldown_DoesNotResume_WhenSlotQuotaDisabled()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 200f };
        var holder = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 500f,
            armorMax = 1000f,
            fieldAuraResumeAfterCooldown = true,
            fieldAuraCollapseCooldownSec = 100f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        holder.disabledModuleSlots.Add("fn_1");
        holder.quotaForcedDisabledSlots.Add("fn_1");
        bf.units.Add(holder);

        FieldAuraService.TryResumeAfterCollapseCooldown(bf, modules);

        Assert.That(holder.fieldAuraEnabledAtSec, Is.EqualTo(0f),
            "限额关掉的槽不得为续开而强行启用");
        Assert.That(holder.fieldAuraResumeAfterCooldown, Is.True);
        Assert.That(FieldAuraService.FindFieldModule(holder, modules, "armor_link_field"), Is.Null);
    }

    [Test]
    public void AfterCooldown_Waits_WhenArmorStillZero()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 200f };
        var holder = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 0f,
            armorMax = 1000f,
            fieldAuraResumeAfterCooldown = true,
            fieldAuraCollapseCooldownSec = 100f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        bf.units.Add(holder);

        FieldAuraService.TryResumeAfterCollapseCooldown(bf, modules);

        Assert.That(holder.fieldAuraEnabledAtSec, Is.EqualTo(0f));
        Assert.That(holder.fieldAuraResumeAfterCooldown, Is.True);
    }

    [Test]
    public void Collapse_UsesModuleField_NotSeparateBalanceFile()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 100f };
        var holder = new BattlefieldUnit
        {
            unitId = "host",
            side = UnitSide.FRIENDLY,
            alive = true,
            armorHp = 0f,
            armorMax = 1000f,
            fieldAuraEnabledAtSec = 1f,
            fieldAuraArmorDominant = true,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        bf.units.Add(holder);

        FieldAuraCollapse.Collapse(holder, bf, modules, "armor_link_field");

        Assert.That(holder.fieldAuraCollapseCooldownSec, Is.EqualTo(130f).Within(0.01f));
        Assert.That(holder.fieldAuraEnabledAtSec, Is.EqualTo(0f));
    }
}
