using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

public sealed class AiRealtimePlayerBrainTests
{
    private static readonly ModuleRegistry Modules = ModuleRegistry.LoadDefault();
    private static readonly ShipRegistry Ships = ShipRegistry.LoadDefault();

    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void AiEnemyCarrier_Focus_DeploysStrikeCraftViaOrderFocus()
    {
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        const string modId = "mod_strike_wing_a_l";
        Assert.That(Modules.Resolve(modId), Is.Not.Null);

        var carrier = Enemy("ai-carrier", "CARRIER", 0f, 0f);
        carrier.fittedModules = new Dictionary<string, string> { { "tube_1", modId } };
        LaunchTubeStateService.InitTubeStates(carrier, Modules);
        var target = Friendly("player-ship", 2_000f, 0f);
        bf.units.Add(carrier);
        bf.units.Add(target);

        AiRealtimePlayerBrain.Tick(state, bf, Modules, Ships, 1f);

        Assert.That(carrier.targetUnitId, Is.EqualTo("player-ship"));
        Assert.That(carrier.explicitFocus, Is.True);
        Assert.That(
            bf.units.Exists(u =>
                "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
                && "ai-carrier".Equals(u.parentUnitId, StringComparison.Ordinal)),
            Is.True,
            "人机集火须走 OrderFocus → 放出舰载机");
        Assert.That(carrier.tubeStates["tube_1"], Is.EqualTo(LaunchTubeState.Activated));

        // 交战带运动可覆盖 FOCUS，但仍保持显式集火 → 不收回
        StrikeWingRecallService.Tick(bf, Modules, new Random(1));
        Assert.That(
            bf.units.Exists(u =>
                "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
                && "ai-carrier".Equals(u.parentUnitId, StringComparison.Ordinal)),
            Is.True,
            "显式集火中不得因 aiOrder≠FOCUS 收回舰载机");
    }

    [Test]
    public void RetargetsFleetFocusEveryThirtySeconds()
    {
        var state = new GameState();
        var bf = new BattlefieldState
        {
            battlefieldId = "bf1",
            timeSec = 0f,
        };
        var possessor = Enemy("ai-big", "BATTLESHIP", 0f, 0f);
        var wingman = Enemy("ai-small", "FRIGATE", 100f, 0f);
        var far = Friendly("far", 10_000f, 0f);
        var near = Friendly("near", 100f, 0f);
        bf.units.Add(possessor);
        bf.units.Add(wingman);
        bf.units.Add(far);
        bf.units.Add(near);

        AiRealtimePlayerBrain.Tick(state, bf, Modules, Ships, 1f);
        Assert.That(wingman.targetUnitId, Is.EqualTo("near"));

        far.x = 10f;
        near.x = 10_000f;
        AiRealtimePlayerBrain.Tick(state, bf, Modules, Ships, 29f);
        Assert.That(wingman.targetUnitId, Is.EqualTo("near"), "Fleet focus should not refresh before 30s");

        AiRealtimePlayerBrain.Tick(state, bf, Modules, Ships, 2f);
        Assert.That(wingman.targetUnitId, Is.EqualTo("far"));
    }

    [Test]
    public void OrbitsNearestFieldGuardWhenActive()
    {
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        var guard = Enemy("guard", "CRUISER", 0f, 0f);
        guard.fittedModules["fn_1"] = "mod_armor_link_s";
        guard.fieldAuraEnabledAtSec = 1f;
        guard.hullId = "hull_cruiser_greywolf_guard";
        var escort = Enemy("escort", "FRIGATE", 5000f, 0f);
        escort.attackRangeM = 20_000f;
        var enemy = Friendly("enemy", 15_000f, 0f);
        bf.units.Add(guard);
        bf.units.Add(escort);
        bf.units.Add(enemy);

        AiRealtimePlayerBrain.Tick(state, bf, Modules, Ships, 1f);

        Assert.That(escort.aiOrder, Is.EqualTo(UnitAiOrder.ORBIT));
        Assert.That(escort.orbitTargetUnitId, Is.EqualTo("guard"));
        Assert.That(escort.targetUnitId, Is.EqualTo("enemy"));
    }

    private static BattlefieldUnit Enemy(string id, string tonnage, float x, float y) => new()
    {
        unitId = id,
        side = UnitSide.ENEMY,
        tonnageClass = tonnage,
        alive = true,
        arrivalAtSec = 0f,
        x = x,
        y = y,
    };

    private static BattlefieldUnit Friendly(string id, float x, float y) => new()
    {
        unitId = id,
        side = UnitSide.FRIENDLY,
        alive = true,
        arrivalAtSec = 0f,
        x = x,
        y = y,
    };
}
