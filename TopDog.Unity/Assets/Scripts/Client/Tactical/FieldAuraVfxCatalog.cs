using System.IO;
using TopDog.Client.OnlineUpdate;
using TopDog.Foundation.Io;
using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §6 · OPEN_DESIGN D-VFX-01
 * 本文件: FieldAuraVfxCatalog.cs — 场域球壳材质（SG tile + 噪声）
 * 【机制要点】
 * · StreamingAssets/content/vfx/field_aura：shell_tile / noise / role noise（HF）
 * · TopDog/FieldAuraSphere：Fresnel 球壳 + 半透明噪声滚动
 * ══
 */

namespace TopDog.Client.Tactical;

public static class FieldAuraVfxCatalog
{
    private const int TexSize = 256;
    private static Texture2D? _shellTile;
    private static Texture2D? _shellStripe;
    private static Texture2D? _shieldStripe;
    private static Texture2D? _armorStripe;
    private static Texture2D? _noiseShared;
    private static Texture2D? _shieldNoise;
    private static Texture2D? _armorNoise;
    private static Texture2D? _shieldUitk;
    private static Texture2D? _armorUitk;
    private static Material? _shieldMat;
    private static Material? _armorMat;
    private static bool _sgLoadAttempted;

    public static readonly Color ShieldTint = new(0.75f, 0.9f, 1f, 1f);
    public static readonly Color ArmorTint = new(1f, 0.92f, 0.72f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeCaches()
    {
        _shellTile = null;
        _shellStripe = null;
        _shieldStripe = null;
        _armorStripe = null;
        _noiseShared = null;
        _shieldNoise = null;
        _armorNoise = null;
        _shieldUitk = null;
        _armorUitk = null;
        _shieldMat = null;
        _armorMat = null;
        _sgLoadAttempted = false;
    }

    public static Texture2D ShieldUitkTexture
    {
        get
        {
            EnsureSgLoaded();
            return _shieldUitk ??= BuildFieldTexture(seed: 11);
        }
    }

    public static Texture2D ArmorUitkTexture
    {
        get
        {
            EnsureSgLoaded();
            return _armorUitk ??= BuildFieldTexture(seed: 23);
        }
    }

    public static Material ShieldMaterial
    {
        get
        {
            return _shieldMat ??= CreateShellMaterial(
                name: "FieldAuraShield",
                tint: new Color(0.55f, 0.82f, 1f, 0.72f),
                rim: new Color(0.85f, 0.97f, 1f, 0.95f),
                noise: () => _shieldNoise ?? _noiseShared,
                seed: 11);
        }
    }

    public static Material ArmorMaterial
    {
        get
        {
            return _armorMat ??= CreateShellMaterial(
                name: "FieldAuraArmor",
                tint: new Color(1f, 0.9f, 0.5f, 0.72f),
                rim: new Color(1f, 0.96f, 0.75f, 0.95f),
                noise: () => _armorNoise ?? _noiseShared,
                seed: 23);
        }
    }

    private static void ApplyShellVisibility(Material mat, Color tint, Color rim)
    {
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }

        if (mat.HasProperty("_RimColor"))
        {
            mat.SetColor("_RimColor", rim);
        }

        if (mat.HasProperty("_ShellFace"))
        {
            mat.SetFloat("_ShellFace", 0.55f);
        }

        if (mat.HasProperty("_BackFaceBoost"))
        {
            mat.SetFloat("_BackFaceBoost", 1.25f);
        }
    }

