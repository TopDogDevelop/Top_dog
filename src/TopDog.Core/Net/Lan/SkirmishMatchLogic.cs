namespace TopDog.Net.Lan;

public enum SkirmishMatchPhase
{
    Idle,
    Seeking,
    ScaleMismatch,
    Ready,
}

public sealed class SkirmishMatchSnapshot
{
    public SkirmishMatchPhase Phase = SkirmishMatchPhase.Idle;
    public string? PeerIp;
    public int OpponentScale;
    public string? HostIp;
    public bool IsLocalHost;
    public string StatusMessage = "";
}

/// <summary>约战真人匹配：配对、规模协商、随机房主（确定性 hash）。</summary>
public static class SkirmishMatchLogic
{
    public static SkirmishMatchSnapshot Evaluate(
        string localIp,
        int localScale,
        IReadOnlyList<SkirmishMatchPacket> recentPeers,
        float nowSec,
        float peerTtlSec = 4f)
    {
        SkirmishMatchPacket? best = null;
        foreach (var peer in recentPeers)
        {
            if (peer.localIp == localIp)
            {
                continue;
            }

            if (!string.Equals(peer.state, "seeking", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            best = peer;
            break;
        }

        if (best == null)
        {
            return new SkirmishMatchSnapshot
            {
                Phase = SkirmishMatchPhase.Seeking,
                StatusMessage = "正在匹配局域网对手…",
            };
        }

        if (best.scale != localScale)
        {
            return new SkirmishMatchSnapshot
            {
                Phase = SkirmishMatchPhase.ScaleMismatch,
                PeerIp = best.localIp,
                OpponentScale = best.scale,
                StatusMessage = $"对手规模 {best.scale} · 请加载对应配置保存槽并改为规模 {best.scale} 后重新匹配",
            };
        }

        var hostIp = PickHostIp(localIp, best.localIp);
        return new SkirmishMatchSnapshot
        {
            Phase = SkirmishMatchPhase.Ready,
            PeerIp = best.localIp,
            OpponentScale = best.scale,
            HostIp = hostIp,
            IsLocalHost = string.Equals(hostIp, localIp, StringComparison.Ordinal),
            StatusMessage = string.Equals(hostIp, localIp, StringComparison.Ordinal)
                ? "匹配成功 · 本机为房主"
                : $"匹配成功 · 连接房主 {hostIp}",
        };
    }

    /// <summary>双方客户端对同一 IP 对得到相同 host（伪随机）。</summary>
    public static string PickHostIp(string ipA, string ipB)
    {
        var first = string.CompareOrdinal(ipA, ipB) <= 0 ? ipA : ipB;
        var second = string.Equals(first, ipA, StringComparison.Ordinal) ? ipB : ipA;
        var pair = first + "|" + second;
        return (StableHash(pair) & 1) == 0 ? first : second;
    }

    public static int StableHash(string text)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in text)
            {
                hash = (hash * 31) + ch;
            }

            return hash;
        }
    }
}
