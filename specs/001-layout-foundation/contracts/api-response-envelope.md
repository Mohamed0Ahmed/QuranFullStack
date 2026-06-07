# Contract: API Response Envelope

Every API response in this phase uses this envelope. Property names are English (camelCase on
the wire); `message` is Arabic by default.

## Success

```json
{
  "isSuccess": true,
  "message": "تمت العملية بنجاح",
  "data": { }
}
```

- `data` holds the endpoint payload.
- `errors` is omitted (or `null`) on success.

## Failure

```json
{
  "isSuccess": false,
  "message": "حدث خطأ غير متوقع",
  "errors": []
}
```

- `data` is omitted (or `null`) on failure.
- `errors` is a (possibly empty) list of strings. It MUST NOT contain stack traces, file paths,
  SQL, or connection details.

## Backend type (C#)

`Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs`:

```csharp
public sealed record ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string? message = null) => new()
    {
        IsSuccess = true, Data = data, Message = message
    };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) => new()
    {
        IsSuccess = false, Message = message, Errors = errors ?? []
    };
}
```

> Default JSON serialization is camelCase, so `IsSuccess` → `isSuccess`. Do not add a custom
> naming policy that would change this.

## Frontend type (TypeScript)

`Frontend/quran-dashboard-ui/src/app/core/data-access/api-response.model.ts`:

```ts
export interface ApiResponse<T> {
  isSuccess: boolean;
  message: string | null;
  data?: T | null;
  errors?: string[] | null;
}
```

## Rules

- HTTP status codes follow `API_GUIDELINES.md` §4 (200 success, 500 from the global handler, etc.).
- The global exception handler returns the **failure** envelope with HTTP 500 (not RFC7807
  ProblemDetails).
