namespace TopDog.Content.Modules;

public static class ResourceIds
{
    public const string Inorganic = "res_inorganic";

    public static bool IsResource(string? id) =>
        id != null && id.StartsWith("res_", StringComparison.Ordinal);

    public static string DisplayName(string? id) => id switch
    {
        Inorganic => "无机物",
        _ => id ?? "?",
    };
}
