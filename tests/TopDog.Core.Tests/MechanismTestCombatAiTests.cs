using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MechanismTestCombatAiTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void Catalog_ListAll_SortedByScenarioOrder()
    {
        var list = MechanismTestCatalog.ListAll();
        Assert.That(list, Has.Count.EqualTo(10));
        Assert.That(list[0].scenarioId, Is.EqualTo("mt_board_summon"));
        Assert.That(list[5].scenarioId, Is.EqualTo("mt_remote_repair"));
        Assert.That(list.Select(s => s.scenarioOrder), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
        Assert.That(list[9].scenarioId, Is.EqualTo("mt_intra_scene_warp"));
    }

    [Test]
    public void IroncoffinSalvo_DealsDamageWhenStoppedInRange()
    {
        var state = new GameState { combatRealtimeActive = true };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        state.battlefields.Add(bf);
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var dreadHull = ships.FindHull("hull_dread_ironcoffin")!;
        var frigateHull = ships.FindHull("hull_frigate_shortlegwolf")!;

        var enemy = new BattlefieldUnit
        {
            unitId = "dread",
            side = UnitSide.ENEMY,
            alive = true,
            arrivalAtSec = 0f,
            x = 10_000f,
            facingRad = MathF.PI,
            fittedModules =
            {
                ["atk_1"] = "mod_hybrid_gun_xl",
                ["atk_2"] = "mod_hybrid_gun_xl",
            },
        };
        ModuleRuntime.ApplyToUnit(enemy, dreadHull, modules);
        enemy.aiOrder = UnitAiOrder.STOP;
        enemy.targetUnitId = "wolf";

        var friendly = new BattlefieldUnit
        {
            unitId = "wolf",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = -10_000f,
        };
        ModuleRuntime.ApplyToUnit(friendly, frigateHull, modules);
        bf.units.Add(enemy);
        bf.units.Add(friendly);

        for (var i = 0; i < 30; i++)
        {
            BattlefieldSystem.Tick(state, modules, ships, 1f);
        }

        var ledger = CombatDamageLedger.GetLedger(bf, "wolf");
        Assert.That(ledger?.totalDamageTaken, Is.GreaterThan(0f));
    }

    [Test]
    public void FieldAura_EnemyIroncoffinDealsDamageAfterAiEngage()
    {
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_field_aura");
        var state = core.State;
        var bf = state.battlefields[0];
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();

        var enemy = bf.units.First(u =>
            u.side == UnitSide.ENEMY
            && "hull_dread_ironcoffin".Equals(u.hullId, StringComparison.Ordinal));
        Assert.That(enemy.salvoRoundDmg, Is.GreaterThan(0f));

        for (var i = 0; i < 5; i++)
        {
            BattlefieldSystem.Tick(state, modules, ships, 1f);
        }

        Assert.That(enemy.targetUnitId, Is.Not.Null);

        var totalDamage = 0f;
        for (var i = 0; i < 1200; i++)
        {
            BattlefieldSystem.Tick(state, modules, ships, 0.1f);
            totalDamage = 0f;
            foreach (var u in bf.units)
            {
                if (u.side != UnitSide.FRIENDLY || u.parentUnitId != null || u.unitId == null)
                {
                    continue;
                }

                totalDamage += CombatDamageLedger.GetLedger(bf, u.unitId)?.totalDamageTaken ?? 0f;
            }

            if (totalDamage > 0f)
            {
                break;
            }
        }

        Assert.That(totalDamage, Is.GreaterThan(0f), "Expected enemy iron coffin salvo damage on player fleet");
        Assert.That(enemy.aiOrder, Is.AnyOf(UnitAiOrder.STOP, UnitAiOrder.APPROACH, UnitAiOrder.AWAY));
    }
}
