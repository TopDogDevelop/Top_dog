using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;

namespace TopDog.Lobby;

/// <summary>约战人机 §8.1：随机舰种 + 每槽最大合法模块。</summary>
public static class SkirmishAiRosterGenerator
{
    public static void FillAiRoster(SkirmishLobbyState lobby, ShipRegistry ships, ModuleRegistry modules, Random rng)
    {
        var ai = lobby.players.Find(p => p.kind == LobbyPlayerKind.AI);
        if (ai == null)
        {
            return;
        }

        var cap = lobby.scale;
        if (!lobby.rosterByPlayerId.TryGetValue(ai.playerId, out var roster))
        {
            roster = new List<SkirmishRosterSlot>();
            lobby.rosterByPlayerId[ai.playerId] = roster;
        }

        while (roster.Count < cap)
        {
            var idx = roster.Count + 1;
            roster.Add(new SkirmishRosterSlot
            {
                memberId = $"sk_ai_{idx}",
                displayName = $"AI {idx}",
            });
        }

        var hullIds = SkirmishLobbyCatalog.AllHullIds(ships).ToList();
        if (hullIds.Count == 0)
        {
            return;
        }

        foreach (var slot in roster)
        {
            var hullId = hullIds[rng.Next(hullIds.Count)];
            slot.hullId = hullId;
            slot.fittedModules.Clear();
            var hull = ships.FindHull(hullId);
            if (hull == null)
            {
                continue;
            }

            FitRandomMaxModules(slot, hull, modules, rng);
        }
    }

    private static void FitRandomMaxModules(
        SkirmishRosterSlot slot,
        HullDef hull,
        ModuleRegistry modules,
        Random rng)
    {
        var modIds = SkirmishLobbyCatalog.AllModuleIds(modules);
        var bySlot = new Dictionary<string, List<ModuleDef>>(StringComparer.Ordinal);
        foreach (var modId in modIds)
        {
            var mod = modules.Resolve(modId);
            if (mod?.slotCategory == null)
            {
                continue;
            }

            foreach (var slotKey in MemberFittingService.ListOpenSlots(hull))
            {
                if (!FittingValidator.ModuleFitsSlot(slotKey, mod, hull))
                {
                    continue;
                }

                if (!bySlot.TryGetValue(slotKey, out var list))
                {
                    list = new List<ModuleDef>();
                    bySlot[slotKey] = list;
                }

                list.Add(mod);
            }
        }

        foreach (var kv in bySlot)
        {
            var candidates = kv.Value;
            if (candidates.Count == 0)
            {
                continue;
            }

            candidates.Sort((a, b) => ModuleSizeRank(b.moduleSize).CompareTo(ModuleSizeRank(a.moduleSize)));
            var topRank = ModuleSizeRank(candidates[0].moduleSize);
            var top = candidates.Where(m => ModuleSizeRank(m.moduleSize) == topRank).ToList();
            slot.fittedModules[kv.Key] = top[rng.Next(top.Count)].moduleId;
        }
    }

    private static int ModuleSizeRank(string? size) => size switch
    {
        "EXTRA_LARGE" or "XL" => 5,
        "LARGE" or "L" => 4,
        "MEDIUM" or "M" => 3,
        "SMALL" or "S" => 2,
        _ => 1,
    };
}
