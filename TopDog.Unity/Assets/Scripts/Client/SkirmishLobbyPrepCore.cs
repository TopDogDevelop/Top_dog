using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Starting;
using TopDog.Content.Traits;
using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Client;

/// <summary>约战准备大厅：将名册槽位与运营同款配船/图鉴 UI 桥接到 scratch SimulationCore。</summary>
public sealed class SkirmishLobbyPrepCore
{
    private const int SkirmishStockQty = 9999;

    private readonly SimulationCore _core;
    private readonly string _localPlayerId;

    public SimulationCore Core => _core;
    public string LocalLegionId => _localPlayerId;

    private SkirmishLobbyPrepCore(SimulationCore core, string localPlayerId)
    {
        _core = core;
        _localPlayerId = localPlayerId;
    }

    public static SkirmishLobbyPrepCore? TryCreate(
        SkirmishLobbyState lobby,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var local = lobby.FindLocal();
        if (local == null)
        {
            return null;
        }

        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
        };
        state.worldline.type = WorldlineType.LEGION_SKIRMISH;
        state.flags["skirmish.lobbyPrep"] = "1";
        state.flags["lobby.localPlayerId"] = local.playerId;

        state.legions.Add(new LegionState
        {
            legionId = local.playerId,
            displayName = local.displayName,
            playerId = local.playerId,
            isLocal = true,
            isAiControlled = false,
        });

        SeedSkirmishStock(state, ships, modules);
        ApplyRosterToState(state, lobby, local.playerId);

        IdentityMigrationService.EnsureFromMembers(state);
        LegionPlayerRegistry.EnsureFromLegions(state);
        LegionPlayerRegistry.EnsureRosterForLegion(state, local.playerId);

        var traits = TraitCatalog.LoadDefault();
        return new SkirmishLobbyPrepCore(
            new SimulationCore(state, new BrickGraph(), ships, traits, modules),
            local.playerId);
    }

    public void SyncFromLobby(SkirmishLobbyState lobby)
    {
        var state = _core.State;
        state.members.Clear();
        state.memberFittedModules.Clear();
        ApplyRosterToState(state, lobby, _localPlayerId);
        IdentityMigrationService.EnsureFromMembers(state);
        LegionPlayerRegistry.EnsureRosterForLegion(state, _localPlayerId);
    }

    public void PullIntoLobby(SkirmishLobbyState lobby)
    {
        if (!lobby.rosterByPlayerId.TryGetValue(_localPlayerId, out var roster))
        {
            return;
        }

        var state = _core.State;
        foreach (var m in state.members)
        {
            if (string.IsNullOrWhiteSpace(m.memberId))
            {
                continue;
            }

            var slot = roster.Find(s => s.memberId == m.memberId);
            if (slot == null)
            {
                continue;
            }

            slot.displayName = m.name ?? slot.displayName;
            if (!string.IsNullOrWhiteSpace(m.equippedHullId))
            {
                slot.hullId = m.equippedHullId;
            }

            var fit = MemberFittingService.Fittings(state, m);
            slot.fittedModules = fit.ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
        }
    }

    public MemberState? FindMember(string? memberId) =>
        MemberSelectionKeys.FindMember(_core.State, memberId);

    private static void SeedSkirmishStock(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        var stock = LegionRegistry.MutableLocalStock(state);
        foreach (var hullId in SkirmishLobbyCatalog.AllHullIds(ships))
        {
            stock[hullId] = SkirmishStockQty;
        }

        foreach (var modId in SkirmishLobbyCatalog.AllModuleIds(modules))
        {
            stock[modId] = SkirmishStockQty;
        }
    }

    private static void ApplyRosterToState(GameState state, SkirmishLobbyState lobby, string localPlayerId)
    {
        if (!lobby.rosterByPlayerId.TryGetValue(localPlayerId, out var roster))
        {
            return;
        }

        foreach (var slot in roster)
        {
            var member = SlotToMember(slot, localPlayerId);
            state.members.Add(member);
            state.memberFittedModules[slot.memberId] = slot.fittedModules
                .ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
        }
    }

    private static MemberState SlotToMember(SkirmishRosterSlot slot, string legionId)
    {
        var member = new MemberState
        {
            memberId = slot.memberId,
            name = slot.displayName,
            legionId = legionId,
            equippedHullId = slot.hullId,
            appraised = true,
            source = "preset",
        };

        if (!SkirmishTemplateRows.TryParseRowKey(slot.memberTemplateRowId, out var templateId, out var identity, out var suffix))
        {
            return member;
        }

        member.identityCode = identity;
        member.accountSuffix = suffix;
        var templateMember = StartingTemplateLoader.LoadMembers(templateId)
            .FirstOrDefault(m => identity.Equals(IdentityCodes.Of(m), StringComparison.Ordinal)
                && suffix.Equals(m.accountSuffix, StringComparison.Ordinal));
        if (templateMember == null)
        {
            return member;
        }

        member.accountName = templateMember.accountName;
        member.rarity = templateMember.rarity;
        member.trueRarity = templateMember.trueRarity;
        member.bio = templateMember.bio;
        member.labels = new List<string>(templateMember.labels);
        member.traitIds = new List<string>(templateMember.traitIds);
        member.cardBackdrop = templateMember.cardBackdrop;
        member.legionBelonging = templateMember.legionBelonging;
        member.energy = templateMember.energy;
        member.wisdom = templateMember.wisdom;
        member.accountBuildScore = templateMember.accountBuildScore;
        if (string.IsNullOrWhiteSpace(member.equippedHullId) && !string.IsNullOrWhiteSpace(templateMember.equippedHullId))
        {
            member.equippedHullId = templateMember.equippedHullId;
        }

        return member;
    }
}
