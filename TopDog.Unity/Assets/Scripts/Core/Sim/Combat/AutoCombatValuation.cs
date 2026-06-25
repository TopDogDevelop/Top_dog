using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

/// <summary>自动交战占位：以星币估值汇总战力（舰体 + 已装模块）。</summary>
public static class AutoCombatValuation
{
    public static float MemberValue(
        GameState state,
        MemberState? m,
        ShipRegistry? ships,
        ModuleRegistry? modules)
    {
        if (m == null || ships == null || string.IsNullOrEmpty(m.equippedHullId))
        {
            return 0f;
        }
        var total = AssetValuation.HullStarCoinValue(ships.FindHull(m.equippedHullId));
        if (modules == null)
        {
            return total;
        }
        foreach (var kv in MemberFittingService.Fittings(state, m))
        {
            total += AssetValuation.ModuleStarCoinValue(modules.Resolve(kv.Value));
        }
        return total;
    }

    public static float RosterLineValue(
        GameState? state,
        CombatRosterLine? line,
        ShipRegistry? ships,
        ModuleRegistry? modules)
    {
        if (line == null || ships == null || string.IsNullOrEmpty(line.hullId) || line.hullId == "(无舰)")
        {
            return 0f;
        }
        var total = AssetValuation.HullStarCoinValue(ships.FindHull(line.hullId));
        if (modules == null)
        {
            return total;
        }
        if (line.fittedModules.Count > 0)
        {
            foreach (var kv in line.fittedModules)
            {
                total += AssetValuation.ModuleStarCoinValue(modules.Resolve(kv.Value));
            }
            return total;
        }
        if (state != null && line.memberId != null)
        {
            var m = FindMember(state, line.memberId);
            if (m != null)
            {
                foreach (var kv in MemberFittingService.Fittings(state, m))
                {
                    total += AssetValuation.ModuleStarCoinValue(modules.Resolve(kv.Value));
                }
            }
        }
        return total;
    }

    public static float RosterTotal(IEnumerable<CombatRosterLine> lines) =>
        lines.Sum(l => l.combatPower);

    public static string FormatValue(float value) => $"{value:F0} 星币";

    private static MemberState? FindMember(GameState state, string id)
    {
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }
}
