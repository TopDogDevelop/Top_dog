using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Traits;

/// <summary>董事会召来：星系内 3AU 随机点跃迁进场（VIP_TRAIT_DESIGN.md）。</summary>
public static class BoardSummonApproachService
{
    private const float MaxSpawnAu = 3f;
    private const float MinWarpEtaSec = 15f;

    public static string SummonWithWarpApproach(
        GameState state,
        BattlefieldState bf,
        IdentityState identity,
        MemberState caster,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (!string.IsNullOrWhiteSpace(state.pendingBoardSummonLegionId))
        {
            return "董事会增援已在途中或已预约";
        }

        var legionId = caster.legionId ?? LegionTraitQuery.LocalLegionId(state);
        if (legionId == null || bf.systemId == null)
        {
            return "无法确定战场星系";
        }

        var dreadnoughts = ships.AllHulls()
            .Where(h => "DREADNOUGHT".Equals(h.tonnageClass, StringComparison.OrdinalIgnoreCase))
            .Select(h => h.hullId)
            .Where(id => id != null)
            .Cast<string>()
            .ToList();
        if (dreadnoughts.Count == 0)
        {
            return "无可用无畏舰型";
        }

        var hullForEta = ships.FindHull(dreadnoughts[0]);
        var warpAuPerSec = TacticalWarpService.ResolveWarpSpeedAups(hullForEta);
        var eta = Math.Max(MinWarpEtaSec, MaxSpawnAu / Math.Max(0.1f, warpAuPerSec));
        var planet = PickRandomPlanet(state, bf.systemId, rng);
        var spawnAu = planet?.anchorAu;

        for (var i = 0; i < BoardSummonService.ReinforcementCount; i++)
        {
            var hullId = dreadnoughts[rng.Next(dreadnoughts.Count)];
            var memberId = BoardSummonService.TempMemberIdPrefix + state.storyRound + "-live-" + i + "-" + rng.Next(1000, 9999);
            var temp = new MemberState
            {
                memberId = memberId,
                identityCode = identity.identityCode + "-sum" + i,
                name = "董事会增援" + (i + 1),
                legionId = legionId,
                equippedHullId = hullId,
                isCombatSummonTemp = true,
                isPlayer = true,
            };
            state.members.Add(temp);
            MemberDispatchAutoFitService.TryFillEmptySlots(state, temp, ships, modules);
            AddInboundUnit(bf, state, temp, ships, modules, rng, eta + i * 1.5f, spawnAu);
        }

        var planetLabel = planet?.name ?? planet?.eventRegionId ?? "深空";
        PushAlert(state, $"董事会召来：5 艘无畏自 {planetLabel} 附近跃迁，约 {eta:0} 秒后到达");
        return $"已召唤董事会增援（约 {eta:0} 秒后到达战场）";
    }

    private static void AddInboundUnit(
        BattlefieldState bf,
        GameState state,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng,
        float etaSec,
        float[]? planetAnchorAu)
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
            displayName = (m.name ?? "董事会增援") + "（跃迁中）",
            hullId = m.equippedHullId,
            tonnageClass = hull.tonnageClass,
            side = UnitSide.FRIENDLY,
            arrivalAtSec = bf.timeSec + etaSec,
            pinnedToBattlefield = true,
            fittedModules = new Dictionary<string, string>(MemberFittingService.Fittings(state, m)),
        };
        ModuleRuntime.ApplyToUnit(u, hull, modules);

        var ang = (float)(rng.NextDouble() * Math.PI * 2);
        var distM = 40_000f + (float)rng.NextDouble() * 20_000f;
        u.x = (float)Math.Cos(ang) * distM;
        u.y = (float)Math.Sin(ang) * distM;
        if (planetAnchorAu is { Length: >= 3 })
        {
            u.z = planetAnchorAu[2] * 1000f;
        }
        else
        {
            u.z = (float)(rng.NextDouble() * 2000f - 1000f);
        }

        bf.units.Add(u);
    }

    private static EventRegionDef? PickRandomPlanet(GameState state, string systemId, Random rng)
    {
        var sys = state.map?.Project?.FindSystem(systemId);
        if (sys?.eventRegions == null || sys.eventRegions.Count == 0)
        {
            return null;
        }

        var planets = sys.eventRegions
            .Where(er => EventRegionKinds.Planet.Equals(er.kind, StringComparison.Ordinal))
            .ToList();
        if (planets.Count == 0)
        {
            planets = sys.eventRegions.ToList();
        }

        return planets[rng.Next(planets.Count)];
    }

    public static void TickWarpArrivals(BattlefieldState bf, Random rng)
    {
        foreach (var u in bf.units)
        {
            if (u.displayName == null || !u.displayName.Contains("跃迁中", StringComparison.Ordinal))
            {
                continue;
            }

            if (!u.Arrived(bf.timeSec))
            {
                continue;
            }

            u.displayName = u.displayName.Replace("（跃迁中）", "", StringComparison.Ordinal);
            var spread = 2500f + (float)rng.NextDouble() * 800f;
            u.x = -spread;
            u.y = (float)(rng.NextDouble() * 1600f - 800f);
            u.z = 0f;
            u.vx = 0f;
            u.vy = 0f;
            u.vz = 0f;
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
}
