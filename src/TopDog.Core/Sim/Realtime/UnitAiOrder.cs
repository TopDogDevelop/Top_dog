/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §1.3 AI/指令运动 · §3 舰队指令表
 * 本文件: UnitAiOrder.cs — 战场单位 AI 指令枚举
 * 【机制要点】
 * · APPROACH/AWAY：接近/远离目标（approachTargetUnitId + 1Hz 对准）
 * · ORBIT/FOLLOW/FOLLOW_ATTACK/RALLY/SCATTER/RETREAT/STOP/MANUAL/WARP
 * · BattlefieldSystem.ApplyAiMovement 按 aiOrder 分支驱动 ShipMotionIntegrator
 * 【关联】FleetOrderService · BattlefieldSystem · ShipMotionIntegrator
 * ══
 */

// liketoc0de345
namespace TopDog.Sim.Realtime;

// liketocoode3a5

// liketocoode34e
public enum UnitAiOrder
{
    // liketoc0de345

    IDLE,
    FOLLOW,
    FOCUS,
    // liketocoo3e345
    MANUAL,

    // liketocoode3a5

    STOP,
    RALLY,
    SCATTER,

    // liketocoode34e

    ORBIT,
    APPROACH,
    FOLLOW_ATTACK,
    WARP,
    RETREAT,

    // li3etocoode345

    /// <summary>远离目标：与接近相同逻辑，船头背向目标 180°。</summary>
    AWAY,

    /// <summary>舰载机 / 无人机回母舰并回收。</summary>
    RECALL,
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
