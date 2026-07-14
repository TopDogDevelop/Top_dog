using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

/// <summary>
/// 未驾驶且无仓舰 → 不进参战名册；有仓舰 → 随机上舰并随机装模块。
/// </summary>
[TestFixture]
public sealed class CombatRosterWarehouseShipTests
{
    [Test]
    public void Unpiloted_NoWarehouseShips_ExcludedFromRoster()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP };
        var localId = "legion-local";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        var m = new MemberState
        {
            memberId = "bare",
            legionId = localId,
            name = "光膀子",
        };
        state.members.Add(m);

        var entry = new CombatQueueEntry
        {
            friendlyMemberIds = { "bare" },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var rng = new Random(1);

        CombatRosterPrepService.PrepMemberForCombat(state, m, ships, modules, rng);
        CombatRosterPrepService.PruneMembersWithoutHull(state, entry);
        CombatRosterRefresh.RefreshFriendly(state, entry, ships, modules);

        Assert.That(m.equippedHullId, Is.Null.Or.Empty);
        Assert.That(entry.friendlyMemberIds, Is.Empty);
        Assert.That(entry.friendlyRosterLines, Is.Empty);
    }

    [Test]
    public void Unpiloted_PersonalWarehouse_EquipsRandomHullAndFits()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP };
        var localId = "legion-local";
        state.legions.Add(new LegionState
        {
            legionId = localId,
            isLocal = true,
            legionStock = { ["mod_hybrid_gun_m"] = 20 },
        });
        var m = new MemberState
        {
            memberId = "stocked",
            legionId = localId,
            name = "有仓",
        };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("hull_bc_spear", 1);

        var entry = new CombatQueueEntry
        {
            friendlyMemberIds = { "stocked" },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        CombatRosterRefresh.RefreshFriendly(state, entry, ships, modules);

        Assert.That(m.equippedHullId, Is.EqualTo("hull_bc_spear"));
        Assert.That(entry.friendlyMemberIds, Is.EquivalentTo(new[] { "stocked" }));
        Assert.That(entry.friendlyRosterLines.Count, Is.EqualTo(1));
        Assert.That(entry.friendlyRosterLines[0].canParticipate, Is.True);
        Assert.That(MemberFittingService.Fittings(state, m).Count, Is.GreaterThan(0));
    }

    [Test]
    public void Unpiloted_LegionWarehouseOnly_EquipsFromLegionStock()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP };
        var localId = "legion-local";
        state.legions.Add(new LegionState
        {
            legionId = localId,
            isLocal = true,
            legionStock =
            {
                ["hull_bc_spear"] = 2,
                ["mod_hybrid_gun_m"] = 10,
            },
        });
        var m = new MemberState
        {
            memberId = "legion_stock",
            legionId = localId,
            isPlayer = true,
            isAi = false,
        };
        LegionPlayerRegistry.AddMemberToLegion(state, localId, m);

        var entry = new CombatQueueEntry
        {
            friendlyMemberIds = { "legion_stock" },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        CombatRosterRefresh.RefreshFriendly(state, entry, ships, modules);

        Assert.That(m.equippedHullId, Is.EqualTo("hull_bc_spear"));
        Assert.That(state.legions[0].legionStock.GetValueOrDefault("hull_bc_spear"), Is.EqualTo(1));
        Assert.That(entry.friendlyRosterLines.Count, Is.EqualTo(1));
    }

    [Test]
    public void AlreadyPiloting_KeepsHull_NotClearedByWarehouseRule()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP };
        var localId = "legion-local";
        state.legions.Add(new LegionState
        {
            legionId = localId,
            isLocal = true,
            legionStock = { ["hull_bc_spear"] = 5, ["mod_hybrid_gun_m"] = 10 },
        });
        var m = new MemberState
        {
            memberId = "pilot",
            legionId = localId,
            equippedHullId = "hull_bc_spear",
        };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("hull_frigate_scout", 3);

        var entry = new CombatQueueEntry
        {
            friendlyMemberIds = { "pilot" },
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        CombatRosterRefresh.RefreshFriendly(state, entry, ships, modules);

        Assert.That(m.equippedHullId, Is.EqualTo("hull_bc_spear"));
        Assert.That(entry.friendlyRosterLines.Count, Is.EqualTo(1));
    }
}
