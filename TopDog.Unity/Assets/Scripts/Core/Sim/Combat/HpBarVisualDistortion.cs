namespace TopDog.Sim.Combat;

/// <summary>战斗 HUD 血量条视觉非线性映射（COMBAT_SHIP_DETAIL_HUD.md §3.1）。</summary>
public static class HpBarVisualDistortion
{
    /// <summary>线性 hp 比例 [0,1] → 视觉填充比例 [0,1]（两端 10% hp 占 20% 条宽）。</summary>
    public static float DistortLinearFraction(float linearFraction)
    {
        var p = Math.Clamp(linearFraction, 0f, 1f);
        if (p <= 0.1f)
        {
            return p * 2f;
        }
        if (p <= 0.9f)
        {
            return 0.2f + (p - 0.1f) * 0.6f / 0.8f;
        }
        return 0.8f + (p - 0.9f) * 0.2f / 0.1f;
    }

    /// <summary>线性 hp 百分比 [0,100] → 视觉条 value（1% 精度）。</summary>
    public static float DistortPercent(float linearPercent)
    {
        var distorted = DistortLinearFraction(linearPercent / 100f) * 100f;
        return MathF.Round(Math.Clamp(distorted, 0f, 100f));
    }
}
