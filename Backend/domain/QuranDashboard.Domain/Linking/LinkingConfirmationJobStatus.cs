namespace QuranDashboard.Domain.Linking;

public enum LinkingConfirmationJobStatus
{
    Queued = 1,
    Running = 2,
    Finalizing = 3,
    Succeeded = 4,
    Stale = 5,
    Failed = 6,
    Cancelled = 7,
}

public enum LinkingConfirmationJobStage
{
    LoadingPrepared = 1,
    ApplyingUnitDiff = 2,
    SynchronizingDoor = 3,
    Committing = 4,
}

public enum LinkingConfirmationJobFailureCode
{
    LinkingDataStale = 1,
    PreflightBlocked = 2,
    PreflightStale = 3,
    ConfirmationCancelled = 4,
    ConfirmationFailed = 5,
    DoorNotFound = 6,
    IdempotencyConflict = 7,
}

public static class LinkingConfirmationJobLifecycleTokens
{
    public static IReadOnlyList<string> StatusTokens { get; } =
    [
        "queued",
        "running",
        "finalizing",
        "succeeded",
        "stale",
        "failed",
        "cancelled",
    ];

    public static IReadOnlyList<string> StageTokens { get; } =
    [
        "loading-prepared",
        "applying-unit-diff",
        "synchronizing-door",
        "committing",
    ];

    public static IReadOnlyList<string> FailureCodeTokens { get; } =
    [
        "LINKING_DATA_STALE",
        "PREFLIGHT_BLOCKED",
        "PREFLIGHT_STALE",
        "CONFIRMATION_CANCELLED",
        "CONFIRMATION_FAILED",
        "DOOR_NOT_FOUND",
        "IDEMPOTENCY_CONFLICT",
    ];

    public static string ToToken(LinkingConfirmationJobStatus status) => StatusTokens[(int)status - 1];

    public static string ToToken(LinkingConfirmationJobStage stage) => StageTokens[(int)stage - 1];

    public static string? ToToken(LinkingConfirmationJobFailureCode? failureCode) =>
        failureCode is null ? null : FailureCodeTokens[(int)failureCode.Value - 1];

    public static LinkingConfirmationJobStatus ParseStatus(string token) =>
        Parse(token, StatusTokens, value => (LinkingConfirmationJobStatus)(value + 1), "status");

    public static LinkingConfirmationJobStage ParseStage(string token) =>
        Parse(token, StageTokens, value => (LinkingConfirmationJobStage)(value + 1), "stage");

    public static LinkingConfirmationJobFailureCode? ParseFailureCode(string? token) =>
        token is null
            ? null
            : Parse(
                token,
                FailureCodeTokens,
                value => (LinkingConfirmationJobFailureCode)(value + 1),
                "failure code");

    private static T Parse<T>(
        string token,
        IReadOnlyList<string> tokens,
        Func<int, T> convert,
        string kind)
    {
        for (var index = 0; index < tokens.Count; index++)
        {
            if (string.Equals(tokens[index], token, StringComparison.Ordinal))
            {
                return convert(index);
            }
        }

        throw new ArgumentOutOfRangeException(nameof(token), token, $"Unknown linking confirmation job {kind}.");
    }
}
