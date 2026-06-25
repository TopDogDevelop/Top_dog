namespace TopDog.Foundation.Result;

public sealed class Result<T, E>
{
    public T? Value { get; }
    public E? Error { get; }
    public IReadOnlyList<E> Errors { get; }

    private Result(T? value, E? error, IReadOnlyList<E>? errors)
    {
        Value = value;
        Error = error;
        Errors = errors ?? Array.Empty<E>();
    }

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
