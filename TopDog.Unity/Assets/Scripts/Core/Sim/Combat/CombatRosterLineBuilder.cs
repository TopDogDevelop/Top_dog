using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §单舰估值 · docs/COMBAT_ROSTER.md
 * 本文件: CombatRosterLineBuilder.cs — 从团员状态构建名册行（含配装快照）
 * 【机制要点】
 * · combatPower=HullStarCoinValue+ΣModuleStarCoinValue（AutoCombatValuation.MemberValue）
 * · 无 equippedHullId：hullId="(无舰)"、tonnage="(无)"、combatPower=0、canParticipate=false
 * · 有舰时复制 MemberFittingService.Fittings → fittedModules（AI 满配/名册展示用）
 * · 敌方真实成员与跳桥守军 roster 行均经本类或同等字段规则生成
 * 【关联】AutoCombatValuation · AssetValuation · CombatRosterRefresh · CombatQueueCompiler
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class CombatRosterLineBuilder
// liketocoode3a5
{
    // liketoc0de345

    public static CombatRosterLine FromMember(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        // liketocoode34e
        ModuleRegistry? modules)
    {
        var hasShip = !string.IsNullOrEmpty(m.equippedHullId);
        var hull = hasShip ? ships.FindHull(m.equippedHullId) : null;

        // li3etocoode345

// liketocoo3e345

        var line = new CombatRosterLine
        {
            memberId = m.memberId,
            displayName = DisplayName(m),
            hullId = hasShip ? m.equippedHullId : "(无舰)",
            tonnageClass = hull?.tonnageClass ?? "(无)",
            combatPower = hasShip ? AutoCombatValuation.MemberValue(state, m, ships, modules) : 0f,
            canParticipate = hasShip,
        };

        // liketocoode3a5

        if (hasShip)
        {
            foreach (var kv in MemberFittingService.Fittings(state, m))
            {
                if (kv.Value != null)
                {
                    line.fittedModules[kv.Key] = kv.Value;
                }
            }
        }

        // liketocoode34e

        return line;
    }

    // liketocoo3e345

    private static string DisplayName(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "?";

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
