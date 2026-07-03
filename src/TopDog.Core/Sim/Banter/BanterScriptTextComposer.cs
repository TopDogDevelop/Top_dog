using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>剧本行成稿：先随机占位→具体文本，再专属覆盖；输出队列仅存最终台词。</summary>
public static class BanterScriptTextComposer
{
    public static string ComposeScriptLine(
        GameState state,
        MemberBanterRuntimeState rt,
        string catalogTemplate,
        Random rng) =>
        BanterDynamicTextResolver.Resolve(state, rt, catalogTemplate, rng);

    public static bool TryComposeIdleLine(
        GameState state,
        MemberBanterRuntimeState rt,
        MemberState member,
        string catalogTemplate,
        Random rng,
        out string finalText,
        out bool usedExclusive)
    {
        var scripted = BanterDynamicTextResolver.Resolve(state, rt, catalogTemplate, rng);
        return BanterPersonalExclusiveLines.TryResolveIdleEmitText(
            member,
            rt,
            scripted,
            rng,
            out finalText,
            out usedExclusive);
    }

    public static string ComposeReactiveLine(
        GameState state,
        MemberBanterRuntimeState rt,
        MemberState member,
        string catalogTemplate,
        Random rng)
    {
        var scripted = BanterDynamicTextResolver.Resolve(state, rt, catalogTemplate, rng);
        return BanterPersonalExclusiveLines.ResolveForReactive(member, rt, scripted, rng);
    }
}
