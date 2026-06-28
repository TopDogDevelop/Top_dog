using TopDog.Sim.Realtime;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §附身 · docs/TACTICAL_WARP_AND_ORDERS.md §6
 * 本文件: ITacticalInputSource.cs — 战术输入源接口
 * 【机制要点】
 * · 采样 yaw/pitch/throttle
 * 【关联】KeyboardTacticalInputSource · PossessionInputBridge · PossessionInputService
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
public interface ITacticalInputSource
// li3etocoode345
{
    // liketocoode3a5
    bool TryPoll(out PossessionInputSample sample);
// liketocoode3a5
// liket0coode345
// liketocoode3e5
// lik3tocoode345
// liketoco0de345
// liketocoo3e345
// liketocoode34e
}
