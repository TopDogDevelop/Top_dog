using System;

using System.Collections.Generic;

using System.IO;

using UnityEngine;



namespace TopDog.Client.Tactical;



/// <summary>

/// Second Galaxy main-universe skybox textures for realtime combat viewport.

/// Reserve sets live under Art/CombatBackgrounds/Reserve (not used for random pick).

/// </summary>

public static class CombatBackgroundCatalog

{

    private const string EquirectFile = "equirect.png";

    private const string LegacyPanoramaFile = "panorama.png";



    private static readonly string[] FaceSuffixes = { "+X", "+Y", "+Z", "-X", "-Y", "-Z" };

    private static readonly CubemapFace[] FaceOrder =

    {

        CubemapFace.PositiveX,

        CubemapFace.PositiveY,

        CubemapFace.PositiveZ,

        CubemapFace.NegativeX,

        CubemapFace.NegativeY,

        CubemapFace.NegativeZ,

    };



    public static readonly string[] MainSetIds =

    {

        "U_Skybox_01",

        "O_Skybox_01",

        "R_Skybox_01",

        "S_Skybox_01",

        "N_Skybox_Arothe01",

    };



    public static readonly string[] ReserveSetIds =

    {

        "Wormhole_Perel",

        "ProjectXSkyBox",

        "Nebula_NuminousGlow",

    };



    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, Cubemap> CubemapCache = new(StringComparer.Ordinal);

    private static readonly System.Random Rng = new();



    public static string PickRandomMainSetId()

    {

        return MainSetIds[Rng.Next(MainSetIds.Length)];

    }



    public static Cubemap? LoadCubemap(string setId, bool mainPoolOnly = true)

    {

        if (string.IsNullOrEmpty(setId))

        {

            return null;

        }



        if (CubemapCache.TryGetValue(setId, out var cached))

        {

            return cached;

        }



        var setDir = ResolveSetDirectory(setId, mainPoolOnly);

        if (setDir == null)

        {

            return null;

        }



        var facePaths = ResolveFacePaths(setDir);

        if (facePaths == null)

        {

            Debug.LogWarning("TopDog: combat cubemap faces missing in " + setDir);

            return null;

        }



        var faceSize = ReadFaceSize(facePaths[0]);

        if (faceSize <= 0)

        {

            return null;

        }



        var cubemap = new Cubemap(faceSize, TextureFormat.RGBA32, false);

        for (var i = 0; i < FaceOrder.Length; i++)

        {

            var faceTex = LoadFaceTexture(facePaths[i]);

            if (faceTex == null)

            {

                UnityEngine.Object.Destroy(cubemap);

                return null;

            }



            cubemap.SetPixels(faceTex.GetPixels(), FaceOrder[i]);

            UnityEngine.Object.Destroy(faceTex);

        }



        cubemap.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        cubemap.filterMode = FilterMode.Bilinear;

        CubemapCache[setId] = cubemap;

        return cubemap;

    }



    public static Texture2D? LoadPanorama(string setId, bool mainPoolOnly = true)

    {

        if (string.IsNullOrEmpty(setId))

        {

            return null;

        }



        if (TextureCache.TryGetValue(setId, out var cached))

        {

            return cached;

        }



        var setDir = ResolveSetDirectory(setId, mainPoolOnly);

        if (setDir == null)

        {

            return null;

        }



        var path = Path.Combine(setDir, EquirectFile);

        if (!File.Exists(path))

        {

            path = Path.Combine(setDir, LegacyPanoramaFile);

        }



        if (!File.Exists(path))

        {

            Debug.LogWarning("TopDog: combat background missing in " + setDir);

            return null;

        }



        var bytes = File.ReadAllBytes(path);

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!tex.LoadImage(bytes))

        {

            UnityEngine.Object.Destroy(tex);

            return null;

        }



        tex.filterMode = FilterMode.Bilinear;

        tex.wrapMode = TextureWrapMode.Repeat;

        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        TextureCache[setId] = tex;

        return tex;

    }



    public static bool IsMainSet(string setId) =>

        Array.IndexOf(MainSetIds, setId) >= 0;



    private static string? ResolveSetDirectory(string setId, bool mainPoolOnly)

    {

        var pool = mainPoolOnly ? "Main" : ResolvePool(setId);

        if (pool == null)

        {

            return null;

        }



        var setDir = Path.Combine(Application.dataPath, "Art", "CombatBackgrounds", pool, setId);

        return Directory.Exists(setDir) ? setDir : null;

    }



    private static string? ResolvePool(string setId)

    {

        if (Array.IndexOf(MainSetIds, setId) >= 0)

        {

            return "Main";

        }



        if (Array.IndexOf(ReserveSetIds, setId) >= 0)

        {

            return "Reserve";

        }



        return null;

    }



    private static string[]? ResolveFacePaths(string setDir)

    {

        var paths = new string[FaceSuffixes.Length];

        for (var i = 0; i < FaceSuffixes.Length; i++)

        {

            var suffix = FaceSuffixes[i];

            string? match = null;

            foreach (var file in Directory.GetFiles(setDir, "*" + suffix + ".png"))

            {

                var name = Path.GetFileName(file);

                if (name.Equals(EquirectFile, StringComparison.OrdinalIgnoreCase)

                    || name.Equals(LegacyPanoramaFile, StringComparison.OrdinalIgnoreCase))

                {

                    continue;

                }



                match = file;

                break;

            }



            if (match == null)

            {

                return null;

            }



            paths[i] = match;

        }



        return paths;

    }



    private static int ReadFaceSize(string path)

    {

        var bytes = File.ReadAllBytes(path);

        var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!probe.LoadImage(bytes))

        {

            UnityEngine.Object.Destroy(probe);

            return -1;

        }



        var size = probe.width;

        UnityEngine.Object.Destroy(probe);

        return size;

    }



    private static Texture2D? LoadFaceTexture(string path)

    {

        var bytes = File.ReadAllBytes(path);

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!tex.LoadImage(bytes))

        {

            UnityEngine.Object.Destroy(tex);

            return null;

        }



        tex.filterMode = FilterMode.Bilinear;

        tex.wrapMode = TextureWrapMode.Clamp;

        return tex;

    }

}


