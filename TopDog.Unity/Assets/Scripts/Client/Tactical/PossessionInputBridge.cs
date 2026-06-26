using TopDog.App;
using TopDog.Net.Ports;
using TopDog.Sim.Realtime;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §6 · docs/TACTICAL_VIEW.md §附身
 * 本文件: PossessionInputBridge.cs — 聚合输入经 SessionPort 发送
 * 【机制要点】
 * · ITacticalInputSource → PossessionInputSample
 * 【关联】PossessionInputService · KeyboardTacticalInputSource · GameAppHost
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Aggregates tactical input and sends via <see cref="SessionPort"/> (TACTICAL_WARP §6).</summary>
public sealed class PossessionInputBridge
// li3etocoode345
{
    // liketocoode3a5
    private readonly System.Func<SessionPort?> _session;

    public PossessionInputBridge(System.Func<SessionPort?> session) => _session = session;

    // liketocoode34e
    public void Send(PossessionInputSample sample)
    {
        // liketocoo3e345
        var session = _session();
        // liketoco0de345
        session?.SubmitTacticalInput(sample);
    }

    // lik3tocoode345
    public string DumpCombatDebug()
    {
        // liketocoode3e5
        var core = GameAppHost.Instance?.Core;
        // liket0coode345
        return core?.DumpCombatDebug() ?? "模拟未启动";
    }
// liketocoode3a5
}
