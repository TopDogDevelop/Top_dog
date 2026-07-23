using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FocusFireSequencerTests
{
    [OneTimeSetUp]
    public void WarmRegistries()
    {
        // BattlefieldSystem.Tick 会 LoadDefault；预热避免本 tick 撞 6ms SceneSimBudget 硬墙跳过单位
        _ = ModuleRegistry.LoadDefault();
        _ = ShipRegistry.LoadDefault();
    }

    [Test]
    public void TryConfirmSalvoTarget_DeadTarget_ReturnsFalse()
    {
        var firer = new BattlefieldUnit { unitId = "a", side = UnitSide.FRIENDLY, alive = true };
        var dead = new BattlefieldUnit
        {
            unitId = "t",
            side = UnitSide.ENEMY,
            alive = false,
            structureHp = 0f,
            structureMax = 100f,
        };
        Assert.That(BattlefieldSystem.TryConfirmSalvoTarget(firer, dead), Is.False);
        Assert.That(BattlefieldSystem.TryConfirmSalvoTarget(firer, null), Is.False);
    }

    [Test]
    public void TryClaimVolleySlot_SameSimSec_OnlyOneSuccess()
    {
        var bf = new BattlefieldState { timeSec = 1f };
        var a0 = MakeFirer("atk-0", "tgt");
        var a1 = MakeFirer("atk-1", "tgt");
        FocusFireSequencer.Register(bf, "tgt", new[] { a0.unitId, a1.unitId });

        Assert.That(FocusFireSequencer.TryClaimVolleySlot(bf, a0), Is.True);
        Assert.That(FocusFireSequencer.TryClaimVolleySlot(bf, a1), Is.False);

        bf.timeSec = 1.02f;
        Assert.That(FocusFireSequencer.TryClaimVolleySlot(bf, a1), Is.True);
    }

    [Test]
    public void ExplicitFocus_SameTick_AtMostOneSuccessfulSalvo()
    {
        var (state, bf, target, firers) = BuildFocusBattlefield(firerCount: 3, targetHp: 50f);

        FocusFireSequencer.Register(bf, target.unitId!, firers.Select(f => f.unitId));
        WarmTick(state);

        Assert.That(target.IsDestroyed(), Is.True);
        Assert.That(bf.pendingHpDeltas.Count(d => !d.isHeal), Is.EqualTo(1));
        Assert.That(firers.Count(EnteredFireCycle), Is.EqualTo(1));
    }

    [Test]
    public void NextTick_AdvancesToNextFirerInQueue()
    {
        var (state, bf, target, firers) = BuildFocusBattlefield(firerCount: 2, targetHp: 500f);
        var a0 = firers[0];
        var a1 = firers[1];
        FocusFireSequencer.Register(bf, target.unitId!, new[] { a0.unitId, a1.unitId });

        WarmTick(state);
        Assert.That(EnteredFireCycle(a0), Is.True);
        Assert.That(EnteredFireCycle(a1), Is.False);
        Assert.That(target.structureHp, Is.EqualTo(300f).Within(0.01f));

        a0.fireCooldownSec = 10f;
        WarmTick(state);
        Assert.That(EnteredFireCycle(a1), Is.True);
        Assert.That(target.structureHp, Is.EqualTo(100f).Within(0.01f));
    }

    [Test]
    public void ConfirmFail_DoesNotEnterCooldown()
    {
        var state = new GameState { combatRealtimeActive = true, autoFireEnabled = false };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-ff3",
            timeSec = 0f,
            disableAutoVictory = true,
            tickBudgetMs = 1000f,
        };
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var dead = new BattlefieldUnit
        {
            unitId = "tgt",
            side = UnitSide.ENEMY,
            alive = false,
            arrivalAtSec = 0f,
            x = 200f,
            structureHp = 0f,
            structureMax = 50f,
        };
        var atk = MakeFirer("atk", dead.unitId!);
        atk.fireCooldownSec = 0f;
        bf.units.Add(dead);
        bf.units.Add(atk);

        WarmTick(state);
        Assert.That(EnteredFireCycle(atk), Is.False);
    }

    [Test]
    public void DeterrenceOnlyFirer_CanClaimFocusVolleySlot()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { timeSec = 1f };
        var repair = new BattlefieldUnit
        {
            unitId = "atk-repair",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            targetUnitId = "tgt",
            explicitFocus = true,
            salvoRoundDmg = 0f,
        };
        var deterrence = new BattlefieldUnit
        {
            unitId = "atk-det",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            targetUnitId = "tgt",
            explicitFocus = true,
            salvoRoundDmg = 0f,
            fittedModules = { ["fn_0"] = "mod_deterrence_gun_yl" },
        };
        bf.units.Add(repair);
        bf.units.Add(deterrence);

        Assert.That(SalvoProfileService.CanClaimFocusVolley(repair, modules), Is.False);
        Assert.That(SalvoProfileService.CanClaimFocusVolley(deterrence, modules), Is.True);

        FocusFireSequencer.Register(bf, "tgt", new[] { repair.unitId, deterrence.unitId });
        Assert.That(FocusFireSequencer.TryClaimVolleySlot(bf, repair), Is.False);
        Assert.That(FocusFireSequencer.TryClaimVolleySlot(bf, deterrence), Is.True);
    }

    private static void WarmTick(GameState state) =>
        BattlefieldSystem.Tick(state, ModuleRegistry.LoadDefault(), ShipRegistry.LoadDefault(), 0.02f);

    private static bool EnteredFireCycle(BattlefieldUnit u) => u.fireCooldownSec > 1f;

    private static (GameState state, BattlefieldState bf, BattlefieldUnit target, List<BattlefieldUnit> firers)
        BuildFocusBattlefield(int firerCount, float targetHp)
    {
        var state = new GameState { combatRealtimeActive = true, autoFireEnabled = false };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-ff-" + firerCount + "-" + targetHp,
            timeSec = 0f,
            disableAutoVictory = true,
            tickBudgetMs = 1000f,
        };
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var target = new BattlefieldUnit
        {
            unitId = "tgt",
            side = UnitSide.ENEMY,
            alive = true,
            arrivalAtSec = 0f,
            x = 200f,
            structureHp = targetHp,
            structureMax = targetHp,
            shieldHp = 0f,
            armorHp = 0f,
        };
        bf.units.Add(target);

        var firers = new List<BattlefieldUnit>();
        for (var i = 0; i < firerCount; i++)
        {
            var a = MakeFirer("atk-" + i, target.unitId!);
            a.y = i * 10f;
            firers.Add(a);
            bf.units.Add(a);
        }

        return (state, bf, target, firers);
    }

    private static BattlefieldUnit MakeFirer(string id, string targetId) => new()
    {
        unitId = id,
        side = UnitSide.FRIENDLY,
        alive = true,
        arrivalAtSec = 0f,
        x = 0f,
        targetUnitId = targetId,
        explicitFocus = true,
        aiOrder = UnitAiOrder.FOCUS,
        salvoRoundDmg = 200f,
        fireCycleSec = 10f,
        fireCooldownSec = 0f,
        attackRangeM = 10_000f,
        weaponTrackingDegPerSec = 0f,
        structureHp = 100f,
        structureMax = 100f,
    };
}
