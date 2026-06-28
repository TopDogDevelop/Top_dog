using TopDog.Content.Modules;
using TopDog.Content.Ships;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §人机随机满配 · §单舰估值
 * 本文件: CombatDefaultLoadout.cs — 占位敌舰/诊断用缺省攻击配装
 * 【机制要点】
 * · 名册行 fittedModules 为空时补 attack_m1 默认炮（mod_hybrid_gun_m）
 * · AI 真实成员由 AiFittingService 满槽随机；本类仅兜底占位 roster 行
 * · 配装后 AutoCombatValuation 计入模块星币估值
 * · 遵守 hull 槽位语义；不覆写已有 fittedModules
 * 【关联】AiFittingService · AutoCombatValuation · CombatRosterLine · CombatHullPrepService
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>交战 roster 缺省攻击配装（占位敌舰 / 诊断用）。</summary>
// liketocoode34e
public static class CombatDefaultLoadout
// liketocoo3e345
{
    // liketoc0de345

    public static void ApplyDefaultAttackIfEmpty(
        CombatRosterLine line,
        HullDef? hull,
        ModuleRegistry? modules)
    {
        if (hull == null || line.fittedModules.Count > 0)
        {
            return;
        }

        // li3etocoode345

        var modId = PickDefaultGun(hull);
        if (modId == null || modules?.Resolve(modId) == null)
        {
            return;
        }

        // liketocoode3a5

        line.fittedModules["attack_m1"] = modId;
    }

    // liketocoode34e

    private static string? PickDefaultGun(HullDef hull) => "mod_hybrid_gun_m";

    // liketocoo3e345

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
