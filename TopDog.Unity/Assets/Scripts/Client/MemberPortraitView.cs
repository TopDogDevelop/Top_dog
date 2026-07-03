using System;
using System.Collections.Generic;
using System.IO;
using TopDog.Content.Members;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Loads member portrait images; artwork scales to the full card/backdrop bounds.</summary>
public static class MemberPortraitView
{
    public enum PortraitPresentation
    {
        Compact,
        Codex,
    }

    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    public static void InvalidateCache()
    {
        foreach (var tex in TextureCache.Values)
        {
            if (tex != null)
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        TextureCache.Clear();
    }

    public static VisualElement Create(
        MemberState member,
        float sizePx,
        int fallbackIndex = 0,
        PortraitPresentation presentation = PortraitPresentation.Compact)
    {
        var host = new VisualElement();
        host.AddToClassList("ops-member-portrait");
        host.AddToClassList(presentation == PortraitPresentation.Codex
            ? "ops-member-portrait-codex"
            : "ops-member-portrait-compact");
        host.style.width = sizePx;
        host.style.height = sizePx;
        host.style.flexShrink = 0;
        host.style.overflow = Overflow.Hidden;
        host.style.position = Position.Relative;

        var backdrop = new VisualElement();
        backdrop.AddToClassList("ops-member-portrait-backdrop");
        backdrop.style.position = Position.Absolute;
        backdrop.style.left = 0;
        backdrop.style.right = 0;
        backdrop.style.top = 0;
        backdrop.style.bottom = 0;
        ApplyBackdropStyle(backdrop, member, fallbackIndex);
        host.Add(backdrop);

        var artLayer = new VisualElement();
        artLayer.AddToClassList("ops-member-portrait-art");
        artLayer.style.position = Position.Absolute;
        artLayer.style.left = 0;
        artLayer.style.right = 0;
        artLayer.style.top = 0;
        artLayer.style.bottom = 0;
        artLayer.style.overflow = Overflow.Hidden;
        host.Add(artLayer);

        var path = MemberPortraitCatalog.Resolve(member);
        if (path != null && TryGetTexture(path, out var texture))
        {
            var image = new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleAndCrop,
            };
            image.AddToClassList("ops-member-portrait-image");
            image.style.width = Length.Percent(100);
            image.style.height = Length.Percent(100);
            image.pickingMode = PickingMode.Ignore;
            artLayer.Add(image);
            return host;
        }

        var placeholder = new Label(Abbreviate(DisplayName(member)));
        placeholder.AddToClassList("ops-member-portrait-fallback");
        placeholder.pickingMode = PickingMode.Ignore;
        artLayer.style.backgroundColor = new Color(0f, 0f, 0f, 0.22f);
        artLayer.style.unityTextAlign = TextAnchor.MiddleCenter;
        artLayer.style.justifyContent = Justify.Center;
        artLayer.style.alignItems = Align.Center;
        artLayer.Add(placeholder);
        return host;
    }

    private static bool TryGetTexture(string path, out Texture2D? texture)
    {
        if (TextureCache.TryGetValue(path, out texture) && texture != null)
        {
            return true;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(tex);
                texture = null;
                return false;
            }

            tex.wrapMode = TextureWrapMode.Clamp;
            TextureCache[path] = tex;
            texture = tex;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MemberPortraitView: failed to load {path}: {ex.Message}");
            texture = null;
            return false;
        }
    }

    private static void ApplyBackdropStyle(VisualElement host, MemberState member, int fallbackIndex)
    {
        host.style.backgroundColor = BackdropColor(member.cardBackdrop, member.proceduralPortraitSeed, fallbackIndex);
    }

    private static Color BackdropColor(string? backdrop, int? seed, int fallbackIndex)
    {
        return backdrop switch
        {
            "战士" => new Color(0.62f, 0.28f, 0.22f, 1f),
            "射手" => new Color(0.28f, 0.48f, 0.58f, 1f),
            "法师" => new Color(0.38f, 0.28f, 0.62f, 1f),
            "刺客" => new Color(0.22f, 0.38f, 0.32f, 1f),
            _ => SeedTint(seed ?? fallbackIndex),
        };
    }

    private static Color SeedTint(int seed)
    {
        var hue = (seed & 0xFFFF) / 65535f;
        var c = Color.HSVToRGB(hue, 0.35f, 0.55f);
        c.a = 1f;
        return c;
    }

    private static string DisplayName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "?";

    private static string Abbreviate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        return name.Length <= 2 ? name : name[..2];
    }
}
