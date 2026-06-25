using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Runtime-swappable UI art provider (default skin, DLC, mod).</summary>
public interface IUiArtSkin
{
    string SkinId { get; }

    Texture2D? GetTexture(UiScreenId screen, string slotId);

    Sprite? GetSprite(UiScreenId screen, string slotId);

    StyleSheet? GetGlobalStyleOverride();

    StyleSheet? GetScreenStyleOverride(UiScreenId screen);

    Font? GetFont(string fontId);
}
