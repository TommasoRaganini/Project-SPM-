namespace Domain.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(bool isSuccess, T? value, IEnumerable<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors.ToList().AsReadOnly();
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
    public static Result<T> Failure(string error) => new(false, default, new[] { error });
}

