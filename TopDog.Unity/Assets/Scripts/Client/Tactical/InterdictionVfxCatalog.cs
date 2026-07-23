using System.IO;
using TopDog.Client.OnlineUpdate;
using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/INTERDICTION_FIELD.md §4
 * 本文件: InterdictionVfxCatalog.cs — 拦截泡壳材质（蓝固定 / 红移动）
 * ══
 */

namespace TopDog.Client.Tactical;

public static class InterdictionVfxCatalog
{
    private static Texture2D? _fixedStripe;
    private static Texture2D? _mobileStripe;
    private static Material? _fixedMat;
    private static Material? _mobileMat;
    private static bool _loadAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeCaches()
    {
        _fixedStripe = null;
        _mobileStripe = null;
        _fixedMat = null;
        _mobileMat = null;
        _loadAttempted = false;
    }

    public static Material FixedMaterial =>
        _fixedMat ??= CreateShellMaterial(
            "InterdictionFixed",
            new Color(0.35f, 0.75f, 1f, 0.72f),
            new Color(0.7f, 0.95f, 1f, 0.95f),
            () =>
            {
                EnsureLoaded();
                return _fixedStripe;
            },
            seed: 11);

    public static Material MobileMaterial =>
        _mobileMat ??= CreateShellMaterial(
            "InterdictionMobile",
            new Color(1f, 0.28f, 0.32f, 0.72f),
            new Color(1f, 0.55f, 0.45f, 0.95f),
            () =>
            {
                EnsureLoaded();
                return _mobileStripe;
            },
            seed: 23);

    private static void EnsureLoaded()
    {
        if (_loadAttempted)
        {
            return;
        }

        _loadAttempted = true;
        _fixedStripe = TryLoad("fixed_stripe.png") ?? TryLoad("shell_stripe.png");
        _mobileStripe = TryLoad("mobile_stripe.png") ?? TryLoad("shell_stripe.png");
    }

    private static Material CreateShellMaterial(
        string name,
        Color tint,
        Color rim,
        System.Func<Texture2D?> stripe,
        int seed)
    {
        EnsureLoaded();
        var tile = stripe() ?? Texture2D.whiteTexture;
        var shader = Shader.Find("Hidden/TopDog/FieldAuraManual")
                     ?? Shader.Find("TopDog/FieldAuraSphere")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tile);
            mat.SetTextureScale("_MainTex", new Vector2(2f, 2f));
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

        return mat;
    }

    private static Texture2D? TryLoad(string fileName)
    {
        foreach (var dir in CandidateDirs())
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
                    name = "Interdiction_" + Path.GetFileNameWithoutExtension(fileName),
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                };
                if (!tex.LoadImage(bytes))
                {
                    Object.Destroy(tex);
                    continue;
                }

                tex.Apply(false, false);
                return tex;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TopDog interdiction-vfx: load failed " + path + " · " + e.Message);
            }
        }

        return null;
    }

    private static string[] CandidateDirs()
    {
        var list = new System.Collections.Generic.List<string>(4);
        try
        {
            list.Add(Path.Combine(OnlineUpdateClient.ContentRuntimeRoot, "content", "vfx", "interdiction"));
        }
        catch
        {
            // ignore
        }

        list.Add(Path.Combine(Application.streamingAssetsPath, "content", "vfx", "interdiction"));
#if UNITY_EDITOR
        list.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content", "vfx", "interdiction")));
        list.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "content", "vfx", "interdiction")));
#endif
        return list.ToArray();
    }
}
