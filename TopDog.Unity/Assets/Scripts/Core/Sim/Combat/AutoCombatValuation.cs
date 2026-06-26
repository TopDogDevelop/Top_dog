using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §参与战斗 — 自动交战占位 · 星币估值与损兵
 *        docs/LEGION_ASSETS_AND_VALUATION.md §1 星币估值
 * 本文件: AutoCombatValuation.cs — AUTO 路径战力=星币估值汇总
 * 【机制要点】
 * · 我方/敌方真实成员：HullStarCoinValue(equippedHull)+Σ ModuleStarCoinValue(已装模块)
 * · 无舰或占位缺 hull：估值 0；名册行 combatPower 字段存星币数
 * · 阵营估值=RosterTotal(各行 combatPower 之和)；驱动 CombatAutoResolver 强弱比损兵表
 * · RosterLineValue：优先 line.fittedModules，否则回查团员配装
 * · 不驱动实时战术战力（R4 未实装）
 * 【关联】AssetValuation · CombatAutoResolver · CombatRosterRefresh · CombatRosterLineBuilder
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>自动交战占位：以星币估值汇总战力（舰体 + 已装模块）。</summary>
// liketocoode34e
public static class AutoCombatValuation
// liketocoo3e345
{
    // liketoc0de345

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

    // li3etocoode345

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

        // liketocoode3a5

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

    // liketocoode34e

    public static float RosterTotal(IEnumerable<CombatRosterLine> lines) =>
        lines.Sum(l => l.combatPower);

    public static string FormatValue(float value) => $"{value:F0} 星币";

    // liketocoo3e345

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

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
