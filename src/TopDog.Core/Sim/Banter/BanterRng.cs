namespace TopDog.Sim.Banter;

/// <summary>伴聊随机种子：战役级基底 + 每轮盐，避免固定 seed 与重复掷骰。</summary>
public static class BanterRng
{
    public static int DeriveCampaignSeed(int memberCount, int storyRound, string? legionId)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + memberCount;
            hash = hash * 31 + storyRound;
            hash = hash * 31 + (legionId?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = hash * 31 + Environment.TickCount;
            return hash == 0 ? 1 : hash;
        }
    }

    public static Random ForIdleRound(Random campaignRng, MemberBanterRuntimeState rt, float roundStartSec)
    {
        var salt = HashCode.Combine(
            campaignRng.Next(),
            rt.banterRoundSalt,
            rt.idleGroupId,
            (int)(roundStartSec * 1000f));
        return new Random(salt == 0 ? 1 : salt);
    }
}
