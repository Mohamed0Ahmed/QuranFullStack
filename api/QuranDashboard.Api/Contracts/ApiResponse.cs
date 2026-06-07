namespace QuranDashboard.Api.Contracts;

public sealed record ApiResponse<T>
{
    public bool IsSuccess { get; init; }

    public string? Message { get; init; }

    public T? Data { get; init; }

    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) => new()
    {
        IsSuccess = false,
        Message = message,
        Errors = errors ?? []
    };
}
