using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_FX.md
 * 本文件: CombatFxCatalog.cs — 混伤弹道材质（调试：满亮度 / 满不透明）
 * 【参考】llamacademy/bullet-trails（MIT）TrailRenderer 用法；贴图自产
 * 【关联】CombatFxTracerPresenter
 * ══
 */

namespace TopDog.Client.Tactical;

public static class CombatFxCatalog
{
    private static Texture2D? _solidWhite;
    private static Material? _dotMat;
    private static Material? _trailMat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeCaches()
    {
        _solidWhite = null;
        _dotMat = null;
        _trailMat = null;
    }

    /// <summary>调试可见：满亮青 + 高 alpha；复用 FieldAuraSphere（ZTest Always）。</summary>
    public static Material DotMaterial => _dotMat ??= CreateSphereMaterial(
        new Color(0.15f, 1f, 1f, 1f),
        new Color(1f, 1f, 1f, 1f),
        "CombatFxHybridDot");

    public static Material TrailMaterial => _trailMat ??= CreateTrailMaterial(
        new Color(0.2f, 1f, 1f, 1f),
        "CombatFxHybridTrail");

    private static Material CreateSphereMaterial(Color tint, Color rim, string name)
    {
        var shader = Shader.Find("TopDog/FieldAuraSphere")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader) { name = name };
        var tex = SolidWhiteTexture;
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
        }

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", tint);
        }

        if (mat.HasProperty("_RimColor"))
        {
            mat.SetColor("_RimColor", rim);
        }

        if (mat.HasProperty("_RimPower"))
        {
            mat.SetFloat("_RimPower", 1.2f);
        }

        if (mat.HasProperty("_ScrollSpeed"))
        {
            mat.SetFloat("_ScrollSpeed", 0f);
        }

        mat.renderQueue = 3000;
        return mat;
    }

    private static Material CreateTrailMaterial(Color tint, string name)
    {
        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", SolidWhiteTexture);
        }

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", SolidWhiteTexture);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", tint);
        }

        mat.renderQueue = 3000;
        return mat;
    }

    private static Texture2D SolidWhiteTexture
    {
        get
        {
            if (_solidWhite != null)
            {
                return _solidWhite;
            }

            _solidWhite = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "CombatFxSolidWhite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            _solidWhite.SetPixels(pixels);
            _solidWhite.Apply(false, true);
            return _solidWhite;
        }
    }
}
