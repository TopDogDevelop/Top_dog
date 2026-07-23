using UnityEngine;

/// <summary>默认场景：护盾融合立场（蓝）+ 装甲域场（金）两枚半透明球。</summary>
[ExecuteAlways]
public sealed class FieldAuraShowcase : MonoBehaviour
{
    [SerializeField] float shieldDiameter = 16f;
    [SerializeField] float armorDiameter = 16f;
    [SerializeField] float separation = 24f;

    bool _built;

    void OnEnable()
    {
        if (_built && transform.childCount >= 2)
        {
            ConfigureCamera();
            return;
        }

        ClearChildren();
        ConfigureCamera();
        BuildSphere(
            "ShieldFusionField",
            new Vector3(-separation * 0.5f, 0f, 0f),
            shieldDiameter,
            new Color(0.35f, 0.72f, 1f, 0.32f),
            new Color(0.65f, 0.9f, 1f, 0.6f),
            11);
        BuildSphere(
            "ArmorLinkField",
            new Vector3(separation * 0.5f, 0f, 0f),
            armorDiameter,
            new Color(1f, 0.82f, 0.35f, 0.28f),
            new Color(1f, 0.92f, 0.55f, 0.55f),
            23);
        _built = true;
    }

    void ClearChildren()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    static void ConfigureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        cam.transform.position = new Vector3(0f, 6f, -30f);
        cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
    }

    void BuildSphere(
        string label,
        Vector3 position,
        float diameter,
        Color tint,
        Color rim,
        int seed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = label;
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * diameter;

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        var shader = Shader.Find("TopDog/FieldAuraSphere");
        if (shader == null)
        {
            Debug.LogError("TopDog/FieldAuraSphere shader missing — assign URP in Project Settings.");
            return;
        }

        var mat = new Material(shader) { name = label + "Material" };
        mat.SetTexture("_MainTex", BuildFieldTexture(seed));
        mat.SetColor("_Color", tint);
        mat.SetColor("_RimColor", rim);

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static Texture2D BuildFieldTexture(int seed)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = seed == 11 ? "FieldAuraShieldTex" : "FieldAuraArmorTex",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };
        var rng = new System.Random(seed);
        var cx = size * 0.5f;
        var cy = size * 0.5f;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - cx) / cx;
                var dy = (y - cy) / cy;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.72f) * 6f);
                var noise = (float)rng.NextDouble() * 0.18f;
                var scan = 0.5f + 0.5f * Mathf.Sin((x + y) * 0.09f + seed);
                var a = Mathf.Clamp01((1f - r) * 0.55f + ring * 0.45f + noise) * scan;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }
}
