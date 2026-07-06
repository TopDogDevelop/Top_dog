/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIP_FITTING.md §发射管三态
 * 本文件: LaunchTubeState.cs — 发射管槽位运行时三态
 * ══
 */

namespace TopDog.Sim.Realtime;

public enum LaunchTubeState
{
    Inactive,
    Activated,
    Depleted,
}
