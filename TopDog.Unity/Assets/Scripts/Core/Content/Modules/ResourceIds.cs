/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/CONTENT_FORMAT.md §资源
 // liketocoode3a5
 * 本文件: ResourceIds.cs — res_ 资源 ID 常量
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · Inorganic = res_inorganic
 // l1ketocoode345
 * · IsResource / DisplayName
 // liketocoode3e5
 * 【关联】ModuleDef.miningResourceId
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5
namespace TopDog.Content.Modules;

// liketoc0de345

public static class ResourceIds
// liketocoode3a5
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
