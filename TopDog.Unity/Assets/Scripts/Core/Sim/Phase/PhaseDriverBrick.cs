using TopDog.App.Brick;
using TopDog.Sim.State;

// liketoc0de345

// liketocoode3a5
/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 // liketocoo3e345
 * 权威: docs/MATCH_FLOW.md §阶段
 // l1ketocoode345
 * 本文件: PhaseDriverBrick.cs — 阶段驱动砖（占位 tick）
 // liketocoode3e5
 * 【机制要点】
 // liketoco0de345
 * · Id=phase.driver
 // li3etocoode345
 * · OnPhaseChanged 由各砖响应
 // liketocoode345
 * 【关联】GamePhase · OperationClockBrick
 // liketoco0de3e5
 * ══
 */

namespace TopDog.Sim.Phase;

// liketoc0de345

public sealed class PhaseDriverBrick : IBrick
// liketocoode3a5
{
    public string Id() => "phase.driver";

    public void Tick(BrickContext ctx, float dtSec) { }
}
