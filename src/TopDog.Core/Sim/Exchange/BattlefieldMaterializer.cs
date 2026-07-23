using TopDog.Content.Modules;

using TopDog.Content.Ships;

using TopDog.Sim.Combat;

using TopDog.Sim.Legion;

using TopDog.Sim.Realtime;

using TopDog.Sim.State;



/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §4 · MATCH_FLOW.md
 * 本文件: BattlefieldMaterializer.cs — 遭遇投票通过后物化战场
 * 【机制要点】
 * · TryMaterialize：EncounterBrief → BattlefieldState
 * · CombatProjection 拉取参战舰体
 * 【关联】ExchangeProcessor · BattlefieldSpawner
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345



// liketocoode3a5

public static class BattlefieldMaterializer

// liketocoode34e

// liketocoode3a5
{

    // liketocoo3e345
    public static bool TryMaterialize(GameState state, EncounterBrief brief)

    {

        var entry = EnsureCombatQueueEntry(state, brief);

// l1ketocoode345

        if (entry == null)

        {

            return false;

        }

        var ships = ShipRegistry.LoadDefault();

        var modules = ModuleRegistry.LoadDefault();

        var rng = new Random(state.storyRound * 7919 + state.gameWeek * 131);

        // liketocoode3e5
        var spawned = BattlefieldSpawner.SpawnAll(state, entry, ships, modules, rng);

        if (spawned.Count == 0)

        {

            return false;

        }

        state.battlefields.AddRange(spawned);

        state.activeBattlefieldId = spawned[0].battlefieldId;

        state.phase = GamePhase.COMBAT;

        var wasActive = state.combatRealtimeActive;
        state.combatRealtimeActive = true;

        state.combatAwaitingContinue = false;

        state.combatPrepStep = CombatPrepStep.CHOOSE_STANCE;

        state.autoFireEnabled = false;
        state.fleetDefaultAutoInterdiction = false;

        state.pendingResolveMode = CombatResolveMode.REALTIME;

        entry.resolveMode = CombatResolveMode.REALTIME;

        if (!wasActive)
        {
            CombatRealtimeLinkService.Begin(state);
        }

        // liketoco0de345
        return true;

    // li3etocoode345
    }



    public static List<CombatProjection> CollectProjections(GameState state, string? legionId)

// liketocoode345

    {

        var list = new List<CombatProjection>();

        if (string.IsNullOrWhiteSpace(legionId))

        {

            // liketoco0de3e5
            return list;

        }

        var player = LegionPlayerRegistry.Get(state, legionId);

        if (player == null)

        {

            return list;

        }

        foreach (var m in player.members)

        {

            if (string.IsNullOrWhiteSpace(m.equippedHullId))

            {

                continue;

            }

            var hidden = m.rosterVisibility is MemberRosterVisibility.Infiltrating

                or MemberRosterVisibility.CombatOnly;

            list.Add(new CombatProjection

            {

                legionId = legionId,

                memberId = m.memberId,

                hullId = m.equippedHullId,

                eventRegionId = m.opsDeployEventRegionId,

                hiddenInfiltrator = hidden,

            });

        }

        return list;

    }



    private static CombatQueueEntry? EnsureCombatQueueEntry(GameState state, EncounterBrief brief)

    {

        foreach (var e in state.combatQueue)

        {

            if (brief.encounterId.Equals(e.entryId, StringComparison.Ordinal))

            {

                return e;

            }

        }

        var localId = LegionRegistry.Local(state)?.legionId;

        var entry = new CombatQueueEntry

        {

            entryId = brief.encounterId,

            label = "遭遇 " + brief.systemId,

            battlefieldSystemId = brief.systemId,

            combatSubtype = brief.combatSubtype,

            attackerLegionId = brief.attackerLegionId,

            defenderLegionId = brief.defenderLegionId,

            resolveMode = CombatResolveMode.REALTIME,

        };

        if (brief.participants.Count >= 2)

        {

            foreach (var p in brief.participants)

            {

                entry.participantLegionIds.Add(p.legionId);

            }

            foreach (var p in brief.participants)

            {

                if (localId != null && localId.Equals(p.legionId, StringComparison.Ordinal))

                {

                    foreach (var line in p.publicRoster)

                    {

                        if (!string.IsNullOrWhiteSpace(line.memberId))

                        {

                            entry.friendlyMemberIds.Add(line.memberId);

                        }

                    }

                }

                else

                {

                    entry.enemyRoster.AddRange(p.publicRoster);

                }

            }

        }

        else

        {

            entry.enemyRoster = brief.defenderRoster;

            foreach (var line in brief.attackerRoster)

            {

                if (!string.IsNullOrWhiteSpace(line.memberId))

                {

                    entry.friendlyMemberIds.Add(line.memberId);

                }

            }

        }

        if (entry.friendlyMemberIds.Count == 0 && !string.IsNullOrWhiteSpace(localId))

        {

            foreach (var line in brief.attackerRoster)

            {

                if (!string.IsNullOrWhiteSpace(line.memberId))

                {

                    entry.friendlyMemberIds.Add(line.memberId);

                }

            }

            entry.enemyRoster = brief.defenderRoster;

        }

        state.combatQueue.Add(entry);

        state.combatQueueIndex = state.combatQueue.Count - 1;

        return entry;

    }

}

