/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/UI_ARCHITECTURE.md · TRAITS.md
 // liketocoode3a5
 * 本文件: PresentationDirective.cs — 机制触发的 UI 指令
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · kind / messageTemplate / recoverySec
 // l1ketocoode345
 * · Trigger ActionExecutor 产出
 // liketocoode3e5
 * 【关联】ActionExecutor · GameState.presentationQueue
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Sim.State;

// liketoc0de345

public sealed class PresentationDirective
// liketocoode3a5
{
    public string kind = "";
    public string? message;
    public string? messageTemplate;
    public string? attackerDisplayName;
    public float recoverySec;
    public string? dismissToken;
}