    private static void EnsureSgLoaded()
    {
        if (_sgLoadAttempted)
        {
            return;
        }

        _sgLoadAttempted = true;
        // 用户 shell_stripe：TR–BL 对角切开 → 蓝紫盾 / 金黄甲
        _shellStripe = TryLoadSgTexture("shell_stripe.png", wrap: TextureWrapMode.Repeat);
        _shieldStripe = TryLoadSgTexture("shield_stripe.png", wrap: TextureWrapMode.Repeat)
                        ?? _shellStripe;
        _armorStripe = TryLoadSgTexture("armor_stripe.png", wrap: TextureWrapMode.Repeat)
                       ?? _shellStripe;
        _shellTile = _shellStripe
                     ?? TryLoadSgTexture("shell_tile.png", wrap: TextureWrapMode.Repeat);
        _noiseShared = TryLoadSgTexture("noise.png", wrap: TextureWrapMode.Repeat);
        _shieldNoise = TryLoadSgTexture("shield_noise.png", wrap: TextureWrapMode.Repeat)
                       ?? _noiseShared;
        _armorNoise = TryLoadSgTexture("armor_noise.png", wrap: TextureWrapMode.Repeat)
                      ?? _noiseShared;
        _shieldUitk = BuildShellDiscUitk(_shieldStripe ?? _shieldNoise, new Color(0.55f, 0.82f, 1f, 1f), seed: 11)
                      ?? TryLoadSgTexture("shield_main.png", wrap: TextureWrapMode.Clamp);
        _armorUitk = BuildShellDiscUitk(_armorStripe ?? _armorNoise, new Color(1f, 0.9f, 0.5f, 1f), seed: 23)
                     ?? TryLoadSgTexture("armor_main.png", wrap: TextureWrapMode.Clamp);

        Debug.Log(
            "TopDog field-aura-vfx: SG shell "
            + $"shieldStripe={(_shieldStripe != null)} armorStripe={(_armorStripe != null)} "
            + $"uitkS={(_shieldUitk != null)} uitkA={(_armorUitk != null)}");
        // #region agent log
        CombatFxAgentLog.Write(
            "F",
            "FieldAuraVfxCatalog.EnsureSgLoaded",
            "stripe-load",
            "{\"shieldStripe\":" + (_shieldStripe != null ? "true" : "false")
            + ",\"armorStripe\":" + (_armorStripe != null ? "true" : "false")
            + ",\"w\":" + (_shellStripe != null ? _shellStripe.width : 0)
            + ",\"h\":" + (_shellStripe != null ? _shellStripe.height : 0) + "}");
        // #endregion
    }

    /// <summary>俯视球壳：外缘亮环 + SG 噪声半透明体内。</summary>
    private static Texture2D? BuildShellDiscUitk(Texture2D? noiseSrc, Color tint, int seed)
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = seed == 11 ? "FieldAuraShieldShellUitk" : "FieldAuraArmorShellUitk",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[size * size];
        Color32[]? noisePx = null;
        var nw = 1;
        var nh = 1;
        if (noiseSrc != null && noiseSrc.isReadable)
        {
            try
            {
                noisePx = noiseSrc.GetPixels32();
                nw = noiseSrc.width;
                nh = noiseSrc.height;
            }
            catch
            {
                noisePx = null;
            }
        }

