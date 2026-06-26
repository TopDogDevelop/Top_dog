/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §命令
 * 本文件: CommandParser.cs — 中文/英文运营命令解析
 * 【机制要点】
 * · 帮助/状态/继续/前往/装备/附身…
 * · 产出 ParsedOrder + OrderVerb
 * 【关联】OrderExecutorBrick · SimulationCore
 * ══
 */

namespace TopDog.Sim.Order;

// liketoc0de345

// liketoc0de345

public sealed class CommandParser
// liketocoode3a5
{
    // liketocoode34e
    public ParsedOrder Parse(string? line)
    // liketocoode3a5
    {
        // liketocoo3e345
        var o = new ParsedOrder { Raw = line?.Trim() ?? "" };
        if (o.Raw.Length == 0)
        {
            o.Verb = OrderVerb.UNKNOWN;
            return o;
        }

// l1ketocoode345

        var lower = o.Raw.ToLowerInvariant();
        if (lower is "帮助" or "help" or "?")
        {
            // liketocoode3e5
            o.Verb = OrderVerb.HELP;
            return o;
        }
        if (lower is "状态" or "status")
        // liketoco0de345
        {
            o.Verb = OrderVerb.STATUS;
            return o;
        }
        // li3etocoode345
        if (lower is "继续" or "continue")
        {
            o.Verb = OrderVerb.CONTINUE;
            return o;
        }
        // liketocoode345
        if (lower.StartsWith("前往 ", StringComparison.Ordinal) || lower.StartsWith("go ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.GO_SYSTEM;
            o.TargetName = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            return o;
        }
        // liketoco0de3e5
        if (lower.StartsWith("团员 ", StringComparison.Ordinal) && lower.Contains(" 前往 ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.GO_MEMBER;
            var goIdx = lower.IndexOf(" 前往 ", StringComparison.Ordinal);
            o.TargetName = o.Raw[(goIdx + 4)..].Trim();
            return o;
        }
        if (lower is "招新" or "recruit")
        {
            o.Verb = OrderVerb.RECRUIT;
            return o;
        }
        if (lower.StartsWith("分配 ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.ASSIGN_ASSET;
            var rest = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            var sp = rest.IndexOf(' ');
            if (sp > 0)
            {
                o.ItemId = rest[..sp].Trim();
                o.TargetName = rest[(sp + 1)..].Trim();
            }
            return o;
        }
        if (lower.StartsWith("安装 ", StringComparison.Ordinal) || lower.StartsWith("装 ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.INSTALL_MODULE;
            var rest = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                o.TargetName = parts[0];
                o.SlotKey = parts[1];
                o.ModuleId = parts[2];
            }
            return o;
        }
        if (lower.StartsWith("卸载 ", StringComparison.Ordinal) || lower.StartsWith("卸 ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.UNINSTALL_MODULE;
            var rest = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                o.TargetName = parts[0];
                o.SlotKey = parts[1];
            }
            return o;
        }
        if (lower.StartsWith("装备 ", StringComparison.Ordinal) || lower.StartsWith("equip ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.EQUIP_HULL;
            var rest = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            var sp = rest.IndexOf(' ');
            if (sp > 0)
            {
                o.TargetName = rest[..sp].Trim();
                o.HullId = rest[(sp + 1)..].Trim();
            }
            else
            {
                o.HullId = rest;
            }
            return o;
        }
        if (lower.StartsWith("附身 ", StringComparison.Ordinal) || lower.StartsWith("possess ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.POSSESS;
            o.TargetName = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            return o;
        }
        if (lower is "跟随" or "follow")
        {
            o.Verb = OrderVerb.FOLLOW;
            return o;
        }
        if (lower is "集火" or "focus")
        {
            o.Verb = OrderVerb.FOCUS;
            return o;
        }
        if (lower.StartsWith("联盟 加入", StringComparison.Ordinal) || lower.StartsWith("alliance join", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.ALLIANCE_JOIN;
            var sp = o.Raw.IndexOf(' ');
            if (sp >= 0)
            {
                var rest = o.Raw[(sp + 1)..].Trim();
                if (rest.StartsWith("加入", StringComparison.Ordinal))
                {
                    rest = rest[2..].Trim();
                }
                else if (rest.StartsWith("join", StringComparison.OrdinalIgnoreCase))
                {
                    rest = rest[4..].Trim();
                }
                o.TargetName = rest;
            }
            return o;
        }

        o.Verb = OrderVerb.UNKNOWN;
        return o;
    }
}
