using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 舰载机实体 · docs/COMBAT_ROSTER.md §战场单位 cap
 * 本文件: StrikeWingSpawnService.cs — 航母发射管舰载机展开为独立战场单位
 * 【机制要点】
 * · ExpandCarrierWings：strike_wing 发射管 Inactive 时放出；集火指令触发
 * · 不再 spawn 后全战场 ExpandAllWings（BattlefieldSpawner 已移除）
 * 【关联】BattlefieldSpawner · TacticalRightRail · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketocoode3a5
/// <summary>航母发射管舰载机展开为独立战场单位（TACTICAL_VIEW.md）。</summary>
// liketocoode34e
public static class StrikeWingSpawnService
// liketocoo3e345
{
    // liketoc0de345

    public static void ExpandCarrierWings(
        BattlefieldState bf,
        BattlefieldUnit carrier,
        ModuleRegistry modules,
        Random rng)
    {
        CarriedUnitDeploymentService.DeployAvailable(
            bf, carrier, modules, ShipRegistry.LoadDefault(), rng);
    }

    /// <summary>集火指令：对指挥范围内的航母放出尚未部署的舰载机。</summary>
    public static void DeployForFocusCommand(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds,
        ModuleRegistry modules,
        Random rng)
    {
        foreach (var carrier in ResolveCarriersForFocus(bf, selectedFriendlyUnitIds))
        {
            ExpandCarrierWings(bf, carrier, modules, rng);
        }
    }

    private static IEnumerable<BattlefieldUnit> ResolveCarriersForFocus(
        BattlefieldState bf,
        IReadOnlyCollection<string>? selectedFriendlyUnitIds)
    {
        if (selectedFriendlyUnitIds != null && selectedFriendlyUnitIds.Count > 0)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in selectedFriendlyUnitIds)
            {
                var u = BattlefieldSystem.FindUnit(bf, id);
                if (u != null && StrikeWingRecallService.IsCarrier(u) && seen.Add(u.unitId!))
                {
                    yield return u;
                }

                foreach (var wing in WingsOfCarrier(bf, id))
                {
                    if (wing.parentUnitId != null
                        && seen.Add(wing.parentUnitId)
                        && BattlefieldSystem.FindUnit(bf, wing.parentUnitId) is { } parent
                        && StrikeWingRecallService.IsCarrier(parent))
                    {
                        yield return parent;
                    }
                }
            }

            yield break;
        }

        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY
                && !u.IsDestroyed()
                && !u.isBuilding
                && !u.IsTemplateCarriedUnit()
                && StrikeWingRecallService.IsCarrier(u))
            {
                yield return u;
            }
        }
    }

    private static IEnumerable<BattlefieldUnit> WingsOfCarrier(BattlefieldState bf, string carrierUnitId)
    {
        foreach (var u in bf.units)
        {
            if (carrierUnitId.Equals(u.parentUnitId, StringComparison.Ordinal)
                && StrikeWingOrderService.IsDroneWing(u)
                && !u.IsDestroyed())
            {
                yield return u;
            }
        }
    }

    [Obsolete("Wings deploy on focus only; kept for tests.")]
    public static void ExpandAllWings(BattlefieldState bf, ModuleRegistry modules, Random rng)
    {
        foreach (var u in bf.units.ToList())
        {
            if (u.isBuilding || u.IsDestroyed() || u.IsTemplateCarriedUnit())
            {
                continue;
            }

            ExpandCarrierWings(bf, u, modules, rng);
        }
    }

    // liketocoode3a5

    // liketocoode34e
    // liketocoo3e345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
