using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

[CreateAssetMenu(fileName = "UiArtSkin", menuName = "TopDog/UI Art Skin")]
public sealed class UiArtSkinAsset : ScriptableObject, IUiArtSkin
{
    [SerializeField] private string skinId = "default";
    [SerializeField] private StyleSheet? globalStyleOverride;
    [SerializeField] private Font? primaryFont;
    [SerializeField] private UiScreenArtSet[] screens = Array.Empty<UiScreenArtSet>();

    private Dictionary<(UiScreenId, string), Texture2D>? _textureLookup;
    private Dictionary<(UiScreenId, string), Sprite>? _spriteLookup;
    private Dictionary<UiScreenId, StyleSheet>? _screenStyleLookup;

    public string SkinId => skinId;

    public Texture2D? GetTexture(UiScreenId screen, string slotId)
    {
        EnsureLookup();
        return _textureLookup!.TryGetValue((screen, slotId), out var tex) ? tex : null;
    }

    public Sprite? GetSprite(UiScreenId screen, string slotId)
    {
        EnsureLookup();
        return _spriteLookup!.TryGetValue((screen, slotId), out var sp) ? sp : null;
    }

    public StyleSheet? GetGlobalStyleOverride() => globalStyleOverride;

    public StyleSheet? GetScreenStyleOverride(UiScreenId screen)
    {
        EnsureLookup();
        return _screenStyleLookup!.TryGetValue(screen, out var sheet) ? sheet : null;
    }

    public Font? GetFont(string fontId) =>
        fontId == "primary" || fontId == "default" ? primaryFont : null;

    private void EnsureLookup()
    {
        if (_textureLookup != null)
        {
            return;
        }

        _textureLookup = new Dictionary<(UiScreenId, string), Texture2D>();
        _spriteLookup = new Dictionary<(UiScreenId, string), Sprite>();
        _screenStyleLookup = new Dictionary<UiScreenId, StyleSheet>();
        foreach (var set in screens)
        {
            if (set.styleOverride != null)
            {
                _screenStyleLookup[set.screen] = set.styleOverride;
            }

            foreach (var entry in set.slots)
            {
                if (string.IsNullOrEmpty(entry.slotId))
                {
                    continue;
                }

                var key = (set.screen, entry.slotId);
                if (entry.texture != null)
                {
                    _textureLookup[key] = entry.texture;
                }

                if (entry.sprite != null)
                {
                    _spriteLookup[key] = entry.sprite;
                }
            }
        }
    }

    private void OnEnable() => InvalidateLookup();

    private void OnValidate() => InvalidateLookup();

    private void InvalidateLookup()
    {
        _textureLookup = null;
        _spriteLookup = null;
        _screenStyleLookup = null;
    }

    [Serializable]
    public sealed class UiArtSlotEntry
    {
        public string slotId = UiArtSlots.ScreenBg;
        public Texture2D? texture;
        public Sprite? sprite;
    }

    [Serializable]
    public sealed class UiScreenArtSet
    {
        public UiScreenId screen;
        public StyleSheet? styleOverride;
        public UiArtSlotEntry[] slots = Array.Empty<UiArtSlotEntry>();
    }
}
