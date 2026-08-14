namespace QuranDashboard.Domain.Linking;

public enum LinkingPreparedPreflightStatus
{
    Queued = 1,
    Preparing = 2,
    Ready = 3,
    Stale = 4,
    Failed = 5,
    Cancelled = 6,
    Expired = 7,
    Confirmed = 8,
}

public enum LinkingPreparedPreflightStage
{
    Resolving = 1,
    Classifying = 2,
    Persisting = 3,
}

public enum LinkingPreparedPreflightFailureCode
{
    LinkingDataStale = 1,
    SourceViewStale = 2,
    WorkspaceSourceStale = 3,
    PreflightNotReady = 4,
    PreflightBlocked = 5,
    PreflightStale = 6,
    PreflightExpired = 7,
    PreflightCancelled = 8,
    PreparationFailed = 9,
    PreflightAlreadyConfirmed = 10,
    PreparationAbandoned = 11,
    ConfirmationCancelled = 12,
    ConfirmationFailed = 13,
    ActiveLinkingWorkflowLimit = 14,
    IdempotencyConflict = 15,
    CancellationTooLate = 16,
}

public static class LinkingPreparedPreflightLifecycleTokens
{
    public static IReadOnlyList<string> StatusTokens { get; } =
    [
        "queued",
        "preparing",
        "ready",
        "stale",
        "failed",
        "cancelled",
        "expired",
        "confirmed",
    ];

    public static IReadOnlyList<string> StageTokens { get; } =
    [
        "resolving",
        "classifying",
        "persisting",
    ];

    public static IReadOnlyList<string> FailureCodeTokens { get; } =
    [
        "LINKING_DATA_STALE",
        "SOURCE_VIEW_STALE",
        "WORKSPACE_SOURCE_STALE",
        "PREFLIGHT_NOT_READY",
        "PREFLIGHT_BLOCKED",
        "PREFLIGHT_STALE",
        "PREFLIGHT_EXPIRED",
        "PREFLIGHT_CANCELLED",
        "PREPARATION_FAILED",
        "PREFLIGHT_ALREADY_CONFIRMED",
        "PREPARATION_ABANDONED",
        "CONFIRMATION_CANCELLED",
        "CONFIRMATION_FAILED",
        "ACTIVE_LINKING_WORKFLOW_LIMIT",
        "IDEMPOTENCY_CONFLICT",
        "CANCELLATION_TOO_LATE",
    ];

    public static string ToToken(LinkingPreparedPreflightStatus status) =>
        StatusTokens[(int)status - 1];

    public static string ToToken(LinkingPreparedPreflightStage stage) =>
        StageTokens[(int)stage - 1];

    public static string? ToToken(LinkingPreparedPreflightFailureCode? failureCode) =>
        failureCode is null ? null : FailureCodeTokens[(int)failureCode.Value - 1];

    public static LinkingPreparedPreflightStatus ParseStatus(string token) =>
        Parse(token, StatusTokens, value => (LinkingPreparedPreflightStatus)(value + 1), "status");

    public static LinkingPreparedPreflightStage ParseStage(string token) =>
        Parse(token, StageTokens, value => (LinkingPreparedPreflightStage)(value + 1), "stage");

    public static LinkingPreparedPreflightFailureCode? ParseFailureCode(string? token) =>
        token is null
            ? null
            : Parse(
                token,
                FailureCodeTokens,
                value => (LinkingPreparedPreflightFailureCode)(value + 1),
                "failure code");

    private static T Parse<T>(
        string token,
        IReadOnlyList<string> tokens,
        Func<int, T> convert,
        string kind)
    {
        var index = -1;
        for (var candidate = 0; candidate < tokens.Count; candidate++)
        {
            if (string.Equals(tokens[candidate], token, StringComparison.Ordinal))
            {
                index = candidate;
                break;
            }
        }

        return index >= 0
            ? convert(index)
            : throw new ArgumentOutOfRangeException(nameof(token), token, $"Unknown linking preflight {kind}.");
    }
}
