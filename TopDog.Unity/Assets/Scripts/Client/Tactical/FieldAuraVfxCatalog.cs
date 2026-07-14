using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §6 · OPEN_DESIGN D-VFX-01
 * 本文件: FieldAuraVfxCatalog.cs — 场域材质（Demo 可见；主工程叠层 ⏳ 顽固）
 * ══
 */

namespace TopDog.Client.Tactical;

public static class FieldAuraVfxCatalog
{
    private const int TexSize = 256;
    private static Texture2D? _shieldTex;
    private static Texture2D? _armorTex;
    private static Material? _shieldMat;
    private static Material? _armorMat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeCaches()
    {
        _shieldTex = null;
        _armorTex = null;
        _shieldMat = null;
        _armorMat = null;
    }

    public static Material ShieldMaterial => _shieldMat ??= CreateMaterial(
        ref _shieldTex,
        new Color(0.35f, 0.72f, 1f, 0.55f),
        new Color(0.65f, 0.9f, 1f, 0.75f),
        seed: 11);

    public static Material ArmorMaterial => _armorMat ??= CreateMaterial(
        ref _armorTex,
        new Color(1f, 0.82f, 0.35f, 0.5f),
        new Color(1f, 0.92f, 0.55f, 0.7f),
        seed: 23);

    private static Material CreateMaterial(
        ref Texture2D? tex,
        Color tint,
        Color rim,
        int seed)
    {
        tex ??= BuildFieldTexture(seed);
        var shader = Shader.Find("TopDog/FieldAuraSphere")
                     ?? Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader)
        {
            name = seed == 11 ? "FieldAuraShield" : "FieldAuraArmor",
        };
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }

        if (mat.HasProperty("_RimColor"))
        {
            mat.SetColor("_RimColor", rim);
        }

        return mat;
    }

    private static Texture2D BuildFieldTexture(int seed)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            name = seed == 11 ? "FieldAuraShieldTex" : "FieldAuraArmorTex",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };
        var rng = new System.Random(seed);
        var cx = TexSize * 0.5f;
        var cy = TexSize * 0.5f;
        var pixels = new Color32[TexSize * TexSize];
        for (var y = 0; y < TexSize; y++)
        {
            for (var x = 0; x < TexSize; x++)
            {
                var dx = (x - cx) / cx;
                var dy = (y - cy) / cy;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.72f) * 6f);
                var noise = (float)rng.NextDouble() * 0.18f;
                var scan = 0.5f + 0.5f * Mathf.Sin((x + y) * 0.09f + seed);
                var a = Mathf.Clamp01((1f - r) * 0.55f + ring * 0.45f + noise) * scan;
                pixels[y * TexSize + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }
}
