using TopDog.Net.Lan;



namespace TopDog.Client.OnlineUpdate;



/// <summary>HF Bucket resolve base URL and path constants for content hot-update.</summary>

public static class OnlineUpdateConfig

{

    /// <summary>

    /// Hugging Face public bucket resolve root (version.json / manifest.json / content/**).

    /// Bucket page: https://huggingface.co/buckets/liketocode789/topdog_online_update_data

    /// GitHub pointer repo (human redirect only): https://github.com/TopDogDevelop/topdog_online_update

    /// </summary>

    public const string DefaultBaseUrl =

        "https://huggingface.co/buckets/liketocode789/topdog_online_update_data/resolve/";



    public const string HfBucketPage =

        "https://huggingface.co/buckets/liketocode789/topdog_online_update_data";



    public const string GithubPointerRawUrl =

        "https://raw.githubusercontent.com/TopDogDevelop/topdog_online_update/main/";



    public const string VersionFile = "version.json";

    public const string ManifestFile = "manifest.json";

    public const string AppliedVersionFile = "applied_version.json";

    public const string OnlineUpdateDirName = "online_update";

    public const string ContentRuntimeDirName = "content_runtime";



    public static string BaseUrl { get; set; } = DefaultBaseUrl;



    public static string VersionUrl => Combine(BaseUrl, VersionFile);

    public static string ManifestUrl => Combine(BaseUrl, ManifestFile);



    public static void ApplyRemoteBaseUrl(string? remoteBaseUrl)

    {

        if (string.IsNullOrWhiteSpace(remoteBaseUrl))

        {

            return;

        }



        BaseUrl = remoteBaseUrl.Trim();

    }



    public static string Combine(string baseUrl, string relative)

    {

        if (string.IsNullOrWhiteSpace(baseUrl))

        {

            baseUrl = DefaultBaseUrl;

        }



        if (!baseUrl.EndsWith('/'))

        {

            baseUrl += "/";

        }



        return baseUrl + relative.TrimStart('/');

    }



    public static string FallbackLocalVersion => ContentVersionGate.Baseline;

}


