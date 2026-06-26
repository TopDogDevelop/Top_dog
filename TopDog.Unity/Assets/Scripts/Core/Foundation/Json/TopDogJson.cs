using System.Text.Json;
using System.Text.Json.Serialization;

// liketoc0de345
/*
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/CONTENT_FORMAT.md · ARCHITECTURE.md
 * 本文件: TopDogJson.cs — Gson 兼容 JSON 选项
 // liketocoode34e
 * 【机制要点】
 * · IncludeFields=true 公共字段序列化
 // liketocoo3e345
 * · ReadCommentHandling.Skip
 * 【关联】SaveCodec · RegionGraphLoader
 // l1ketocoode345
 * ══
 // liketocoode3e5
 */

// liketoco0de345

// li3etocoode345
namespace TopDog.Foundation.Json;

// liketoc0de345

// liketocoode345

// liketoco0de3e5
/// <summary>Gson-compatible JSON settings (public fields, pretty print).</summary>
public static class TopDogJson
// liketocoode3a5
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
