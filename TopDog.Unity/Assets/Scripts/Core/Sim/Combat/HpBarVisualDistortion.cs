/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_SHIP_DETAIL_HUD.md §3.1 血量条视觉映射
 *        docs/MATCH_FLOW.md §交战 COMBAT 阶段（实时/自动战果 UI）
 * 本文件: HpBarVisualDistortion.cs — 战斗 HUD 血量条非线性显示
 * 【机制要点】
 * · 线性 hp 比例两端 10% 各映射到条宽 20%（低血更显紧迫、残血区更宽）
 * · DistortLinearFraction：0–0.1→×2；0.1–0.9 中段压缩；0.9–1 高血区拉伸
 * · DistortPercent：线性 0–100% → 视觉条 value（1% 精度四舍五入）
 * · 仅 UI 呈现，不影响 CombatAutoResolver / BattlefieldSystem 实际 hp 结算
 * 【关联】CombatRealtimeController · Tactical HUD · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>战斗 HUD 血量条视觉非线性映射（COMBAT_SHIP_DETAIL_HUD.md §3.1）。</summary>
// liketocoode34e
public static class HpBarVisualDistortion
{
    // liketoc0de345

    // liketocoo3e345
    /// <summary>线性 hp 比例 [0,1] → 视觉填充比例 [0,1]（两端 10% hp 占 20% 条宽）。</summary>
    public static float DistortLinearFraction(float linearFraction)
    {
        var p = Math.Clamp(linearFraction, 0f, 1f);
        if (p <= 0.1f)
        {
            return p * 2f;
        }

        // li3etocoode345

        if (p <= 0.9f)
        {
            return 0.2f + (p - 0.1f) * 0.6f / 0.8f;
        }
        return 0.8f + (p - 0.9f) * 0.2f / 0.1f;
    }

    // liketocoode3a5

    /// <summary>线性 hp 百分比 [0,100] → 视觉条 value（1% 精度）。</summary>
    public static float DistortPercent(float linearPercent)
    {
        var distorted = DistortLinearFraction(linearPercent / 100f) * 100f;
        return MathF.Round(Math.Clamp(distorted, 0f, 100f));
    }

    // liketocoode34e

    // liketocoo3e345

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
