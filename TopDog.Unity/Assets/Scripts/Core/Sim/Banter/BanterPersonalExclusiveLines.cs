using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>个人专属闲聊台词：绵羊必出；狈头军师 50% 专属 / 50% CSV。</summary>
public static class BanterPersonalExclusiveLines
{
    public const int BeiTouExclusiveChancePercent = 50;
    public const string BeiTouJunShiName = "狈头军师";
    public const string BeiTouRecruitLine = "我要招点人。";

    public static bool HasSpokenExclusiveThisRound(MemberBanterRuntimeState rt, MemberState member) =>
        rt.idleMandatoryLineSpokenIdentities.Contains(IdentityCodes.Of(member));

    public static bool IsBeiTouJunShi(MemberState member) =>
        string.Equals(member.name, BeiTouJunShiName, StringComparison.Ordinal);

    public static bool CanUsePersonalExclusive(MemberState member) =>
        BanterSheepDuckPhrases.IsSheepIdentity(IdentityCodes.Of(member))
        || IsBeiTouJunShi(member);

    /// <summary>闲聊：尝试覆盖 CSV；返回是否实际输出了专属台词。</summary>
    public static bool TryResolveForIdle(
        MemberState member,
        MemberBanterRuntimeState rt,
        string catalogText,
        Random rng,
        out string resolvedText,
        out bool usedExclusive)
    {
        resolvedText = catalogText;
        usedExclusive = false;

        if (HasSpokenExclusiveThisRound(rt, member))
        {
            return false;
        }

        if (BanterSheepDuckPhrases.IsSheepIdentity(IdentityCodes.Of(member)))
        {
            resolvedText = BanterSheepDuckPhrases.DrawNext(rt.sheepDuckPhraseBag, rng);
            usedExclusive = true;
            return true;
        }

        if (IsBeiTouJunShi(member) && rng.Next(100) < BeiTouExclusiveChancePercent)
        {
            resolvedText = BeiTouRecruitLine;
            usedExclusive = true;
            return true;
        }

        return false;
    }

    public static string ResolveForReactive(MemberState member, MemberBanterRuntimeState rt, string catalogText, Random rng)
    {
        if (BanterSheepDuckPhrases.IsSheepIdentity(IdentityCodes.Of(member)))
        {
            return BanterSheepDuckPhrases.DrawNext(rt.sheepDuckPhraseBag, rng);
        }

        return catalogText;
    }
}
