using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

public sealed record CreateLinkingConfirmationJobRequest(
    Guid PreflightId,
    string PreflightToken,
    Guid IdempotencyKey);

public abstract record LinkingConfirmationSubmissionDto
{
    private LinkingConfirmationSubmissionDto() { }

    public abstract string ResourceKind { get; }

    public sealed record Job(LinkingConfirmationJobStatusDto Resource) : LinkingConfirmationSubmissionDto
    {
        public override string ResourceKind => "job";
    }

    public sealed record DurableOutcome(LinkingDurableConfirmationOutcomeDto Resource)
        : LinkingConfirmationSubmissionDto
    {
        public override string ResourceKind => "durable_outcome";
    }
}

public sealed record LinkingConfirmationJobReceipt(
    LinkingConfirmationSubmissionDto Submission,
    bool IsNew);

public sealed record LinkingConfirmationJobStatusDto(
    Guid JobId,
    Guid PreflightId,
    string Status,
    string Stage,
    int ProcessedItems,
    int TotalItems,
    int PollAfterMs,
    bool CancellationRequested,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    LinkingConfirmationResultDto? Result,
    string? FailureCode);

public sealed record LinkingDurableConfirmationOutcomeDto(
    string ResourceKind,
    Guid JobId,
    Guid PreflightId,
    Guid IdempotencyKey,
    string Status,
    DateTimeOffset CompletedAtUtc,
    LinkingConfirmationResultDto Result);

public sealed record LinkingConfirmationJobLease(
    Guid JobId,
    Guid PreflightId,
    int ActorUserId,
    int DoorId,
    Guid IdempotencyKey,
    string RequestHash,
    Guid LeaseOwner,
    int AttemptCount,
    LinkingConfirmationJobStatus Status);

public sealed record LinkingPreparedConfirmationExecution(
    LinkingOperationRequest Request,
    LinkingOperationIntent Intent,
    int TotalItems);

public enum LinkingConfirmationJobConflictKind
{
    IdempotencyConflict,
    ActiveWorkflowLimit,
    PreflightNotReady,
    PreflightBlocked,
    PreflightStale,
    PreflightExpired,
    CancellationTooLate,
    TerminalState,
}

public sealed class LinkingConfirmationJobConflictException(
    LinkingConfirmationJobConflictKind kind,
    string failureCode) : Exception(failureCode)
{
    public LinkingConfirmationJobConflictKind Kind { get; } = kind;
    public string FailureCode { get; } = failureCode;
}
