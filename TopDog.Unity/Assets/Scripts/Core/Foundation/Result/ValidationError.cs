namespace TopDog.Foundation.Result;

public sealed class ValidationError
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
