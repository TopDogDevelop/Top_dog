using System.Text;
using TopDog.Content.Map;
using TopDog.Sim.State;

namespace TopDog.Client;

/// <summary>Builds operations overlay body text from live GameState.</summary>
internal static class OperationsOverlayBuilder
{
    public static string BuildLegionAssets(GameState s)
    {
        var sb = new StringBuilder();
        sb.AppendLine("军团舰库存 · 装备库（左键物品 → 分配给团员）");
        sb.AppendLine();
        if (s.legionStock.Count == 0)
        {
            sb.AppendLine("（库存为空 — 开局资产加载后显示）");
        }
        else
        {
            foreach (var kv in s.legionStock)
            {
                sb.AppendLine($"· {kv.Key} × {kv.Value}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("操作：选中团员 → 点击条目 → 「分配给…」");
        return sb.ToString().TrimEnd();
    }

    public static string BuildMemberCodex(GameState s)
    {
        var sb = new StringBuilder();
        sb.AppendLine("团员图鉴 — 稀有度 / 卡面底图 / 简介");
        sb.AppendLine();
        if (s.members.Count == 0)
        {
            sb.AppendLine("（暂无团员）");
            return sb.ToString().TrimEnd();
        }
        foreach (var m in s.members)
        {
            var name = !string.IsNullOrEmpty(m.name) ? m.name : m.memberId ?? "团员";
            var backdrop = string.IsNullOrEmpty(m.cardBackdrop) ? "—" : m.cardBackdrop;
            var rarity = !m.appraised && m.rarity == "U" ? "U(未鉴定)" : m.rarity;
            sb.AppendLine($"【{name}】 {rarity} · 底图 {backdrop}");
            if (m.traitIds.Count > 0)
            {
                sb.AppendLine("  词条: " + string.Join(", ", m.traitIds));
            }
            if (!string.IsNullOrEmpty(m.bio))
            {
                sb.AppendLine("  " + m.bio);
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public static string BuildRecruit(GameState s)
    {
        var sb = new StringBuilder();
        sb.AppendLine("招新窗口（20 秒）");
        sb.AppendLine();
        sb.AppendLine($"当前团员 {s.members.Count} · 运营剩余 {FormatTime(s.operationTimeRemainingSec)}");
        sb.AppendLine();
        sb.AppendLine("· 随机生成 1–3 名候选团员");
        sb.AppendLine("· 消耗军团资金 / 能量（Balance 实装后扣费）");
        sb.AppendLine("· 点击候选卡「招募」加入军团");
        sb.AppendLine();
        sb.AppendLine("（首包 UI 占位 — 候选列表与倒计时下一迭代接入 RecruitOverlay）");
        return sb.ToString().TrimEnd();
    }

    public static string BuildShipFitting(GameState s, MemberState member)
    {
        var sb = new StringBuilder();
        var name = !string.IsNullOrEmpty(member.name) ? member.name : member.memberId ?? "团员";
        sb.AppendLine($"配船 — {name}");
        sb.AppendLine();
        sb.AppendLine($"舰体: {member.equippedHullId ?? "无（从库存分配舰体）"}");
        sb.AppendLine($"能量 {member.energy} · 智慧 {member.wisdom} · 资金 {member.funds}");
        sb.AppendLine();
        sb.AppendLine("环状装配（EVE 式）：");
        sb.AppendLine("· 攻击 / 防御 / 导航槽 — 从个人+军团库存选装");
        sb.AppendLine("· 推进器全舰仅可装 1 个 [推进]");
        sb.AppendLine("· 军团库存物品需先「转入个人」再装配");
        sb.AppendLine();
        sb.AppendLine("（槽位明细与 DPS/盾回 — MemberDetailSlidePanel R2）");
        return sb.ToString().TrimEnd();
    }

    public static string BuildMemberDetail(GameState s, MemberState m)
    {
        var sb = new StringBuilder();
        var name = !string.IsNullOrEmpty(m.name) ? m.name : m.memberId ?? "团员";
        sb.AppendLine(name);
        sb.AppendLine($"稀有度 {m.rarity} · 底图 {m.cardBackdrop ?? "—"}");
        sb.AppendLine($"任务 {m.assignedTask} @ {SystemName(s, m.currentSolarSystemId)}");
        sb.AppendLine($"舰体 {m.equippedHullId ?? "无"} · 编队 {m.formationId ?? "—"}");
        sb.AppendLine($"能量 {m.energy} · 智慧 {m.wisdom} · 资金 {m.funds}");
        if (m.traitIds.Count > 0)
        {
            sb.AppendLine("词条: " + string.Join(", ", m.traitIds));
        }
        sb.AppendLine();
        sb.AppendLine("操作槽（OperationSlot · 置顶）");
        sb.AppendLine("· 选择装备 → 装 / 卸");
        if (!string.IsNullOrEmpty(m.bio))
        {
            sb.AppendLine();
            sb.AppendLine(m.bio);
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatTime(float sec)
    {
        var rem = System.Math.Max(0, (int)sec);
        return $"{rem / 60:00}:{rem % 60:00}";
    }

    private static string SystemName(GameState s, string? systemId)
    {
        if (string.IsNullOrEmpty(systemId))
        {
            return "—";
        }
        var map = s.map?.Project;
        if (map != null)
        {
            var sys = map.FindSystem(systemId);
            if (sys != null && !string.IsNullOrEmpty(sys.name))
            {
                return sys.name;
            }
        }
        return systemId;
    }
}
