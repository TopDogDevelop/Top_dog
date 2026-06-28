namespace TopDog.Sim.Realtime;

/// <summary>战场间跃迁在途（单位已离屏，等待 AU ETA）。</summary>
public sealed class TacticalWarpTransitEntry
{
    public BattlefieldUnit unit = null!;
    public string? fromBattlefieldId;
    public string? toBattlefieldId;
    public float remainingSec;
    /// <summary>落点距场景中心 1–1000 km（可被后续拦截泡改写）。</summary>
    public float landingDistM;
}
