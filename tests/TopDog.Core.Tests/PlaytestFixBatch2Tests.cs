using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class PlaytestFixBatch2Tests
{
    [Test]
    public void BuildingAssault_StartPositions_Are100kmTeamClusters()
    {
        var bf = new BattlefieldState { targetBuildingId = "bld-1" };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "bld-1",
            buildingId = "bld-1",
            isBuilding = true,
            side = UnitSide.FRIENDLY,
            structureHp = 100f,
            structureMax = 100f,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "atk-1",
            side = UnitSide.ENEMY,
            legionId = "legion-atk",
            structureHp = 100f,
            structureMax = 100f,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "atk-2",
            side = UnitSide.ENEMY,
            legionId = "legion-atk",
            structureHp = 100f,
            structureMax = 100f,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "def-1",
            side = UnitSide.FRIENDLY,
            legionId = "legion-def",
            structureHp = 100f,
            structureMax = 100f,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "def-2",
            side = UnitSide.FRIENDLY,
            legionId = "legion-def",
            structureHp = 100f,
            structureMax = 100f,
        });

        BuildingCombatRules.LayoutAssaultStartPositions(bf, new Random(3));

        var friendly = bf.units.Where(u => !u.isBuilding && u.side == UnitSide.FRIENDLY).ToList();
        var enemy = bf.units.Where(u => !u.isBuilding && u.side == UnitSide.ENEMY).ToList();
        Assert.That(friendly, Has.Count.EqualTo(2));
        Assert.That(enemy, Has.Count.EqualTo(2));

        foreach (var u in friendly.Concat(enemy))
        {
            var dist = MathF.Sqrt(u.x * u.x + u.y * u.y);
            Assert.That(dist, Is.GreaterThan(BuildingCombatRules.AssaultStartDistanceM - 2500f));
            Assert.That(dist, Is.LessThan(BuildingCombatRules.AssaultStartDistanceM + 2500f));
        }

        Assert.That(MaxPairDistance(friendly), Is.LessThan(BuildingCombatRules.AssaultClusterSpreadM));
        Assert.That(MaxPairDistance(enemy), Is.LessThan(BuildingCombatRules.AssaultClusterSpreadM));

        var friendlyCentroid = Centroid(friendly);
        var enemyCentroid = Centroid(enemy);
        var betweenTeams = MathF.Sqrt(
            MathF.Pow(friendlyCentroid.x - enemyCentroid.x, 2)
            + MathF.Pow(friendlyCentroid.y - enemyCentroid.y, 2));
        Assert.That(betweenTeams, Is.GreaterThan(150_000f), "defenders and assault should face each other on opposite arcs");
    }

    [Test]
    public void BuildingAssault_MultiLegionAssault_SharesAssaultArc()
    {
        var bf = new BattlefieldState { targetBuildingId = "bld-1" };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "bld-1",
            buildingId = "bld-1",
            isBuilding = true,
            side = UnitSide.FRIENDLY,
            structureHp = 100f,
            structureMax = 100f,
        });
        foreach (var legion in new[] { "legion-a", "legion-b", "legion-c" })
        {
            bf.units.Add(new BattlefieldUnit
            {
                unitId = "atk-" + legion,
                side = UnitSide.ENEMY,
                legionId = legion,
                structureHp = 100f,
                structureMax = 100f,
            });
        }
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "def-1",
            side = UnitSide.FRIENDLY,
            legionId = "legion-def",
            structureHp = 100f,
            structureMax = 100f,
        });

        BuildingCombatRules.LayoutAssaultStartPositions(bf, new Random(7));

        var assault = bf.units.Where(u => !u.isBuilding && u.side == UnitSide.ENEMY).ToList();
        foreach (var u in assault)
        {
            Assert.That(u.x, Is.LessThan(-50_000f), "assault legions should share the assault half-plane (x<0)");
        }
    }

    [Test]
    public void CollectiveStop_DoesNotKillMissileThrottle()
    {
        var state = new GameState { combatRealtimeActive = true };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 1f };
        var target = new BattlefieldUnit
        {
            unitId = "tgt",
            side = UnitSide.ENEMY,
            x = 5000f,
            structureHp = 100f,
            structureMax = 100f,
            arrivalAtSec = 0f,
        };
        var missile = new BattlefieldUnit
        {
            unitId = "msl",
            side = UnitSide.FRIENDLY,
            missileModuleId = "mod_chaos_missile_l",
            missileProfileSnapshot = new MissileProjectileProfile
            {
                ModuleId = "mod_chaos_missile_l",
                FlightSpeedMps = 1000f,
                FlightMaxSec = 30f,
                ContactHoldSec = 1f,
                AoeBaseDamage = 1000f,
                AoeZeroRadiusM = 7000f,
            },
            targetUnitId = "tgt",
            structureHp = 100f,
            structureMax = 100f,
            maxSpeedMps = 1000f,
            accelMps2 = 700f,
            arrivalAtSec = 0f,
            aiOrder = UnitAiOrder.STOP,
        };
        bf.units.Add(target);
        bf.units.Add(missile);
        state.battlefields.Add(bf);

        FleetOrderService.OrderStop(state, bf, allFriendly: true, null);
        Assert.That(missile.aiOrder, Is.EqualTo(UnitAiOrder.STOP));

        BattlefieldSystem.Tick(state, 0.05f);
        Assert.That(missile.throttleOn, Is.True, "missile flight must not be stopped by fleet STOP");
    }

    private static float MaxPairDistance(IReadOnlyList<BattlefieldUnit> units)
    {
        var max = 0f;
        for (var i = 0; i < units.Count; i++)
        {
            for (var j = i + 1; j < units.Count; j++)
            {
                var dx = units[i].x - units[j].x;
                var dy = units[i].y - units[j].y;
                max = MathF.Max(max, MathF.Sqrt(dx * dx + dy * dy));
            }
        }
        return max;
    }

    private static (float x, float y) Centroid(IReadOnlyList<BattlefieldUnit> units)
    {
        var x = 0f;
        var y = 0f;
        foreach (var u in units)
        {
            x += u.x;
            y += u.y;
        }
        return (x / units.Count, y / units.Count);
    }
}
