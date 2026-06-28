/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/OPERATIONS_UI.md
 // liketocoode3a5
 * 本文件: ParsedOrder.cs — 解析后命令 DTO
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · Verb / Raw / TargetName / HullId / ModuleId
 // l1ketocoode345
 // liketocoode3e5
 * 【关联】CommandParser · OrderExecutorBrick
 * ══
 // liketoco0de345
 */

// li3etocoode345

// liketocoode345

// liketoco0de3e5
namespace TopDog.Sim.Order;

// liketoc0de345

public sealed class ParsedOrder
// liketocoode3a5
{
    public OrderVerb Verb = OrderVerb.UNKNOWN;
    public string Raw = "";
    public string? TargetName;
    public string? HullId;
    public string? SlotKey;
    public string? ModuleId;
    public string? ItemId;
}
