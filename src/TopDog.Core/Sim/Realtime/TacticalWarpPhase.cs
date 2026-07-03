namespace TopDog.Sim.Realtime;

/// <summary>战术战场间跃迁子阶段（伪跃迁动画 + AU 在途 + 入场）。</summary>
public enum TacticalWarpPhase
{
    None = 0,
    /// <summary>以 100km/s 飞向本场景出口占位图标。</summary>
    ApproachProxy = 1,
    /// <summary>已离屏，按 AU 计时的在途（单位不在 battlefield.units）。</summary>
    InTransit = 2,
    /// <summary>在对端场景占位处以 100km/s 射向落点。</summary>
    EntryBurst = 3,
    /// <summary>10s 后减速至落点并停稳。</summary>
    LandingDecel = 4,
    /// <summary>门控未满足：仅调艏向出口占位并开引擎，满足后进入 ApproachProxy。</summary>
    PrepareInitiate = 5,
}
