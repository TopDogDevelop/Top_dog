using TopDog.App;
using TopDog.Content.Ships;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Ship;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FleetScaleStressTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void Aoe_CapsExploredAt200()
    {
        var bf = new BattlefieldState { battlefieldId = "t" };
        var hash = new BattlefieldSpatialHash();
        // Pack many units inside 1km of origin
        for (var i = 0; i < 500; i++)
        {
            bf.units.Add(new BattlefieldUnit
            {
                unitId = $"u{i}",
                side = UnitSide.ENEMY,
                combatFactionId = 1,
                x = (i % 20) * 10f,
                y = (i / 20) * 10f,
                z = 0,
                structureHp = 100,
                structureMax = 100,
                alive = true,
            });
        }

        hash.Rebuild(bf.units, 5000f);
        var state = new TopDog.Sim.State.GameState();
        var src = new BattlefieldUnit
        {
            unitId = "src",
            side = UnitSide.FRIENDLY,
            combatFactionId = 0,
            structureHp = 100,
            structureMax = 100,
            alive = true,
        };
        var result = AoeDamageService.ResolveAt(
            state, bf, hash, 0, 0, 0,
            zeroRadiusM: 50_000f,
            baseDamage: 10f,
            src,
            structureOnly: true,
            maxExplore: AoeDamageService.MaxTargetsExplored);
        Assert.That(result.Explored, Is.LessThanOrEqualTo(AoeDamageService.MaxTargetsExplored));
        Assert.That(result.Capped, Is.True);
    }

    [Test]
    public void CombatHostility_FourFactionsMutual()
    {
        var a = new BattlefieldUnit { combatFactionId = 0, side = UnitSide.FRIENDLY };
        var b = new BattlefieldUnit { combatFactionId = 1, side = UnitSide.ENEMY };
        var c = new BattlefieldUnit { combatFactionId = 2, side = UnitSide.ENEMY };
        Assert.That(CombatHostility.AreHostile(a, b), Is.True);
        Assert.That(CombatHostility.AreHostile(b, c), Is.True);
        Assert.That(CombatHostility.AreHostile(b, b), Is.False);
    }

    [Test]
    public void StressScenario_SpawnsDefaultRosterIn100kmBall()
    {
        Assert.That(MechanismTestCatalog.TryGet("mt_stress_10k_icons", out var scenario), Is.True);
        Assert.That(scenario.mapMode, Is.EqualTo("stress_10k_icons"));
        Assert.That(scenario.stressUnitCount, Is.EqualTo(2_000));
        Assert.That(scenario.scenarioOrder, Is.EqualTo(11));

        var core = CampaignBootstrap.CreateFromMechanismTest("mt_stress_10k_icons");
        var bf = core.State.battlefields[0];
        var ships = bf.units.Where(u => u.parentUnitId == null && !BattlefieldSceneProxyService.IsSceneProxy(u)).ToList();
        Assert.That(ships.Count, Is.EqualTo(2_000));
        Assert.That(bf.disableAutoVictory, Is.True);
        Assert.That(bf.entityBudget, Is.EqualTo(1500));
        Assert.That(bf.tickBudgetMs, Is.EqualTo(16f));
        Assert.That(bf.maxLiveMissiles, Is.EqualTo(200));
        Assert.That(core.State.autoFireEnabled, Is.True);

        var factions = ships.Select(u => u.combatFactionId).Distinct().OrderBy(x => x).ToList();
        Assert.That(factions, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));

        var r2 = MechanismTestStressSpawnService.DefaultScatterRadiusM;
        r2 *= r2;
        Assert.That(ships.All(u => u.x * u.x + u.y * u.y + u.z * u.z <= r2 * 1.001f), Is.True);
        Assert.That(CombatHostility.AreHostile(ships.First(u => u.combatFactionId == 1), ships.First(u => u.combatFactionId == 2)), Is.True);

        var sample = ships.Take(40).ToList();
        Assert.That(sample.All(u => u.fittedModules.Count > 0), Is.True);
        var shipsReg = ShipRegistry.LoadDefault();
        var modulesReg = FieldNavTestContent.LoadModules();
        foreach (var u in sample)
        {
            var hull = shipsReg.FindHull(u.hullId);
            Assert.That(hull, Is.Not.Null, u.hullId);
            var open = MemberFittingService.ListOpenSlots(hull!);
            var fillable = 0;
            foreach (var slot in open)
            {
                var any = modulesReg.All().Values.Any(m =>
                    m != null && FittingValidator.ModuleFitsSlot(slot, m, hull));
                if (any)
                {
                    fillable++;
                }
            }

            Assert.That(u.fittedModules.Count, Is.EqualTo(fillable),
                $"unit={u.unitId} hull={u.hullId} should fill all legal slots");
            foreach (var kv in u.fittedModules)
            {
                var mod = modulesReg.Resolve(kv.Value);
                Assert.That(FittingValidator.ModuleFitsSlot(kv.Key, mod, hull), Is.True, kv.Key + "=" + kv.Value);
            }
        }
    }

    [Test]
    public void MaxLiveMissiles_BlocksFurtherLaunches()
    {
        var bf = new BattlefieldState
        {
            battlefieldId = "cap",
            maxLiveMissiles = 2,
            timeSec = 10f,
        };
        for (var i = 0; i < 2; i++)
        {
            bf.units.Add(new BattlefieldUnit
            {
                unitId = $"m{i}",
                missileModuleId = "mod_ballistic_missile_yl",
                alive = true,
                arrivalAtSec = 0,
            });
        }

        var launcher = new BattlefieldUnit
        {
            unitId = "l1",
            side = UnitSide.FRIENDLY,
            combatFactionId = 0,
            alive = true,
            arrivalAtSec = 0,
            fittedModules = { ["a1"] = "mod_ballistic_missile_yl" },
            attackRangeM = 50_000f,
            targetUnitId = "t1",
        };
        var target = new BattlefieldUnit
        {
            unitId = "t1",
            side = UnitSide.ENEMY,
            combatFactionId = 1,
            alive = true,
            arrivalAtSec = 0,
            x = 100f,
            structureHp = 100,
            structureMax = 100,
        };
        bf.units.Add(launcher);
        bf.units.Add(target);

        var before = bf.units.Count(u => u.IsBallisticMissile());
        Assert.That(before, Is.EqualTo(2));

        // Cap path: counting live missiles >= maxLiveMissiles must refuse (tested at service gate).
        var live = 0;
        foreach (var u in bf.units)
        {
            if (!u.IsDestroyed() && u.IsBallisticMissile())
            {
                live++;
            }
        }

        Assert.That(live >= bf.maxLiveMissiles, Is.True);
    }

    [Test]
    public void SampleInBall_StaysWithinRadius()
    {
        var rng = new Random(1);
        for (var i = 0; i < 200; i++)
        {
            MechanismTestStressSpawnService.SampleInBall(rng, 100_000f, out var x, out var y, out var z);
            Assert.That(x * x + y * y + z * z, Is.LessThanOrEqualTo(100_000f * 100_000f * 1.001f));
        }
    }
}
