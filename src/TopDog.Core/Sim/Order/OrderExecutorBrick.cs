using TopDog.App.Brick;
using TopDog.Content.Ships;
using TopDog.Sim.Alliance;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Possession;
using TopDog.Sim.State;
using TopDog.Sim.Ship;
using TopDog.Sim.Tutorial;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md · ARCHITECTURE.md
 * 本文件: OrderExecutorBrick.cs — 命令执行砖
 * 【机制要点】
 * · Execute：按 OrderVerb 分发联盟/招募/配舰/附身
 * · 委托 FleetTransitBrick / TutorialOpsBrick
 * 【关联】CommandParser · PossessionService
 * ══
 */

namespace TopDog.Sim.Order;

// liketoc0de345

// liketoc0de345

public sealed class OrderExecutorBrick : IBrick
// liketocoode3a5
{
    private FleetTransitBrick? _transit;
    // liketocoode34e
    private TutorialOpsBrick? _tutorial;

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public string Id() => "order.executor";

// liketocoode3e5

    // liketoco0de345
    public void Tick(BrickContext ctx, float dtSec) { }

    public string Execute(BrickContext ctx, ParsedOrder order)
    {
        var echo = order.Verb switch
        {
            OrderVerb.HELP => HelpText(ctx),
            OrderVerb.STATUS => StatusText(ctx),
            OrderVerb.CONTINUE => DoContinue(ctx),
            OrderVerb.GO_SYSTEM or OrderVerb.GO_MEMBER => GoSystem(ctx, order.TargetName),
            OrderVerb.EQUIP_HULL => EquipHull(ctx, order.TargetName, order.HullId),
            OrderVerb.RECRUIT => RecruitService.Start(ctx.State, null),
            OrderVerb.ASSIGN_ASSET => AssignAsset(ctx, order.ItemId, order.TargetName),
            OrderVerb.INSTALL_MODULE => InstallModule(ctx, order),
            OrderVerb.UNINSTALL_MODULE => UninstallModule(ctx, order),
            OrderVerb.POSSESS => PossessMember(ctx, order.TargetName),
            OrderVerb.FOLLOW => PossessionService.OrderFollow(ctx.State),
            OrderVerb.FOCUS => PossessionService.OrderFocus(ctx.State),
            OrderVerb.ALLIANCE_JOIN => AllianceJoinService.Join(ctx.State, order.TargetName),
            // li3etocoode345
            _ => "未知命令: " + order.Raw,
        // liketocoode345
        };
        ctx.State.lastCommandEcho = echo;
        // liketoco0de3e5
        PushAlert(ctx, echo);
        return echo;
    }

    private static string HelpText(BrickContext ctx)
    {
        var sb = new System.Text.StringBuilder("命令: 帮助 | 状态 | 继续");
        if (ctx.State.map != null)
        {
            sb.Append(" | 前往 <星系>");
        }
        sb.Append(" | 附身 <团员> | 跟随 | 集火 | 联盟 加入 <名>");
        sb.Append(" | 招新 | 分配 <物品> <团员> | 安装 <团员> <槽> <模块> | 卸载 <团员> <槽>");
        return sb.ToString();
    }

    private static string StatusText(BrickContext ctx)
    {
        var sys = ctx.State.currentSolarSystemId ?? "?";
        return $"阶段={ctx.State.phase} 星系={sys} 团员={ctx.State.members.Count} 运营剩余={ctx.State.operationTimeRemainingSec:F0}s 教程{ctx.State.tutorialStep}";
    }

    private string DoContinue(BrickContext ctx)
    {
        if (_tutorial != null && _tutorial.Advance(ctx))
        {
            return ctx.State.tutorialComplete ? "教程完成" : "教程前进";
        }
        return "无教程进度";
    }

    private string GoSystem(BrickContext ctx, string? targetName)
    {
        if (_transit == null || ctx.State.members.Count == 0)
        {
            return "跃迁未启用";
        }
        var target = _transit.FindSystemByName(ctx, targetName);
        if (target == null)
        {
            return "找不到星系 " + targetName;
        }
        var leader = ctx.State.members[0];
        if (leader.equippedHullId == null)
        {
            return "主团员未配船";
        }
        if (_transit.RequestTransit(ctx, leader, target.solarSystemId!))
        {
            return "跃迁开始：" + target.name;
        }
        return "无法跃迁（无跳桥或未配船）";
    }

    private static string EquipHull(BrickContext ctx, string? memberName, string? hullId)
    {
        if (hullId == null)
        {
            return "缺少 hullId";
        }
        var hull = ctx.Ships.FindHull(hullId);
        if (hull == null)
        {
            return "未知舰体: " + hullId;
        }
        var m = FindMemberByName(ctx, memberName) ?? (ctx.State.members.Count > 0 ? ctx.State.members[0] : null);
        if (m == null)
        {
            return "找不到团员";
        }
        m.equippedHullId = hullId;
        return m.name + " 装备 " + hull.displayName;
    }

    private static string AssignAsset(BrickContext ctx, string? itemId, string? memberName)
    {
        if (itemId == null)
        {
            return "缺少物品 id";
        }
        var m = FindMemberByName(ctx, memberName);
        if (m == null)
        {
            return "找不到团员";
        }
        var issuerLegionId = ctx.State.commandIssuerLegionId;
        if (!string.IsNullOrWhiteSpace(issuerLegionId))
        {
            var memberLegion = LegionQuery.OfMember(m);
            if (!issuerLegionId.Equals(memberLegion, StringComparison.Ordinal))
            {
                return "该团员不属于命令军团";
            }
        }
        if (MemberAssetService.LegionQtyFor(ctx.State, issuerLegionId, itemId) <= 0)
        {
            return "军团库存不足";
        }
        MemberAssetService.TransferLegionToPersonal(ctx.State, m, itemId);
        return "已分配 " + itemId + " → " + m.name;
    }

    private static string InstallModule(BrickContext ctx, ParsedOrder order)
    {
        if (order.SlotKey == null || order.ModuleId == null)
        {
            return "用法: 安装 <团员> <槽位> <模块>";
        }
        var m = FindMemberByName(ctx, order.TargetName);
        if (m == null)
        {
            return "找不到团员";
        }
        var hull = m.equippedHullId != null ? ctx.Ships.FindHull(m.equippedHullId) : null;
        return MemberFittingService.EquipModule(
            ctx.State, m, order.SlotKey, order.ModuleId, null, hull, ctx.Modules);
    }

    private static string UninstallModule(BrickContext ctx, ParsedOrder order)
    {
        if (order.SlotKey == null)
        {
            return "用法: 卸载 <团员> <槽位>";
        }
        var m = FindMemberByName(ctx, order.TargetName);
        if (m == null)
        {
            return "找不到团员";
        }
        return MemberFittingService.UnequipModule(ctx.State, m, order.SlotKey, ctx.Modules);
    }

    private static string PossessMember(BrickContext ctx, string? memberName)
    {
        var m = FindMemberByName(ctx, memberName);
        if (m == null)
        {
            return "找不到团员 " + memberName;
        }
        return PossessionService.Possess(ctx.State, m.memberId!);
    }

    private static MemberState? FindMemberByName(BrickContext ctx, string? name)
    {
        if (name == null)
        {
            return null;
        }
        var n = name.Trim();
        foreach (var m in ctx.State.members)
        {
            if (n == m.name || n == m.memberId)
            {
                return m;
            }
        }
        return null;
    }

    public void Bind(FleetTransitBrick? transit, TutorialOpsBrick? tutorial)
    {
        _transit = transit;
        _tutorial = tutorial;
    }

    private static void PushAlert(BrickContext ctx, string msg)
    {
        ctx.State.alertLog.Add(msg);
        if (ctx.State.alertLog.Count > 50)
        {
            ctx.State.alertLog.RemoveAt(0);
        }
    }
}
