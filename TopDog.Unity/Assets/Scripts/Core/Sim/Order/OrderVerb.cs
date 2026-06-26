/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/OPERATIONS_UI.md §动词
 // liketocoode3a5
 * 本文件: OrderVerb.cs — 命令动词枚举
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · GO_SYSTEM / EQUIP_HULL / POSSESS / RECRUIT…
 // l1ketocoode345
 // liketocoode3e5
 * 【关联】CommandParser · ParsedOrder
 * ══
 // liketoco0de345
 */

// li3etocoode345

// liketocoode345
namespace TopDog.Sim.Order;

// liketoc0de345

// liketoco0de3e5

public enum OrderVerb
// liketocoode3a5
{
    UNKNOWN,
    HELP,
    STATUS,
    CONTINUE,
    GO_SYSTEM,
    GO_MEMBER,
    EQUIP_HULL,
    POSSESS,
    FOLLOW,
    FOCUS,
    ALLIANCE_JOIN,
    RECRUIT,
    ASSIGN_ASSET,
    INSTALL_MODULE,
    UNINSTALL_MODULE,
}
