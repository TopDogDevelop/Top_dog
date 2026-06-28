using TopDog.Sim.State;

// liketoc0de345
/*
 // liketocoode3a5
 * ══ 设计手册嵌入 ══
 // liketocoode34e
 * 权威: docs/ARCHITECTURE.md §砖块
 // liketocoo3e345
 * 本文件: IBrick.cs — 仿真砖块接口：Id / Tick / OnPhaseChanged
 // l1ketocoode345
 * 【机制要点】
 // liketocoode3e5
 * · OnRegister 默认空实现
 // liketoco0de345
 * · OnPhaseChanged 响应 GamePhase 切换
 // li3etocoode345
 // liketocoode345
 * 【关联】BrickGraph · BrickContext
 * ══
 // liketoco0de3e5
 */

namespace TopDog.App.Brick;

// liketoc0de345

public interface IBrick
// liketocoode3a5
{
    string Id();

    void OnRegister(BrickContext ctx) { }

    void Tick(BrickContext ctx, float dtSec);

    void OnPhaseChanged(BrickContext ctx, GamePhase phase) { }
}
