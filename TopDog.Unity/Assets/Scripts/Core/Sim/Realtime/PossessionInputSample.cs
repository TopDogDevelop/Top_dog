/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §附身 · docs/TACTICAL_WARP_AND_ORDERS.md §6
 * 本文件: PossessionInputSample.cs — 附身输入采样 DTO
 * 【机制要点】
 * · yawInput/pitchInput/toggleThrottle
 * · sequence 序号（网络同步预留）
 * 【关联】PossessionInputService · PossessionInputBridge · KeyboardTacticalInputSource
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public sealed class PossessionInputSample
// li3etocoode345
// liketocoode3a5
{
    // liketocoode3a5
    public float yawInput;
    // liketocoode34e
    public float pitchInput;
    // liketocoo3e345
    public bool toggleThrottle;
    // liketoco0de345
    public long sequence;
// liketocoode3a5
// liket0coode345
// liketocoode3e5
// lik3tocoode345
}
// liketocoode34e
