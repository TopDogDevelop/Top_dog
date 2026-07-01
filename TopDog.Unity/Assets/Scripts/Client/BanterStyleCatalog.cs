using TopDog.Content.Banter;
using TopDog.Foundation.Io;

namespace TopDog.Client;

/// <summary>伴聊字色/表情资源（Client 只读 content/banter）。</summary>
public static class BanterStyleCatalog
{
    private static BanterCatalog? _cached;

    public static BanterCatalog Instance => _cached ??= BanterCatalogLoader.LoadDefault();

    public static void Invalidate() => _cached = null;

    public static string? ResolveColorHex(int? colorId)
    {
        if (colorId == null)
        {
            return null;
        }

        return Instance.Colors.TryGetValue(colorId.Value, out var hex) ? NormalizeHex(hex) : null;
    }

    public static string? ResolveEmoteSpriteRef(int emoteId) =>
        Instance.EmoteSpriteRefs.TryGetValue(emoteId, out var sprite) ? sprite : null;

    private static string? NormalizeHex(string hex)
    {
        var t = hex.Trim().TrimStart('#');
        return t.Length == 0 ? null : "#" + t;
    }
}
