using TopDog.Content.Balance;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Sim.Skirmish;

public static class SkirmishRespawnService
{
    public static bool IsWingOrChildUnit(BattlefieldUnit unit)
    {
        if (unit.IsTemplateCarriedUnit())
        {
            return true;
        }

        var tonnage = unit.tonnageClass ?? "";
        return tonnage is "DRONE" or "STRIKE_CRAFT" or "SHUTTLE" or "MISSILE";
    }

    public static void QueueRespawn(GameState state, BattlefieldUnit unit, ShipRegistry? ships = null)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state) || state.skirmish == null || unit.memberId == null)
        {
            return;
        }

        if (IsWingOrChildUnit(unit))
        {
            return;
        }

        if (state.boardingPermadeadMemberIds.Contains(unit.memberId))
        {
            PushAlert(state, $"{unit.displayName ?? unit.memberId} 被登录夺舍 · 本局无法重生");
            return;
        }

        var member = LegionPlayerRegistry.FindMember(state, unit.memberId);
        if (member == null)
        {
            return;
        }

        ships ??= ShipRegistry.LoadDefault();
        var balance = SkirmishBalanceConfig.LoadDefault();
        var cd = balance.respawnCooldownSec;
        var hull = unit.hullId != null ? ships.FindHull(unit.hullId) : null;
        var shipName = hull?.displayName ?? unit.hullId ?? "舰船";
        var memberName = member.name ?? unit.memberId;
        RespawnNoticeService.PushQueuedOnce(state, unit.memberId, shipName, memberName, cd);

        var baseline = MatchMemberBaselineService.TryGet(state, unit.memberId);
        state.skirmish.respawnQueue.Add(new SkirmishRespawnEntry
        {
            memberId = member.memberId ?? unit.memberId,
            legionId = member.legionId ?? unit.legionId ?? "",
            respawnAtSec = state.skirmish.elapsedSec + cd,
            hullId = baseline?.hullId ?? member.equippedHullId ?? unit.hullId ?? "",
            fittedModules = baseline != null
                ? new Dictionary<string, string?>(baseline.fittedModules)
                : new Dictionary<string, string?>(MemberFittingService.Fittings(state, member)),
        });
    }

    public static void Tick(GameState state, ShipRegistry ships, ModuleRegistry modules, Random rng)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state) || state.skirmish == null)
        {
            return;
        }

        for (var i = state.skirmish.respawnQueue.Count - 1; i >= 0; i--)
        {
            var entry = state.skirmish.respawnQueue[i];
            if (entry.respawnAtSec > state.skirmish.elapsedSec)
            {
                continue;
            }

            if (state.boardingPermadeadMemberIds.Contains(entry.memberId))
            {
                state.skirmish.respawnQueue.RemoveAt(i);
                continue;
            }

            var legion = state.legions.Find(l => entry.legionId.Equals(l.legionId, StringComparison.Ordinal));
            var bf = FindLegionFortressBattlefield(state, entry.legionId);
            if (legion == null || bf == null)
            {
                state.skirmish.respawnQueue.RemoveAt(i);
                continue;
            }

            var member = LegionPlayerRegistry.FindMember(state, entry.memberId);
            if (member == null)
            {
                state.skirmish.respawnQueue.RemoveAt(i);
                continue;
            }

            var hullId = entry.hullId;
            if (string.IsNullOrWhiteSpace(hullId))
            {
                hullId = member.equippedHullId;
            }

            var hull = hullId != null ? ships.FindHull(hullId) : null;
            if (hull == null)
            {
                state.skirmish.respawnQueue.RemoveAt(i);
                continue;
            }

            var balance = SkirmishBalanceConfig.LoadDefault();
            var side = legion.isLocal ? UnitSide.FRIENDLY : UnitSide.ENEMY;
            var u = new BattlefieldUnit
            {
                unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
                displayName = member.name ?? entry.memberId,
                hullId = hullId,
                memberId = member.memberId,
                legionId = entry.legionId,
                side = side,
                arrivalAtSec = bf.timeSec,
                facingRad = side == UnitSide.FRIENDLY ? 0f : (float)Math.PI,
                combatSeizedHullThisLife = false,
            };
            SkirmishSpawnService.ApplyFortressSpawnOffset(u, rng, balance.spawnRadiusM);
            u.fittedModules = new Dictionary<string, string>();
            foreach (var kv in entry.fittedModules)
            {
                if (kv.Value != null)
                {
                    u.fittedModules[kv.Key] = kv.Value;
                }
            }

            TraitGrantedModuleService.ApplyForMember(member, u, modules);
            ModuleRuntime.ApplyToUnit(u, hull, modules);
            bf.units.Add(u);
            PushAlert(state, $"{u.displayName} 已在军堡附近重生");
            state.flags.Remove("respawn.notice." + entry.memberId);
            state.skirmish.respawnQueue.RemoveAt(i);
        }
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }

    private static BattlefieldState? FindLegionFortressBattlefield(GameState state, string legionId)
    {
        string? regionId = null;
        foreach (var building in state.buildings)
        {
            if (building.legionId != null
                && building.legionId.Equals(legionId, StringComparison.Ordinal)
                && string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
            {
                regionId = building.eventRegionId;
                break;
            }
        }

        if (regionId == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (regionId.Equals(bf.eventRegionId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }
}
