using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Traits;

/// <summary>董事会召来：战场注入 5 艘随机无畏 + 临时团员。</summary>
public static class BoardSummonService
{
    public const int ReinforcementCount = 5;
    public const string TempMemberIdPrefix = "board-summon-";

    public static void InjectIfPending(
        GameState state,
        CombatQueueEntry entry,
        BattlefieldState bf,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (string.IsNullOrWhiteSpace(state.pendingBoardSummonLegionId)
            || string.IsNullOrWhiteSpace(state.pendingBoardSummonIdentityCode))
        {
            return;
        }
        if (!state.pendingBoardSummonLegionId.Equals(entry.attackerLegionId, StringComparison.Ordinal)
            && !state.pendingBoardSummonLegionId.Equals(entry.defenderLegionId, StringComparison.Ordinal))
        {
            return;
        }

        var dreadnoughts = ships.AllHulls()
            .Where(h => "DREADNOUGHT".Equals(h.tonnageClass, StringComparison.OrdinalIgnoreCase))
            .Select(h => h.hullId)
            .Where(id => id != null)
            .Cast<string>()
            .ToList();
        if (dreadnoughts.Count == 0)
        {
            ClearPending(state);
            return;
        }

        for (var i = 0; i < ReinforcementCount; i++)
        {
            var hullId = dreadnoughts[rng.Next(dreadnoughts.Count)];
            var memberId = TempMemberIdPrefix + state.storyRound + "-" + i + "-" + rng.Next(1000, 9999);
            var temp = new MemberState
            {
                memberId = memberId,
                identityCode = state.pendingBoardSummonIdentityCode + "-sum" + i,
                name = "董事会增援" + (i + 1),
                legionId = state.pendingBoardSummonLegionId,
                equippedHullId = hullId,
                isCombatSummonTemp = true,
                isPlayer = true,
            };
            state.members.Add(temp);
            MemberDispatchAutoFitService.TryFillEmptySlots(state, temp, ships, modules);
            AddSummonedUnit(bf, state, temp, ships, modules, rng);
        }

        PushAlert(state, "董事会召来：战场增援 " + ReinforcementCount + " 艘无畏");
        ClearPending(state);
    }

    public static void PurgeTempMembers(GameState state)
    {
        state.members.RemoveAll(m => m.isCombatSummonTemp);
    }

    private static void AddSummonedUnit(
        BattlefieldState bf,
        GameState state,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var hull = ships.FindHull(m.equippedHullId!);
        if (hull == null)
        {
            return;
        }
        var u = new BattlefieldUnit
        {
            unitId = "u-" + Guid.NewGuid().ToString("N")[..8],
            memberId = m.memberId,
            displayName = m.name,
            hullId = m.equippedHullId,
            tonnageClass = hull.tonnageClass,
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            pinnedToBattlefield = true,
            fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, m)),
        };
        ModuleRuntime.ApplyToUnit(u, hull, modules);
        var spread = 800f + (float)rng.NextDouble() * 400f;
        var ang = (float)(rng.NextDouble() * Math.PI * 2);
        u.x = (float)Math.Cos(ang) * spread;
        u.z = (float)Math.Sin(ang) * spread;
        bf.units.Add(u);
    }

    private static void ClearPending(GameState state)
    {
        state.pendingBoardSummonIdentityCode = null;
        state.pendingBoardSummonLegionId = null;
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
