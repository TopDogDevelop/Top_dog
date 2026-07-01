namespace TopDog.Foundation.Io;

/// <summary>Unity 仓约战 overlay（libGDX 同步后回写 content/）。</summary>
public static class SkirmishContentOverlay
{
    public static string Dir(string subfolder) =>
        Path.Combine(AppRoot.Find(), "content", "skirmish_overlay", subfolder);
}
