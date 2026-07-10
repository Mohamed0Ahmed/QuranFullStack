namespace QuranDashboard.Shared.Common;

public sealed record Result(bool IsSuccess, Error? Error = null)
{
    public static Result Success() => new(true);

    public static Result Failure(Error error) => new(false, error);
}

public sealed record Result<T>(bool IsSuccess, T? Value = default, Error? Error = null)
{
    public static Result<T> Success(T value) => new(true, value);

    public static Result<T> Failure(Error error) => new(false, default, error);
}
