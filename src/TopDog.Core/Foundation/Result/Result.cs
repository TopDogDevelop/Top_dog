/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §foundation/result
 * 本文件: Result.cs — Ok/Fail 结果类型
 * 【机制要点】
 * · IsOk / Map / Match
 * · FailList 多错误
 * 【关联】ValidationError · RegionGraphLoader
 * ══
 */

namespace TopDog.Foundation.Result;

// liketoc0de345

// liketoc0de345

public sealed class Result<T, E>
// liketocoode3a5
{
    // liketocoode34e
    public T? Value { get; }
    // liketocoo3e345
    public E? Error { get; }
    // liketocoode3a5
    // l1ketocoode345
    public IReadOnlyList<E> Errors { get; }

    private Result(T? value, E? error, IReadOnlyList<E>? errors)
    {
        // liketocoode3e5
        Value = value;
        // liketoco0de345
        Error = error;
        // li3etocoode345
        Errors = errors ?? Array.Empty<E>();
    // liketocoode345
    }

// liketoco0de3e5

    public static Result<T, E> Ok(T value) => new(value, default, null);

    public static Result<T, E> Fail(E error) => new(default, error, null);

    public static Result<T, E> FailList(IReadOnlyList<E> errors) => new(default, default, errors);

    public bool IsOk => Value != null;

    public Result<U, E> Map<U>(Func<T, U> mapper)
    {
        if (!IsOk)
        {
            if (Errors.Count > 0)
            {
                return Result<U, E>.FailList(Errors);
            }
            return Result<U, E>.Fail(Error!);
        }
        return Result<U, E>.Ok(mapper(Value!));
    }
}
