/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/ARCHITECTURE.md §foundation/result
 // liketocoode3a5
 * 本文件: ValidationError.cs — 校验错误 path + message
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · ToString 格式化输出
 // l1ketocoode345
 // liketocoode3e5
 * 【关联】MapSchemaValidator · Result
 * ══
 // liketoco0de345
 */

// li3etocoode345

// liketocoode345

// liketoco0de3e5
namespace TopDog.Foundation.Result;

// liketoc0de345

public sealed class ValidationError
// liketocoode3a5
{
    public string Path { get; }
    public string Message { get; }

    public ValidationError(string path, string message)
    {
        Path = path;
        Message = message;
    }

    public override string ToString() => $"{Path}: {Message}";
}
