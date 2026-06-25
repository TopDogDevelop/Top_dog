namespace TopDog.Sim.Order;

public sealed class CommandParser
{
    public ParsedOrder Parse(string? line)
    {
        var o = new ParsedOrder { Raw = line?.Trim() ?? "" };
        if (o.Raw.Length == 0)
        {
            o.Verb = OrderVerb.UNKNOWN;
            return o;
        }

        var lower = o.Raw.ToLowerInvariant();
        if (lower is "帮助" or "help" or "?")
        {
            o.Verb = OrderVerb.HELP;
            return o;
        }
        if (lower is "状态" or "status")
        {
            o.Verb = OrderVerb.STATUS;
            return o;
        }
        if (lower is "继续" or "continue")
        {
            o.Verb = OrderVerb.CONTINUE;
            return o;
        }
        if (lower.StartsWith("前往 ", StringComparison.Ordinal) || lower.StartsWith("go ", StringComparison.Ordinal))
        {
            o.Verb = OrderVerb.GO_SYSTEM;
            o.TargetName = o.Raw[(o.Raw.IndexOf(' ') + 1)..].Trim();
            return o;
        }
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
