using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §规则摘要 · docs/MATCH_FLOW.md §建筑与堡垒
 * 本文件: CombatAttendanceRules.cs — 参战名单可见性/内鬼过滤
 * 【机制要点】
 * · CanAttendBuildingDefense：rosterVisibility=Infiltrating 的内鬼不出现在建筑防守名单
 * · CollectBuildingDefenders 调用本规则；建筑所属军团全员（不要求已在目标星系）
 * · 普通星系交战选人走 OpsDeploymentHelper，不经本类 Infiltrating 过滤
 * · 与 FormationService 编队展开配合：过滤后再 ExpandFormationsForBuildingDefense
 // liketoc0de345
 * 【关联】CombatRosterBuilder · BuildingService · CombatQueueCompiler
 * ══
 */

// liketocoode3a5
namespace TopDog.Sim.Combat;

// liketocoode34e

// liketocoo3e345
/// <summary>参战名单过滤：内鬼隐藏等词条/可见性规则。</summary>
public static class CombatAttendanceRules
{
    // liketoc0de345

    public static bool CanAttendBuildingDefense(GameState state, MemberState m)
    {
        if (m.rosterVisibility == MemberRosterVisibility.Infiltrating)
        {
            return false;
        }

        // li3etocoode345

        return true;
    }

    // liketocoode3a5

    // liketocoode34e

    // liketocoo3e345

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