        // PNG LoadImage 后默认 readable；若不可读则采样失败用程序噪声
        var rng = new System.Random(seed);
        var cx = (size - 1) * 0.5f;
        var cy = (size - 1) * 0.5f;
        var rad = size * 0.48f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - cx) / rad;
                var dy = (y - cy) / rad;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1.02f)
                {
                    pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                // 空心壳环：仅 rim（俯视等价球壳外缘；中心全透避免实心盘）
                var rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) * 9f);
                if (rim < 0.02f)
                {
                    pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                float n = 0.55f;
                if (noisePx != null && nw > 0 && nh > 0)
                {
                    var u = ((x * nw) / size) % nw;
                    var v = ((y * nh) / size) % nh;
                    if (u < 0)
                    {
                        u += nw;
                    }

                    if (v < 0)
                    {
                        v += nh;
                    }

                    var c = noisePx[v * nw + u];
                    n = (c.r + c.g + c.b) / (3f * 255f);
                    if (c.a > 12)
                    {
                        n = Mathf.Lerp(n, c.a / 255f, 0.5f);
                    }
                }
                else
                {
                    n = 0.35f + 0.65f * (float)rng.NextDouble();
                }

                var a = Mathf.Clamp01(rim * (0.55f + 0.45f * n));
                var rgb = tint * (0.55f + 0.45f * n);
                rgb = Color.Lerp(rgb, Color.white, rim * 0.35f);
                pixels[y * size + x] = new Color32(
                    (byte)(Mathf.Clamp01(rgb.r) * 255f),
                    (byte)(Mathf.Clamp01(rgb.g) * 255f),
                    (byte)(Mathf.Clamp01(rgb.b) * 255f),
                    (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Material CreateShellMaterial(
        string name,
        Color tint,
        Color rim,
        System.Func<Texture2D?> noise,
        int seed)
    {
        EnsureSgLoaded();
        var tile = (seed == 11 ? _shieldStripe : _armorStripe)
                   ?? _shellStripe
                   ?? _shellTile
                   ?? (seed == 11 ? _shieldUitk : _armorUitk)
                   ?? BuildFieldTexture(seed);
        var noiseTex = noise() ?? BuildNoiseFallback(seed);
        // Built-in CG 优先：CommandBuffer / 手动 RT 能画；URP HLSL 作编辑器预览回退
        var shader = Shader.Find("Hidden/TopDog/FieldAuraManual")
                     ?? Shader.Find("TopDog/FieldAuraSphere")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tile);
            mat.SetTextureScale("_MainTex", new Vector2(2f, 2f));
        }

        if (mat.HasProperty("_NoiseTex"))
        {
            mat.SetTexture("_NoiseTex", noiseTex);
            mat.SetTextureScale("_NoiseTex", new Vector2(2.5f, 2.5f));
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }

        if (mat.HasProperty("_RimColor"))
        {
            mat.SetColor("_RimColor", rim);
        }

        if (mat.HasProperty("_RimPower"))
        {
            mat.SetFloat("_RimPower", 1.55f);
        }

        if (mat.HasProperty("_ShellFace"))
        {
            mat.SetFloat("_ShellFace", 0.55f);
        }

        if (mat.HasProperty("_BackFaceBoost"))
        {
            mat.SetFloat("_BackFaceBoost", 1.25f);
        }

        if (mat.HasProperty("_StripeStrength"))
        {
            mat.SetFloat("_StripeStrength", 1f);
        }

        if (mat.HasProperty("_Contrast"))
        {
            mat.SetFloat("_Contrast", 10f);
        }

        if (mat.HasProperty("_Scroll"))
        {
            mat.SetFloat("_Scroll", seed == 11 ? 0.045f : 0.035f);
        }

        // legacy props if URP fallback
        if (mat.HasProperty("_ShellFill"))
        {
            mat.SetFloat("_ShellFill", 0f);
        }

        if (mat.HasProperty("_NoiseStrength"))
        {
            mat.SetFloat("_NoiseStrength", 0f);
        }

        return mat;
    }

    private static Texture2D? TryLoadSgTexture(string fileName, TextureWrapMode wrap)
    {
        foreach (var dir in CandidateFieldAuraDirs())
        {
            var path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "FieldAura_" + Path.GetFileNameWithoutExtension(fileName),
                    wrapMode = wrap,
                    filterMode = FilterMode.Bilinear,
                };
                if (!tex.LoadImage(bytes))
                {
                    Object.Destroy(tex);
                    continue;
                }

                tex.Apply(false, false);
                // 保持 readable：UITK 球壳合成要 GetPixels32
                return tex;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TopDog field-aura-vfx: load failed " + path + " · " + e.Message);
            }
        }

        return null;
    }

    private static string[] CandidateFieldAuraDirs()
    {
        var list = new System.Collections.Generic.List<string>(4);
        try
        {
            list.Add(Path.Combine(
                OnlineUpdateClient.ContentRuntimeRoot,
                "content",
                "vfx",
                "field_aura"));
        }
        catch
        {
            // ignored
        }

        try
        {
            var root = AppRoot.Find();
            if (!string.IsNullOrEmpty(root))
            {
                list.Add(Path.Combine(root, "content", "vfx", "field_aura"));
            }
        }
        catch
        {
            // ignored
        }

#if UNITY_EDITOR
        list.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content", "vfx", "field_aura")));
#endif
        return list.ToArray();
    }

    private static Texture2D BuildNoiseFallback(int seed)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            name = "FieldAuraNoiseFallback",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };
        var rng = new System.Random(seed + 99);
        var pixels = new Color32[TexSize * TexSize];
        for (var i = 0; i < pixels.Length; i++)
        {
            var v = (byte)rng.Next(40, 220);
            pixels[i] = new Color32(v, v, v, v);
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D BuildFieldTexture(int seed)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            name = seed == 11 ? "FieldAuraShieldTex" : "FieldAuraArmorTex",
            wrapMode = TextureWrapMode.Clamp,
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
                if (r > 1f)
                {
                    a = 0f;
                }

                pixels[y * TexSize + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }
}
