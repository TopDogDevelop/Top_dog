using TopDog.Content.Banter;

namespace TopDog.Sim.Banter;

/// <summary>闲聊节奏：30s 一轮；组内下一条间隔 2s + 下条正文字数×0.2s（不含表情标记）。</summary>
public static class BanterIdleTiming
{
    public const float RoundGapSec = 30f;
    public const float MessageBaseGapSec = 2f;
    public const float CharDelaySec = 0.2f;
    /// <summary>多开跟读：各号分别输出同一句，号与号之间的间隔。</summary>
    public const float MultiboxEchoGapSec = 0.4f;

    public static int CountTextChars(string? catalogText) =>
        BanterInlineMarkupParser.StripMarkupForValidation(catalogText).Length;

    public static float GapBeforeNextMessage(string? nextCatalogText) =>
        MessageBaseGapSec + CountTextChars(nextCatalogText) * CharDelaySec;
}
