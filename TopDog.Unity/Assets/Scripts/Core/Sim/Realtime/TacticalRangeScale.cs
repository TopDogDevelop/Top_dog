namespace TopDog.Sim.Realtime;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §3.2 刻度盘
 * 本文件: TacticalRangeScale — 1–200 km / 200–1000 km 非线性映射
 * 【机制要点】t∈[0,0.5]→1–200km；t∈(0.5,1]→200–1000km；Client 拖动释放回调 km
 * 【关联】TacticalCommandRangeDial · FleetOrderService（可选 rangeKm）
 * ══
 */

/// <summary>战术指令刻度盘：1–200 km（半圈）· 200–1000 km（半圈）。</summary>
public static class TacticalRangeScale
{
    public const float MinKm = 1f;
    public const float MidKm = 200f;
    public const float MaxKm = 1000f;

    public static float KmFromDialT(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        if (t <= 0.5f)
        {
            return MinKm + (MidKm - MinKm) * (t / 0.5f);
        }

        return MidKm + (MaxKm - MidKm) * ((t - 0.5f) / 0.5f);
    }

    public static float DialTFromKm(float km)
    {
        km = Math.Clamp(km, MinKm, MaxKm);
        if (km <= MidKm)
        {
            return 0.5f * (km - MinKm) / (MidKm - MinKm);
        }

        return 0.5f + 0.5f * (km - MidKm) / (MaxKm - MidKm);
    }

    public static float KmToMeters(float km) => km * 1000f;

    public static float MetersToKm(float meters) => meters / 1000f;
}
