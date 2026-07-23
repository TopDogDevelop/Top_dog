/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_FX.md
 * 本文件: CombatFxEvent.cs — 特效只读事件（Client Drain，不改结算）
 * 【机制要点】
 * · kind + firer/target id；端点由 Client 每帧读单位实时位
 * · 写入 BattlefieldState.pendingCombatFx
 * 【关联】CombatFxEmit · CombatFxTracerPresenter
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>特效表现事件；机制结算不受影响。</summary>
public sealed class CombatFxEvent
{
    public const string KindHybridGunTracer = "hybrid_gun_tracer";

    public string? kind;
    public string? firerUnitId;
    public string? targetUnitId;
    /// <summary>开火瞬间距离（m）；Client 用于 duration=min(1, dist/50000)。</summary>
    public float distAtSpawnM;
    public float battleTimeSec;
}
